using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BouncyPlatform : MonoBehaviour {

    public Vector3 flatForce = new Vector3(0f, 10f, 0f);
    public Vector3 velocityScalar = new Vector3(1.5f, 1.5f, 0f);
    public float maxDuration = 3f;

    private float duration;
    private Rigidbody rb;
    private Material mat;

    public void SpawnClone()
    {
        GameObject clone = Instantiate(gameObject);
        clone.SetActive(true);
        BouncyPlatform platform = clone.GetComponent<BouncyPlatform>();
        platform.maxDuration = maxDuration;  //this prefab's duration setting
        platform.duration = maxDuration;

    }

    public void SpawnClone(Transform t)
    {
        GameObject clone = Instantiate(gameObject);
        clone.SetActive(true);

        Vector3 position = t.position;
        position.z = 25f;
        clone.transform.position = position;

        Quaternion rotation = t.rotation;
        rotation.x = 0f;
        clone.transform.rotation = rotation;

        BouncyPlatform platform = clone.GetComponent<BouncyPlatform>();
        platform.maxDuration = maxDuration;  //this prefab's duration setting
        platform.duration = maxDuration;

    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        var renderer = gameObject.GetComponent<MeshRenderer>();
        mat = renderer.material;
    }

    private void Update()
    {
        duration -= Time.deltaTime;

        if (duration <= 0f)
            Destroy(this.gameObject);
        else
        {
            Color c = Color.grey;
            c.a = duration / maxDuration;
            mat.SetColor("_Color", c);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody colliderRb = collision.rigidbody;
        if (colliderRb != null)
        {
            //Vector3 bounceForce = Vector3.Scale(colliderRb.velocity, velocityScalar);
            //bounceForce += flatForce;
            //colliderRb.AddForce(bounceForce);
            //Debug.Log("Force added = " + bounceForce);

            //Split a float force value into vector components based on the platform's rotation?

            colliderRb.AddForce(flatForce);
        }
    }
}
