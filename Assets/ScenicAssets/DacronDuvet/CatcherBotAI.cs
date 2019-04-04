using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NPBehave;
using System.Linq;

public class CatcherBotAI : MonoBehaviour, IElectronTracker {

    private const string CATCHER_CHARGE = "catcherChargeLevel";
    private const string DISTANCE_BATTERY = "distanceToBattery";

    public float moveSpeed = 1f;
    public float rotateSpeed = 1f;

    public BoxCollider floorCollider;

    public GameObject movementTargetObj;
    public Transform movementTargetTransform;
    public Vector3 catchMoveTarget;

    public ElectronCatcher electronCatcher;

    public ElectronLauncher electronLauncher;
    public DacronBattery battery;
    public GameObject transferPrefab;

    //Movement components
    private Transform localT;
    private Rigidbody rb;
    private float yMovementPlane;
    private Vector3 batteryDockPos;

    //Projectile tracking logic
    private float halfCollectionWidth;
    private float landingPlaneHeight;
    private float turnAngle;

    private List<Electron> unanalyzedElectrons;
    private Dictionary<Electron, ProjectileTrajectoryData> analyzedElectrons;

    //NPBehave AI model
    private Root behaviorTree;
    private Blackboard blackboard;

    private void Awake()
    {
        localT = gameObject.transform;
        rb = GetComponent<Rigidbody>();
        yMovementPlane = localT.localPosition.y;

        unanalyzedElectrons = new List<Electron>();
        analyzedElectrons = new Dictionary<Electron, ProjectileTrajectoryData>();
    }

    void Start ()
    {
        MeshCollider meshCol = electronCatcher.GetComponent<MeshCollider>();
        halfCollectionWidth = (meshCol.transform.localPosition.z * meshCol.transform.localScale.z) - meshCol.bounds.size.x;
        landingPlaneHeight = meshCol.bounds.size.y / 2f + meshCol.transform.position.y;

        SetMovementPoint(movementTargetTransform);

        batteryDockPos = battery.GetDockTransform().position;
        batteryDockPos.y = localT.position.y;

        catchMoveTarget = this.transform.position;

        behaviorTree = CreateBehaviorTree();
        blackboard = behaviorTree.Blackboard;
        UpdateBlackboard();

#if UNITY_EDITOR
        Debugger debugger = (Debugger)this.gameObject.AddComponent(typeof(Debugger));
        debugger.BehaviorTree = behaviorTree;
#endif

        behaviorTree.Start();
    }

    private Root CreateBehaviorTree()
    {
        return new Root(

            new Selector(

                //1-- If basket is not full, catch particles
                new BlackboardCondition(CATCHER_CHARGE, Operator.IS_SMALLER, electronCatcher.capacity, Stops.IMMEDIATE_RESTART,

                    new Action(() => UpdateElectronCatchMovement())),

                //2-- If basket is full AND not at the battery dock, move towards the battery
                new BlackboardQuery(
                    new string[] { CATCHER_CHARGE, DISTANCE_BATTERY },
                    Stops.IMMEDIATE_RESTART,
                    () =>
                    {
                        return blackboard.Get<int>(CATCHER_CHARGE) == electronCatcher.capacity
                        && blackboard.Get<float>(DISTANCE_BATTERY) > 0.5f;
                    },

                    new Action(() => UpdateBatteryDepositMovement())),

                //3-- If basket is full AND at the battery dock, deposit energy
                new BlackboardQuery(
                    new string[] { CATCHER_CHARGE, DISTANCE_BATTERY },
                    Stops.IMMEDIATE_RESTART,
                    () =>
                    {
                        return blackboard.Get<int>(CATCHER_CHARGE) == electronCatcher.capacity
                        && blackboard.Get<float>(DISTANCE_BATTERY) <= 0.5f;
                    },

                    new Action(() => DepositEnergy()))
            )
        );
    }

    void Update ()
    {
        UpdateBlackboard();
	}

