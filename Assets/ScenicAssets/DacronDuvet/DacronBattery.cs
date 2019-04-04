using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DacronBattery : MonoBehaviour {

    public KeyCode addChargeKey = KeyCode.Q;
    public Transform dockTransform;
    //public EmitLightByAmplitude lightControl;
    public BatteryWall batteryWall;

    public float chargeLevel;
    public bool discharging = false;
    public float chargeUsagePerSecond = 0.025f;
    public float rapidDischargePerSecond = 0.5f;
    public float initialCharge = 0f;
    
    [ColorUsage(false, true)]
    public Color maxColor;
    [ColorUsage(false, true)]
    public Color minColor;
    public GameObject ledObj;
    public string colorPropKey = "_EmissionColor";

    public float maxYScale = 1.01f;
    public float minYScale = 0.21f;

    public float emissiveLightScalar = 1.5f;
    //public float maxLightRange = 50f;
    //public float minLightRange = 10f;
    //public float maxLightAmp = 10f;
    //public float minLightAmp = 1f;
    //public Light ledLight;

    private float cachedChargeUsage;
    private Color ledColor;
    private Transform ledTransform;
    private Material ledMat;
    private Coroutine dischargingToWall;
    private bool updatingLED = true;

    private void Awake()
    {
        MeshRenderer ledRenderer = ledObj.GetComponent<MeshRenderer>();
        ledMat = ledRenderer.material;

        ledTransform = ledObj.transform;
    }

    void Start ()
    {
        chargeLevel = 0f;
        UpdateLED();
	}
	
	public void AddCharge(float charge)
    {
        chargeLevel += charge;
        if (chargeLevel > 1f)
            chargeLevel = 1f;

        UpdateLED();
    }

    public void SetCharge(float charge)
    {
        chargeLevel = charge;
        if (chargeLevel > 1f)
            chargeLevel = 1f;

        if (chargeLevel < 0f)
            chargeLevel = 0f;

        UpdateLED();
    }

    public void SetEmissiveLightScalar(float scalar)
    {
        emissiveLightScalar = scalar;
    }

    public void EnableLEDUpdate()
    {
        updatingLED = true;
    }

    public void DisableLEDUpdate()
    {
        updatingLED = false;
    }

    public void SetLEDColor(Color c)
    {
        ledMat.SetColor(colorPropKey, c);
    }

    public Color GetLEDColor()
    {
        return ledColor;
    }

    public void TogglePower()
    {
        discharging = !discharging;
    }

    public Transform GetDockTransform()
    {
        return dockTransform;
    }

    private void Update()
    {
        if (discharging && chargeLevel > 0)
        {
            chargeLevel -= chargeUsagePerSecond * Time.deltaTime;
            UpdateLED();
        }

        if (chargeLevel > 0.98f && batteryWall != null && dischargingToWall == null)
            dischargingToWall = StartCoroutine(RapidDischarge());

        //Debug hotkey
        if (Input.GetKeyDown(addChargeKey))
            AddCharge(0.10f);
    }

    private IEnumerator RapidDischarge()
    {
        batteryWall.StartChargeEvent();

        //Save original discharge rate and replace it with the faster setting until battery is depleted
        cachedChargeUsage = chargeUsagePerSecond;
        chargeUsagePerSecond = rapidDischargePerSecond;

        float dischargeTime = 1f / rapidDischargePerSecond;
        float timer = 0f;

        while (timer < dischargeTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        chargeUsagePerSecond = cachedChargeUsage;
        dischargingToWall = null;
    }

    private void UpdateLED()
    {
        ledColor = Color.Lerp(minColor, maxColor, chargeLevel);

        //ledLight.color = c; //Set the point light without extra intensity scaling

        ledColor.r *= emissiveLightScalar;
        ledColor.g *= emissiveLightScalar;
        ledColor.b *= emissiveLightScalar;

        if (updatingLED)
            ledMat.SetColor(colorPropKey, ledColor);

        //float lightRange = Mathf.Lerp(lowChargeLightRange, highChargeLightRange, chargeLevel);
        //float lightAmp = Mathf.Lerp(lowChargeLightAmp, highChargeLightAmp, chargeLevel);

        //ledLight.range = lightRange;
        //ledLight.intensity = lightAmp;

        //-- Update transform of the LED mesh
        float yScale = Mathf.Lerp(minYScale, maxYScale, chargeLevel);
        Vector3 newScale = new Vector3(ledTransform.localScale.x, yScale, ledTransform.localScale.z);
        ledTransform.localScale = newScale;

        float yPos = yScale - 1f;
        Vector3 newPos = new Vector3(ledTransform.localPosition.x, yPos, ledTransform.localPosition.z);
        ledTransform.localPosition = newPos;
    }
}
