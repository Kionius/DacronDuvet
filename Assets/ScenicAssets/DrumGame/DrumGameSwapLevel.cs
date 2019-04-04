using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrumGameSwapLevel : MonoBehaviour {

    public DrumGameSwitcher drumSwitcher;
    public DrumGameLevel swappableLevel;
    public KeyCode swapHotkey = KeyCode.S;

	void Update () {
		if (Input.GetKeyDown(swapHotkey))
        {
            drumSwitcher.LoadDrumLevel(swappableLevel);
        }
	}
}
