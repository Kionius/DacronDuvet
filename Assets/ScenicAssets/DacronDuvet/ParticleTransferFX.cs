using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleTransferFX : MonoBehaviour {

    public DacronBattery battery;
    public Transform targetTransform;
    public ParticleSystem particles;
    public AnimationCurve risingCurve;
    public AnimationCurve transferMovementCurve;

    public float transferCurveScale = 2f;
    public float riseTime = 1.5f;
    //public float riseDistance = 2f;
    public float transferTime = 3f;
    public float singleTravelTime = 1.1f;
    public float scaleDecrease = 1f / 20f;

    public int totalParticles = 10;
    public float chargePerParticle = 0.05f;
    public float launchInterval = 0.01f; //seconds
    public Vector3 originPosition;

    private List<Vector3> travelVectors; //randomly inverse or nullify the transferMovementCurve
    private ParticleSystem.Particle[] partArray;
    private GameObject electronObj;

    private void Awake()
    {
        transferTime = riseTime + (launchInterval * totalParticles) + singleTravelTime + 1f;

        electronObj = transform.GetChild(0).gameObject;
    }

    void Start ()
    {
        travelVectors = new List<Vector3>(totalParticles);
        for (int i = 0; i < totalParticles; i++)
        {
            Vector3 randomVector = GetRandomVector();

            //No two consecutive vectors should be the same
            while (i != 0 && randomVector == travelVectors[i - 1])
                randomVector = GetRandomVector();

            travelVectors.Add(randomVector);
        }

        //StartCoroutine(TransferAnimation());
	}

    private Vector3 GetRandomVector()
    {
        //We want a clearly-banded span of trajectories, so cast to int first...
        //Result ends up as -1, 0, or 1. Then scale.
        //Note the x vector is not used by the particle movement function

        float yVector = Mathf.RoundToInt(Random.Range(-1f, 1f)) * transferCurveScale;
        float zVector = Mathf.RoundToInt(Random.Range(-1f, 1f)) * transferCurveScale;
        return new Vector3(1f, yVector, zVector);
    }

    public void SetBattery(DacronBattery battery)
    {
        this.battery = battery;
        targetTransform = battery.transform;
    }

    public void StartTransferAnimation()
    {
        StartCoroutine(TransferAnimation());
    }

    private IEnumerator TransferAnimation()
    {
        partArray = new ParticleSystem.Particle[totalParticles];
        particles.GetParticles(partArray);

        float timer = 0f;

        //Move it upwards by riseDistance over riseTime, set originPosition upon arrival
        originPosition = transform.position;
        //Vector3 risenPosition = originPosition + new Vector3(0f, riseDistance, 0f);
        Vector3 risenPosition = originPosition + new Vector3(
            0f, 
            battery.dockTransform.position.y, //assumes center-alignment of transform 
            0f);

        while (timer < riseTime)
        {
            timer += Time.deltaTime;
            float t = risingCurve.Evaluate(timer / riseTime);
            transform.position = Vector3.Lerp(originPosition, risenPosition, t);

            yield return null;
        }

        originPosition = transform.position;

        //Emit the particles (their velocity is zero) and store them in the array
        particles.Emit(totalParticles);
        particles.GetParticles(partArray);

        //Launch particles one at a time and give them one of the generated motion vectors
        for (int i = 0; i < totalParticles; i++)
        {
            StartCoroutine(SingleParticleTravel(i));

            Vector3 newScale = electronObj.transform.localScale;
            newScale -= new Vector3(scaleDecrease, scaleDecrease, scaleDecrease);
            electronObj.transform.localScale = newScale;

            yield return new WaitForSeconds(launchInterval);
        }

        //Wait for the last particle's travel plus a bit of padding for the trail to fade
        yield return new WaitForSeconds(singleTravelTime * 1.3f);

        Destroy(this.gameObject);
    }

    private IEnumerator SingleParticleTravel(int index)
    {
        float timer = 0f;

        while (timer < singleTravelTime)
        {
            timer += Time.deltaTime;
            float t = timer / singleTravelTime;
            float x = Mathf.Lerp(originPosition.x, targetTransform.position.x, t);
            float y = Mathf.Lerp(originPosition.y, targetTransform.position.y, t) + transferMovementCurve.Evaluate(t) * travelVectors[index].y;
            float z = Mathf.Lerp(originPosition.z, targetTransform.position.z, t) + transferMovementCurve.Evaluate(t) * travelVectors[index].z;
            partArray[index].position = new Vector3(x, y, z);

            particles.SetParticles(partArray, totalParticles); //I think each of these CRs has to do this assignment...?

            yield return null;
        }

        battery.AddCharge(chargePerParticle);
    }
}