    private void UpdateBlackboard()
    {
        blackboard[CATCHER_CHARGE] = electronCatcher.storage;
        blackboard[DISTANCE_BATTERY] = Vector3.Distance(localT.position, batteryDockPos);
    }

    private void DepositEnergy()
    {
        GameObject transferObj = GameObject.Instantiate(transferPrefab);
        transferObj.transform.position = electronCatcher.transform.position;
        ParticleTransferFX transfer = transferObj.GetComponent<ParticleTransferFX>();
        transfer.SetBattery(battery);
        transfer.StartTransferAnimation();

        electronCatcher.ClearCharge();
    }

    //As electron is spawned, analyze its projectile data and give it a call-back to update analysis if it bounces
    public void TrackElectron(Electron electron)
    {
        if (!unanalyzedElectrons.Contains(electron))
            unanalyzedElectrons.Add(electron);

        electron.SetBounceCallback(UpdateTrackedElectron);

        AnalyzeElectrons();
    }

    public void UpdateTrackedElectron(Electron electron)
    {
        //When an electron bounces, its projectile data is now useless and we should recalculate it

        analyzedElectrons.Remove(electron);
        if (!unanalyzedElectrons.Contains(electron))
            unanalyzedElectrons.Add(electron);

        AnalyzeElectrons();
    }

    public void UntrackElectron(Electron electron)
    {
        unanalyzedElectrons.Remove(electron);
        analyzedElectrons.Remove(electron);

        AnalyzeElectrons();
    }

    //After each electron spawn or bounce, calculate any unknown/changed trajectories
    //Then evaluate them all to choose which electron to catch
    private void AnalyzeElectrons()
    {
        Vector3 adjustedCatchPosition = localT.localPosition;
        //adjustedCatchPosition.x += halfCollectionWidth;

        foreach (Electron electron in unanalyzedElectrons)
        {
            ProjectileTrajectoryData projData = AnalyzeTarget(electron, adjustedCatchPosition);
            if (analyzedElectrons.ContainsKey(electron))
            {
                analyzedElectrons[electron] = projData;
            }
            else
            {
                analyzedElectrons.Add(electron, projData);
            }

        }
        unanalyzedElectrons.Clear();

        EvaluateTargets();
    }

    private ProjectileTrajectoryData AnalyzeTarget(Electron proj, Vector3 currentCatcherPosition)
    {
        //Calculate an electron's landing time and position based on its velocity
        //Store this in ProjectileTrajectoryData so we don't have to recalculate anything until a collision occurs

        float timeToGround;
        if (proj.rb.velocity.y > 0)
        {
            float timeToRise = proj.rb.velocity.y / -Physics.gravity.y;
            float maxHeight = proj.transform.position.y + proj.rb.velocity.y * timeToRise - (0.5f * -Physics.gravity.y * Mathf.Pow(timeToRise, 2));
            float timeToFall = Mathf.Sqrt((2f * (maxHeight - landingPlaneHeight)) / -Physics.gravity.y);
            timeToGround = timeToRise + timeToFall;
            //Debug.Log("Upward projectile analysis for " + proj.gameObject.name + ": timeToGround = " + timeToGround +
                //", maxHeight = " + maxHeight + ", timeToFall = " + timeToFall);
        }
        else
        {
            timeToGround = (-proj.rb.velocity.y - Mathf.Sqrt(Mathf.Abs(Mathf.Pow(proj.rb.velocity.y, 2) - (2f * Physics.gravity.y * -(landingPlaneHeight - proj.transform.position.y))))) / Physics.gravity.y;
            //Debug.Log("Falling projectile analysis for " + proj.gameObject.name + ": timeToGround = " + timeToGround + 
                //", y0 = " + proj.transform.position.y + ", Vy0 = " + proj.rb.velocity.y);
        }

        Vector3 landingLocation = new Vector3(
            proj.rb.velocity.x * timeToGround + proj.rb.position.x,
            landingPlaneHeight,
            proj.rb.velocity.z * timeToGround + proj.rb.position.z);

        float distance = Vector3.Distance(landingLocation, currentCatcherPosition);
        float timeToCatch = distance / moveSpeed;

        ProjectileTrajectoryData projectileData = new ProjectileTrajectoryData();
        projectileData.landingTimestamp = timeToGround + Time.time;
        projectileData.landingLocation = landingLocation;
        projectileData.catchable = IsCatchable(projectileData, currentCatcherPosition);

        return projectileData;
    }

