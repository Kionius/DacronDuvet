using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSeek : MonoBehaviour
{
    public ParticleSystem particles;
    public Transform target;
    public float force = 10.0f;
    public bool scaleForceByR2 = false;
    public float r2Scale = 1f;
    public bool enableOnStart = false;

    ParticleSystem ps;

    void Awake()
    {
        if (particles != null)
        {
            ps = particles;
        }
        else
        {
            ps = GetComponent<ParticleSystem>();
        }
        
        enabled = enableOnStart;
    }

    void LateUpdate()
    {
        ParticleSystem.Particle[] particles =
            new ParticleSystem.Particle[ps.particleCount];

        ps.GetParticles(particles);

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem.Particle p = particles[i];

            Vector3 particleWorldPosition;

            if (ps.main.simulationSpace == ParticleSystemSimulationSpace.Local)
            {
                particleWorldPosition = transform.TransformPoint(p.position);
            }
            else if (ps.main.simulationSpace == ParticleSystemSimulationSpace.Custom)
            {
                particleWorldPosition = ps.main.customSimulationSpace.TransformPoint(p.position);
            }
            else
            {
                particleWorldPosition = p.position;
            }

            Vector3 directionToTarget = (target.position - particleWorldPosition).normalized;
            Vector3 seekForce = (directionToTarget * force) * Time.deltaTime;

            if (scaleForceByR2)
            {
                float distance = Vector3.Distance(target.position, particleWorldPosition);
                float r2 = distance * distance;
                seekForce *= r2 * r2Scale;
            }

            p.velocity += seekForce;

            particles[i] = p;
        }

        ps.SetParticles(particles, particles.Length);
    }

    public void SetSeekTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void EnableAndSetTarget(Transform newTarget)
    {
        enabled = true;
        target = newTarget;
    }

    public void ToggleParticleSystem()
    {
        if (ps.isPlaying)
            ps.Stop();
        else
            ps.Play();
    }
}