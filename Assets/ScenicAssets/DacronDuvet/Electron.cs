using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class Electron : MonoBehaviour {

    public float gravity = -3f;
    public float bounciness = 0.5f;
    public float detachedLifetime = 5f;
    public bool emitStreamOnDestroy = false;
    public bool emitBurstOnDestroy = true;
    public int emitNumber = 100;
    [HideInInspector]
    public Rigidbody rb;

    private Transform localT;
    private ParticleSystem particles;
    private MeshRenderer meshRenderer;
    private SphereCollider sphereCollider;

    private ParticleSystem orbitParticles;
    private GameObject orbitObj;
    private ParticleSeek orbitSeek;
    private ParticleCollectionFX collectionFX;

    private bool destroying = false;
    private Vector3 gravityForce;
    private float flightTime;

    private bool finalAnimation;
    private UpdateElectronTracking updateMotionCallback;
    private UpdateElectronTracking untrackCallback;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        localT = gameObject.transform;
        particles = GetComponent<ParticleSystem>();
        meshRenderer = GetComponent<MeshRenderer>();
        sphereCollider = GetComponent<SphereCollider>();

        orbitParticles = gameObject.transform.GetChild(0).GetComponentInChildren<ParticleSystem>();
        orbitObj = orbitParticles.gameObject;
        orbitSeek = GetComponentInChildren<ParticleSeek>();

        collectionFX = GetComponentInChildren<ParticleCollectionFX>();
        if (collectionFX != null)
            collectionFX.SetAnimationEndedCallback(delegate { Destroy(gameObject); Destroy(orbitObj); });
    }

    void Start () {
        gravityForce = new Vector3(0, gravity, 0);
        finalAnimation = false;
        destroying = false;
        flightTime = 0f;
	}
	
	void FixedUpdate () {
        //apply low gravity if eligible
        //if (!stuck)
            //{
                //rb.AddForce(gravityForce * Time.fixedDeltaTime);

                //rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y - (gravity * Time.fixedDeltaTime), rb.velocity.z);
            //}

        flightTime += Time.fixedDeltaTime;
	}

    public void SetBounceCallback(UpdateElectronTracking callbackParam)
    {
        updateMotionCallback = callbackParam;
    }

    public void SetDisableCallback(UpdateElectronTracking callbackParam)
    {
        untrackCallback = callbackParam;
    }

    public void StartDestroyAnimation()
    {
        if (!destroying)
        {
            destroying = true;
            StartCoroutine(AnimateDestroy());
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        ElectronCatcher catcher = collision.collider.gameObject.GetComponent<ElectronCatcher>();
        if (catcher != null && !finalAnimation)
        {
            if (untrackCallback != null)
                untrackCallback.Invoke(this); //notify Bot that this object is being destroyed

            //TODO: there is a rare bug where code execution will stop here
            //when this occurs, electrons may not be properly destroyed when colliding with the floor

            if (catcher.useCollectionAnimation)
            {
                PrepareCollectionAnimation();
                collectionFX.StartAnimation(collision.transform);
                finalAnimation = true;
            }
            else
            {
                StartDestroyAnimation();
                finalAnimation = true;
            }
        }
        else if (catcher == null)
        {
            if (updateMotionCallback != null)
                updateMotionCallback.Invoke(this);
        }
    }

    private void PrepareCollectionAnimation()
    {
        meshRenderer.enabled = false;
        sphereCollider.enabled = false;
        DetachOrbit();
    }

    private IEnumerator AnimateDestroy()
    {
        meshRenderer.enabled = false;
        if (emitBurstOnDestroy)
        {
            particles.Emit(emitNumber);
        }
        if (emitStreamOnDestroy)
        {
            particles.Play();
            var emissionMod = particles.emission;
            emissionMod.enabled = true;
        }
        //particles.Emit(300);
        
        
        sphereCollider.enabled = false;

        DetachOrbit();
        //orbitObj.transform.parent = this.gameObject.transform.parent;
        //Rigidbody orbitRB = orbitObj.AddComponent<Rigidbody>();
        //orbitRB.useGravity = false;
        //orbitRB.velocity = Vector3.zero;
        //orbitParticles.Stop();
        ////var mainModule = orbitParticles.main;
        ////mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        ////orbitObj.transform.position = localT.position;
        //var collision = orbitParticles.collision;
        //collision.enabled = true;
        ////orbitSeek.enabled = false;

        //ParticleSystem.Particle[] orbitArray = new ParticleSystem.Particle[orbitParticles.particleCount];
        //orbitParticles.GetParticles(orbitArray);

        float maxTimer = detachedLifetime;
        float timer = maxTimer;
        while (timer > 0f)
        {
            //float scale = Mathf.Lerp(1f, 2.5f, 1 - (timer / maxTimer));
            //orbitObj.transform.localScale = new Vector3(scale, scale, scale);
            timer -= Time.deltaTime;
            yield return null;
        }

        maxTimer = orbitParticles.main.duration - maxTimer;
        timer = maxTimer;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        Destroy(orbitObj);
        Destroy(this.gameObject);
    }

    private void DetachOrbit()
    {
        orbitObj.transform.parent = this.gameObject.transform.parent;
        Rigidbody orbitRB = orbitObj.AddComponent<Rigidbody>();
        orbitRB.useGravity = false;
        orbitRB.velocity = Vector3.zero;
        orbitParticles.Stop();
        var collision = orbitParticles.collision;
        collision.enabled = true;
    }
}
