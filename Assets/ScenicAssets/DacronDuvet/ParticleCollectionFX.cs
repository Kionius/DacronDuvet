using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleCollectionFX : MonoBehaviour {

    public float durationWeightless = 2f;
    public float durationAttract = 2f;
    public float attractionForceStart = 5f;
    public float attractionForceEnd = 50f;
    public AnimationCurve translateCurveX;
    public AnimationCurve translateCurveY;

    public Transform targetT;

    private float startingTimescale;
    private ParticleSystem particles;
    private UnityAction onAnimationEnd;

    private void Awake()
    {
        particles = GetComponent<ParticleSystem>();
    }

    public void SetAnimationEndedCallback(UnityAction action)
    {
        onAnimationEnd = action;
    }

    public void StartAnimation(Transform attractTarget)
    {
        targetT = attractTarget;
        StartCoroutine(AttractionAnimation());
    }

    private IEnumerator AttractionAnimation()
    {
        particles.Emit(100);

        var main = particles.main;
        startingTimescale = main.simulationSpeed;

        float maxTimer = durationWeightless;
        float timer = maxTimer;
        while (timer > 0f)
        {
            main.simulationSpeed = Mathf.Lerp(startingTimescale, 0, 1 - (timer / maxTimer));
            timer -= Time.deltaTime;
            yield return null;
        }
        
        main.gravityModifier = 0f;
        main.simulationSpeed = startingTimescale;

        maxTimer = durationAttract;
        timer = maxTimer;

        ParticleSystem.Particle[] partArray =
            new ParticleSystem.Particle[particles.particleCount];

        particles.GetParticles(partArray);

        Vector3[] originArray = new Vector3[partArray.Length];

        //Note: only works for worldspace particle simulation
        for (int i = 0; i < partArray.Length; i++)
        {
            originArray[i] = partArray[i].position;
        }

        while (timer > 0f)
        {
            float lerpVal = 1 - timer / maxTimer;
            float xLerp = translateCurveX.Evaluate(lerpVal);
            float yLerp = translateCurveY.Evaluate(lerpVal);

            for (int i = 0; i < partArray.Length; i++)
            {
                float posX = Mathf.Lerp(originArray[i].x, targetT.position.x, xLerp);
                float posY = Mathf.Lerp(originArray[i].y, targetT.position.y, yLerp);
                float posZ = Mathf.Lerp(originArray[i].z, targetT.position.z, lerpVal); //constant slope
                partArray[i].position = new Vector3(posX, posY, posZ);
            }

            particles.SetParticles(partArray, partArray.Length);

            timer -= Time.deltaTime;
            yield return null;
        }

        onAnimationEnd.Invoke();
    }
}
