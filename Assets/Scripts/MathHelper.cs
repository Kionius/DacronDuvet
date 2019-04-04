using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathHelper {

    public static float SineFunc(float amp, float frequency, float time)
    {
        return (Mathf.Sin(2f * Mathf.PI * frequency * time) * amp);
    }

    public static float SineFunc(float amp, float frequency, float time, float phase)
    {
        return (Mathf.Sin(2f * Mathf.PI * frequency * time + phase) * amp);
    }

    public static float CosineFunc(float amp, float frequency, float time)
    {
        return (Mathf.Cos(2f * Mathf.PI * frequency * time) * amp);
    }

    public static float CosineFunc(float amp, float frequency, float time, float phase)
    {
        return (Mathf.Cos(2f * Mathf.PI * frequency * time + phase) * amp);
    }

    public static float MapToRangeLin (float minIn, float maxIn, float minOut, float maxOut, float input)
    {
        if (input <= minIn)
            return minOut;
        else if (input >= maxIn)
            return maxOut;
        
        return ((maxOut - minOut) * (input - minIn)) / (maxIn - minIn) + minOut;
    }

    public static float MapToRangeLog(float minIn, float maxIn, float minOut, float maxOut, float input)
    {
        if (input <= minIn)
            return minOut;
        else if (input >= maxIn)
            return maxOut;

        float minv = Mathf.Log(minOut);
        float maxv = Mathf.Log(maxOut);

        float scale = (maxv - minv) / (maxIn - minIn);

        return Mathf.Exp(minv + scale * (input - minIn));
    }

    public static float MapToRangeLin2(float a1, float a2, float b1, float b2, float input)
    {
        return b1 + (input - a1) * (b2 - b1) / (a2 - a1);
    }

    /// <summary>
    /// Rounds an input value to be the closest of min or max parameters.
    /// </summary>
    public static float RoundBetween(float min, float max, float input)
    {
        float midpoint = (max + min) / 2f;
        return input >= midpoint ? max : min;
    }

    /// <summary>
    /// Returns the parameter value that is closest by absolute distance to the input value. Returns val1 if equal.
    /// </summary>
    public static float NearestOf(float val1, float val2, float input)
    {
        float delta1 = Mathf.Abs(input - val1);
        float delta2 = Mathf.Abs(input - val2);

        return delta1 >= delta2 ? val1 : val2;
    }
}
