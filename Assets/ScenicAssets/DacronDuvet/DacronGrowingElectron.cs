using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The central electron within the DacronGear that charges up to generate the projectile Electrons
/// which SquirrelBot catches
/// </summary>
public class DacronGrowingElectron : MonoBehaviour {

    /* TODOLIST:
     * Define rate of charge/growth relative to the angular velocity of the gear
     * Define rate of decay when gear stops spinning
     * Define thresholds at which multiple electrons begin spawning with each finished sequence
     * Define charge loss when multiple electrons are dropped (these freebies should be treated as a short term bonus)
     * (Losing the charge when the bonus is put into play prevents scaling up to ridiculous numbers of electrons)
     * When do we WANT ridiculous numbers of electrons?
     * At some level of progress, maybe the electron gets HUGE and stops dropping electrons for a bit,
     * then drops 100 all at once
     */

    public float chargeScale = 1f;
    public float maxCharge = 100f;
    public float decayRate = 1f;
    public float bonusThreshold = 50f;
    public float dischargePerElectron = 10f;
    public float bonusElectronInterval = 0.3f;

    public float maxSize = 5f;
    public float minSize = 1f;

    public Transform particleTransform;
    public ElectronLauncher launcher; //TODO: temporary ref, unless we can get these two implementations
                                      //to work together
    public DrumGameSwitcher drumSwitcher;

    private float localCharge = 0f; //0 - 100
    private Transform localT;

    private void Awake()
    {
        localT = gameObject.transform;
    }

    private void Start()
    {
        drumSwitcher.SubscribeToFinishRepetitionEvent(Discharge);
    }

    //public void SetScaleByAngularVelocity(Vector3 angularVelocity, float maxVelocity)
    //{
    //    float size = MathHelper.MapToRangeLin(0f, maxVelocity, minSize, maxSize, -angularVelocity.z);
    //    SetScale(new Vector3(size, size, size));
    //}

    public void AddChargeByAngularVelocity(Vector3 angularVelocity, float maxVelocity)
    {
        float normalizedVelocity = MathHelper.MapToRangeLin(0f, maxVelocity, 0f, 1f, -angularVelocity.z);
        float charge = normalizedVelocity * chargeScale;

        localCharge += charge;
        if (localCharge > 100f)
            localCharge = 100f;
    }

    public void Discharge()
    {
        //Decrement charge level
        localCharge -= dischargePerElectron;

        if (localCharge < 0f)
            localCharge = 0f;

        //Spawn an electron
        launcher.FireElectron();

        //If sufficient charge remains, spawn additional electrons over time
        if (localCharge > bonusThreshold)
            StartCoroutine(RepeatDischarge());
    }

    private IEnumerator RepeatDischarge()
    {
        yield return new WaitForSeconds(bonusElectronInterval);

        Discharge();
    }

    private void Update()
    {
        localCharge -= decayRate * Time.deltaTime;

        SetScaleByCharge();
    }

    private void SetScaleByCharge()
    {
        float scale = MathHelper.MapToRangeLin(0f, 100f, minSize, maxSize, localCharge);
        Vector3 nextScale = new Vector3(scale, scale, scale);

        SetScale(nextScale);
    }

    private void SetScale(Vector3 scale)
    {
        localT.localScale = scale;
        particleTransform.localScale = scale;
    }
}
