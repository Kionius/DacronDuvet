using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DrumGameLevel", menuName = "BlacksAppbone/DrumGameLevel", order = 12)]
public class DrumGameLevel : ScriptableObject {

    public List<DrumSequence> sequences;
}
