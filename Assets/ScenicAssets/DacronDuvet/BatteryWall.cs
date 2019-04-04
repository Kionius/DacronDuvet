using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BatteryWall : MonoBehaviour {

    public DacronBattery parentBattery;
    public DacronCable cable;

    public float transferancePerSecond = 1f;
    public float chargePercentPerEvent = 0.05f;
    public float chargeEventDuration = 1f;

    public AnimationCurve chargeGainCurve;
    public AnimationCurve lightGlowCurve;

    [ColorUsage(true, true)]
    public Color chargeEventColor;

    private DacronBattery[] batteries;
    private float baseLightScalar;

	void Start ()
    {
        batteries = GetComponentsInChildren<DacronBattery>();
        baseLightScalar = batteries[0].emissiveLightScalar;
	}
	
	//void Update ()
 //   {
 //       if (parentBattery.chargeLevel.value < 0.01f)
 //           return;

 //       float chargeUpdate = transferancePerSecond * Time.deltaTime;

 //       //parentBattery.chargeLevel.value -= chargeUpdate;

 //       //AddChargeToAllBatteries(chargeUpdate);
	//}

    public void StartChargeEvent()
    {
        cable.StartChargeEvent();

        StartCoroutine(ChargeEventAnimation());
    }

    private IEnumerator ChargeEventAnimation()
    {
        float timer = 0f;
        float startChargeValue = batteries[0].chargeLevel;

        DisableAllLEDUpdates();

        while (timer < chargeEventDuration)
        {
            timer += Time.deltaTime;
            float t = timer / chargeEventDuration;

            float charveCurveVal = chargeGainCurve.Evaluate(t) * chargePercentPerEvent;
            SetChargeForAllBatteries(startChargeValue + charveCurveVal);

            float lightCurveVal = lightGlowCurve.Evaluate(t);
            Color chargeColor = Color.Lerp(batteries[0].GetLEDColor(), chargeEventColor, lightCurveVal);
            SetColorForAllBatteries(chargeColor);

            yield return null;
        }

        EnableAllLEDUpdates(); //Implicitly sets color back to what it should be post-charge

        SetChargeForAllBatteries(startChargeValue + chargePercentPerEvent);
    }

    private void AddChargeToAllBatteries(float val)
    {
        for (int i = 0; i < batteries.Length; i++)
            batteries[i].AddCharge(val);
    }

    private void SetChargeForAllBatteries(float val)
    {
        for (int i = 0; i < batteries.Length; i++)
            batteries[i].SetCharge(val);
    }

    private void DisableAllLEDUpdates()
    {
        for (int i = 0; i < batteries.Length; i++)
            batteries[i].DisableLEDUpdate();
    }

    private void EnableAllLEDUpdates()
    {
        for (int i = 0; i < batteries.Length; i++)
            batteries[i].EnableLEDUpdate();
    }

    private void SetColorForAllBatteries(Color c)
    {
        for (int i = 0; i < batteries.Length; i++)
            batteries[i].SetLEDColor(c);
    }
}
