using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectronCatcher : MonoBehaviour { 

    public int capacity = 10;
    public bool useCollectionAnimation = false;
    public KeyCode giveMaxCharge = KeyCode.G;

    public int storage;

	void Start () {
        storage = 0;
	}

    private void Update()
    {
        if (Input.GetKeyDown(giveMaxCharge))
            AddCharge(capacity);
    }

    public void ClearCharge()
    {
        storage = 0;
    }
	
    public void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("Electron catcher hit by " + collision.collider.gameObject.name);

        var electron = collision.collider.GetComponent<Electron>();
        if (electron != null)
        {
            AddCharge(1);
        }
    }

    private void AddCharge(int amount)
    {
        storage += amount;

        if (storage > capacity)
            storage = capacity;
    }
}
