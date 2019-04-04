using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DacronCable : MonoBehaviour {

    public float chargeEventDuration = 2f;
    public float glowScale = 1.15f;
    public AnimationCurve glowIntensityCurve;

    private Color baseGlowColor;
    private Color maxGlowColor;
    private List<Material> cableMats;

    private void Awake()
    {
        cableMats = new List<Material>();

        var cableRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var cable in cableRenderers)
            cableMats.Add(cable.material);

        baseGlowColor = cableMats[0].GetColor("_EmissionColor");
        maxGlowColor = new Color
        (
            baseGlowColor.r * glowScale,
            baseGlowColor.g * glowScale,
            baseGlowColor.b * glowScale,
            baseGlowColor.a
        );
    }

    public void StartChargeEvent()
    {
        StartCoroutine(GlowIntensifying());
    }

    private IEnumerator GlowIntensifying()
    {
        float timer = 0f;

        while (timer < chargeEventDuration)
        {
            timer += Time.deltaTime;
            float t = timer / chargeEventDuration;
            float curveVal = glowIntensityCurve.Evaluate(t);

            Color animatedColor = Color.Lerp(baseGlowColor, maxGlowColor, curveVal);
            SetAllEmissiveColors(animatedColor);

            yield return null;
        }
    }

    private void SetAllEmissiveColors(Color color)
    {
        foreach (var cable in cableMats)
            cable.SetColor("_EmissionColor", color);
    }
}
