using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DrumInputMIDIMap
{
    public static DrumSequence.DrumKey ReadDrumKeyDown()
    {
        DrumSequence.DrumKey hit = DrumSequence.DrumKey.NONE;

        //Note -- MidiJack is not included with this sample project, but is the creation of Keijiro Takahashi
        //https://github.com/keijiro/MidiJack

        //if (MidiJack.MidiMaster.GetKeyDown(0x1F))
        //    hit = DrumSequence.DrumKey.One;

        //if (MidiJack.MidiMaster.GetKeyDown(0x30))
        //    hit = DrumSequence.DrumKey.Two;

        //if (MidiJack.MidiMaster.GetKeyDown(0x2F))
        //    hit = DrumSequence.DrumKey.Three;

        //if (MidiJack.MidiMaster.GetKeyDown(0x2D))
        //    hit = DrumSequence.DrumKey.Four;

        //if (MidiJack.MidiMaster.GetKeyDown(0x2B))
        //    hit = DrumSequence.DrumKey.Five;

        //if (MidiJack.MidiMaster.GetKeyDown(0x33))
        //    hit = DrumSequence.DrumKey.Six;

        //if (MidiJack.MidiMaster.GetKeyDown(0x31) || MidiJack.MidiMaster.GetKeyDown(0x37))
        //    hit = DrumSequence.DrumKey.Seven;

        return hit;
    }
}
