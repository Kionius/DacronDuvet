using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IElectronTracker {

    void TrackElectron(Electron electron);
    void UntrackElectron(Electron electron);
}
