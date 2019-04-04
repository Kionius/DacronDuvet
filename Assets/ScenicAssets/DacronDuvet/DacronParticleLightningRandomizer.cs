using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class DacronParticleLightningRandomizer : MonoBehaviour {

    /* TODO
     * Each time a lightning particle fires, it should select one of several variants
     */

    public float circleRadius;
    public KeyCode triggerLightningKey = KeyCode.P;
    public float maxFrequency = 1f;
    public float minFrequency = 5f;
    public float frequencyInput01 = 0f;
    public float maxRandomDelay = 0.03f;

    private ParticleSystem particle;
    private float zPlane;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();

        zPlane = transform.localPosition.z;
    }

    private void Start()
    {
        StartCoroutine(RandomizingEmission());
    }

    void Update () {
		if (Input.GetKeyDown(triggerLightningKey))
        {
            float randomAngle = Random.Range(0f, 1f) * Mathf.PI * 2f;
            TriggerAtRandomPoint(randomAngle);
        }
	}

    private IEnumerator RandomizingEmission()
    {
        float startOffsetSeed = Random.Range(0f, 1f);
        float offsetDelay = MathHelper.MapToRangeLin(0f, 1f, maxFrequency, minFrequency, startOffsetSeed);

        yield return new WaitForSeconds(offsetDelay);

        float nextEmissionTimer = maxFrequency;
        float extraDelay = 0f;



        while (true)
        {
            float randomAngle = Random.Range(0f, 1f) * Mathf.PI * 2f;
            TriggerAtRandomPoint(randomAngle);

            extraDelay = Random.Range(0f, maxRandomDelay);
            nextEmissionTimer = MathHelper.MapToRangeLin(0f, 1f, minFrequency, maxFrequency, frequencyInput01);
            nextEmissionTimer += extraDelay;

            yield return new WaitForSeconds(nextEmissionTimer);
        }
    }

    public void TriggerAtRandomPoint(float radians)
    {
        //Move to a random position along the radius, corresponding to [phase] (in radians, 0 - 2 + pi)
        float x = Mathf.Cos(radians) * circleRadius;
        float y = Mathf.Sin(radians) * circleRadius;

        Vector3 randomPoint = new Vector3(x, y, 0f);

        //Calculate the angle to center point (z only, with 0 as top center and moving counterclockwise as it increases)
        float zAngle = radians * Mathf.Rad2Deg - 90f;
        Vector3 eulerAngles = new Vector3(0, 0, zAngle);
        Quaternion rotationToCenter = Quaternion.Euler(eulerAngles);

        //Randomize the index of particleObjs to access

        //Set the object's z rotation by the calculated euler angle
        transform.localPosition = randomPoint;
        transform.localRotation = rotationToCenter;

        //Have the particle system emit a particle
        particle.Emit(1);
    }
}
