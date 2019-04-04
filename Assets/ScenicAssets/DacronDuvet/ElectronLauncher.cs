using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectronLauncher : MonoBehaviour {

    public bool firing = true;
    public float firingRate = 0.5f;
    public Vector3 minFiringForce = new Vector3(1, 3, 0);
    public Vector3 maxFiringForce = new Vector3(3, 10, 0);
    public KeyCode fireAtMinForce = KeyCode.W;
    public KeyCode fireAtMaxForce = KeyCode.E;
    public KeyCode enableAutoFire = KeyCode.T;
    public KeyCode fireRandom = KeyCode.R;
    public GameObject electronPrefab;
    public Transform electronSpawn;
    public Transform electronParent;
    public CatcherBotAI bot;

    private Coroutine firingLoop;
    private int id = 0;

	void Start () {
        if (firing)
            firingLoop = StartCoroutine(FireElectronLoop());
	}

    public void LaunchN(int n)
    {
        while (n > 0)
        {
            FireElectron();
            --n;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(fireAtMinForce))
            FireElectron(minFiringForce);
        if (Input.GetKeyDown(fireAtMaxForce))
            FireElectron(maxFiringForce);
        if (Input.GetKeyDown(enableAutoFire))
            firingLoop = StartCoroutine(FireElectronLoop());
        if (Input.GetKeyDown(fireRandom))
            FireElectron();
    }

    private IEnumerator FireElectronLoop()
    {
        firing = true;
        while (firing)
        {
            FireElectron();
            yield return new WaitForSeconds(firingRate);
        }

        firingLoop = null;
    }

    public void FireElectron()
    {
        GameObject elecObj = Instantiate<GameObject>(electronPrefab, electronParent, true);
        elecObj.name = "Electron " + id++;
        elecObj.transform.position = electronSpawn.position;

        Vector3 randomForce = new Vector3(
            Random.Range(minFiringForce.x, maxFiringForce.x),
            Random.Range(minFiringForce.y, maxFiringForce.y),
            0);
        Rigidbody elecRB = elecObj.GetComponent<Rigidbody>();
        elecRB.velocity = randomForce;
        //elecRB.AddForce(randomForce);

        Electron electron = elecObj.GetComponent<Electron>();
        UpdateBotTracking(electron);
    }

    //TODO: the Launcher shouldn't own the logic to bind the Bot's tracking to the individual Electrons
    //this may be better done in whatever manager takes care of pooling the Electrons
    //all of the pooled Electrons could be given the callback once at scene start

    public void FireElectron(Vector3 firingForce)
    {
        GameObject elecObj = Instantiate<GameObject>(electronPrefab, electronParent, true);
        elecObj.name = "Electron " + id++;
        elecObj.transform.position = electronSpawn.position;

        Rigidbody elecRB = elecObj.GetComponent<Rigidbody>();
        elecRB.velocity = firingForce;
        //elecRB.AddForce(firingForce);

        Electron electron = elecObj.GetComponent<Electron>();
        UpdateBotTracking(electron);
    }

    private void UpdateBotTracking(Electron electron)
    {
        bot.TrackElectron(electron);
        electron.SetDisableCallback(bot.UntrackElectron);
    }
}