    private bool IsCatchable(ProjectileTrajectoryData data, Vector3 currentCatcherPosition)
    {
        float distance = Vector3.Distance(data.landingLocation, currentCatcherPosition);
        float catchTimestamp = (distance / moveSpeed) + Time.time;

        //Check that the electron is reachable before it falls, and that it will fall within level bounds
        return catchTimestamp < data.landingTimestamp &&
            data.landingLocation.x > electronLauncher.electronSpawn.position.x &&
            data.landingLocation.x < batteryDockPos.x;
    }

    private void EvaluateTargets()
    {
        //Heuristic -- target the lowest landing time as long as it is catchable

        if (analyzedElectrons.Count == 0)
            return;

        //Update catchability based on the cached data from the last analysis
        foreach (var pair in analyzedElectrons)
        {
            ProjectileTrajectoryData data = pair.Value;
            data.catchable = IsCatchable(data, localT.localPosition);
            //analyzedElectrons[pair.Key] = data;
        }

        var minimalCatchable = analyzedElectrons.Aggregate((minItem, nextItem) => 
            //nextItem.Value.catchable && 
            IsCatchable(nextItem.Value, localT.localPosition) &&
            (nextItem.Value.landingTimestamp < minItem.Value.landingTimestamp) ? nextItem : minItem);

        movementTargetObj = minimalCatchable.Key.gameObject; //TODO: this can be null when the electron is being destroyed!?
        SetMovementPoint(minimalCatchable.Value.landingLocation);
    }

    private void SetMovementPoint(Transform targetTransform)
    {
        SetMovementPoint(targetTransform.position);
        movementTargetObj = targetTransform.gameObject;
    }

    private void SetMovementPoint(Vector3 targetPosition)
    {
        catchMoveTarget = targetPosition;
        catchMoveTarget.x += halfCollectionWidth;
        catchMoveTarget.y = yMovementPlane;

        turnAngle = GetAngleToTarget(catchMoveTarget);
    }

    private void UpdateElectronCatchMovement()
    {
        MoveTowards(catchMoveTarget);
        StepRotation(electronLauncher.transform.position);
    }

    private void UpdateBatteryDepositMovement()
    {
        MoveTowards(batteryDockPos);
        StepRotation(battery.transform.position);

        blackboard[DISTANCE_BATTERY] = Vector3.Distance(localT.position, batteryDockPos);
    }

    private void MoveTowards(Vector3 target)
    {
        Vector3 movementStep = Vector3.MoveTowards(localT.position, target, (moveSpeed * Time.deltaTime));
        rb.MovePosition(movementStep);
    }

    private void StepRotation(Vector3 destination)
    {
        //Calculate one step worth of angular rotation and apply it
        float rotateStep = rotateSpeed * Time.deltaTime;
        Vector3 targetDirection = destination - localT.position;
        targetDirection.y = localT.position.y;

        Vector3 lookDirection = Vector3.RotateTowards(localT.forward, targetDirection, rotateStep, 0f);
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        lookRotation.x = 0;
        lookRotation.z = 0;

        localT.rotation = lookRotation;
    }

    private float GetAngleToTarget(Vector3 target)
    {
        //Calculate the full angle to the target
        Vector3 targetDirection = target - localT.position;
        targetDirection.y = localT.position.y;

        Vector3 lookDirection = Vector3.RotateTowards(localT.forward, targetDirection, 2f, 0f);
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        lookRotation.x = 0;
        lookRotation.z = 0;

        float angle = Quaternion.Angle(localT.rotation, lookRotation);

        //Debug.Log("Get Angle: " + angle);
        return angle;
    }
}
