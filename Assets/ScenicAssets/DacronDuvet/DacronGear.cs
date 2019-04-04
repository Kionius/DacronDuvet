using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

[RequireComponent(typeof(Rigidbody))]
public class DacronGear : MonoBehaviour {

    public FloatReactiveProperty successForce = new FloatReactiveProperty(20f);
    public FloatReactiveProperty errorForce = new FloatReactiveProperty(-10f);
    public float maxAngularVelocity = 500f;
    public float maxElectronSize = 5f;
    public KeyCode debugSpinHotkey = KeyCode.S;
    public List<DacronParticleLightningRandomizer> lightningSources;

    public DrumGameSwitcher drumGameSwitcher;
    public DacronGrowingElectron growingElectron;

    private Rigidbody rb;
    private Vector3 successVector;
    private Vector3 errorVector;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        successForce.Subscribe(force => successVector = new Vector3(0, -force, 0));
        errorForce.Subscribe(force => errorVector = new Vector3(0, -force, 0));
    }

    void Start ()
    {
        drumGameSwitcher.SubscribeToProgressEvent(AddAngularVelocity);
        drumGameSwitcher.SubscribeToFinishRepetitionEvent(SpawnElectron);
        drumGameSwitcher.SubscribeToMistakeEvent(RemoveAngularVelocity);
	}
	
	void Update () {
        if (Input.GetKeyDown(debugSpinHotkey))
            AddAngularVelocity();

        growingElectron.AddChargeByAngularVelocity(rb.angularVelocity, maxAngularVelocity);

        UpdateLightingParticles();
	}

    //Add velocity up to the defined velocity cap
    private void AddAngularVelocity()
    {
        rb.AddRelativeTorque(successVector);
    }

    //Slow the gear down, but stop if the velocity would reverse to counter-clockwise
    private void RemoveAngularVelocity()
    {
        //TODO: add a check to prevent pushing the gear counterclockwise

        rb.AddRelativeTorque(errorVector);
    }

    //Use the current charge of the central electron particle to determine how many electrons should launch
    //Decrement the charge for each one spawned
    private void SpawnElectron()
    {
        //TODO: create an electron charging class and vfx, call to activate it from here

        //Debug.Log("Fire electrons!");
    }

    private void UpdateLightingParticles()
    {
        float normalizedAngularVelocity = MathHelper.MapToRangeLin(0f, 5f, 0f, 1f, -rb.angularVelocity.z);

        //Debug.Log($"AngVel = {rb.angularVelocity}; normalized = {normalizedAngularVelocity}");

        for (int i = 0; i < lightningSources.Count; i++)
        {
            lightningSources[i].frequencyInput01 = normalizedAngularVelocity;
        }
    }
}
