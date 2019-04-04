using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

[CreateAssetMenu(fileName = "DrumSequence", menuName = "BlacksAppbone/DrumSequence", order = 11)]
public class DrumSequence : ScriptableObject, ICloneable
{
    public SequenceType type = SequenceType.Circle;
    [UnityEngine.Serialization.FormerlySerializedAs("sequence")]
    public List<DrumKey> keys;

    public int repetitions = 20;

    public enum DrumKey { NONE, One, Two, Three, Four, Five, Six, Seven }
    public enum SequenceType { Circle, Line }
    public List<Vector3> coords;
    public List<bool> showLine;
    public Vector3 zoomoutPosition;

    public DrumSequence()
    {
        keys = new List<DrumKey>(0);
    }

    public DrumKey GetCurrentDrumKey(int index)
    {
        return keys[index];
    }

    public DrumSequence Clone()
    {
        //Need to make sure these are all treated as value types... Lists of enums??
        //If not we need some more mojo to deep copy the lists and their elements
        DrumSequence clone = ScriptableObject.Instantiate(this);
        clone.type = this.type;
        clone.keys = this.keys;
        clone.repetitions = this.repetitions;
        clone.coords = this.coords;
        clone.zoomoutPosition = this.zoomoutPosition;
        clone.showLine = this.showLine;

        return clone;
    }

    public void SelfCleanUp()
    {
        Debug.Log("TODO: implement DrumSequence.SelfCleanup()");
    }
}
