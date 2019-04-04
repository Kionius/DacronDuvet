using UnityEngine;
using System.Collections;

/// <summary>
/// Contract for types that imply cloning.
/// </summary>
public interface ICloneable {

    void SelfCleanUp();
    //int getIndex(); //ICloneables hold their own index
}
