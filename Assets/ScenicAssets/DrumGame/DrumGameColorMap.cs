using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrumGameColorMap : MonoBehaviour
{
    public List<Color> colorMap;

    public Color GetColorForKey(DrumSequence.DrumKey key)
    {
        int keyCode = (int)key - 1; //Enum is 1-indexed
        return colorMap[keyCode];
    }
}
