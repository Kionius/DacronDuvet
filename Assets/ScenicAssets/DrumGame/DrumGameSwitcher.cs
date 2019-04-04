using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UniRx;

/// <summary>
/// Switches between active DrumGameManager instances to coordinate alternating on-screen patterns
/// </summary>
public class DrumGameSwitcher : MonoBehaviour {

    public IntReactiveProperty activeManagerIndex;
    public IntReactiveProperty globalLevelIndex;
    public KeyCode refreshCurrentLevelKey;
    public KeyCode skipToNextSequenceKey = KeyCode.K;
    public int numInstances;
    public DrumGameManagerCircle circleManagerTemplate;
    public DrumGameManagerLine lineManager;
    public List<DrumGameLevel> levels = new List<DrumGameLevel>();
    public List<Color> colorMap;

    //These events can be subscribed to from other classes to add behavior that reacts to DrumGame's gameplay
    //After managers are initialized, the Switcher provides each manager callbacks which let them trigger these events
    //This protects external classes from worrying about which Manager instance is active
    protected UnityEvent onProgress;
    protected UnityEvent onFinishRepetition;
    protected UnityEvent onMistake;

    //TODO: add a list of position offsets for instances of split UI. for now let's limit it to 4 instances

    private List<DrumGameManagerCircle> circleManagers;
    private int numVisibleManagers = 0;

    private void Awake()
    {
        activeManagerIndex = new IntReactiveProperty();
        globalLevelIndex = new IntReactiveProperty();

        onProgress = new UnityEvent();
        onFinishRepetition = new UnityEvent();
        onMistake = new UnityEvent();

        if (circleManagerTemplate != null)
            InstantiateCircleManagers();

        if (lineManager != null)
            InitializeLineManager();

        globalLevelIndex.Subscribe(index => PushSequencesToManagers(index));
        globalLevelIndex.Subscribe(_ => numVisibleManagers = CountVisibleManagers());

        activeManagerIndex.Subscribe(i => SetExclusiveActiveManager(i));

#if UNITY_EDITOR
        DrumGameLineEditor lineEditor = GetComponentInChildren<DrumGameLineEditor>();
        if (lineEditor != null)
        {
            lineEditor.SetRefreshDelegate(RefreshCurrentLevel);
        }
#endif
    }

    public void LoadDrumLevel(DrumGameLevel level)
    {
        //TODO: this doesn't address the List nature of the level storage at the Switcher,
        //but this method is a temporary stand-in for switching to a particle-firing mode
        //for the call and response section

        levels = new List<DrumGameLevel>();
        levels.Add(level);

        globalLevelIndex.SetValueAndForceNotify(0);
    }

    public void SubscribeToProgressEvent(UnityAction action)
    {
        onProgress.AddListener(action);
    }

    public void SubscribeToFinishRepetitionEvent(UnityAction action)
    {
        onFinishRepetition.AddListener(action);
    }

    public void SubscribeToMistakeEvent(UnityAction action)
    {
        onMistake.AddListener(action);
    }

    private void PushSequencesToManagers(int sequenceIndex)
    {
        //Lines are a special case -- they'll only work in the first Level of the collection
        if (levels[0].sequences[sequenceIndex].type == DrumSequence.SequenceType.Line)
        {
            activeManagerIndex.Value = -1;

            if (lineManager != null)
                lineManager.LoadSequence(levels[0].sequences[sequenceIndex]);

            for (int i = 0; i < circleManagers.Count; i++)
                circleManagers[i].isVisible.Value = false;
        }
        else
        {
            activeManagerIndex.Value = 0;

            for (int i = 0; i < circleManagers.Count; i++)
                circleManagers[i].LoadSequence(levels[i].sequences[sequenceIndex]);

            if (lineManager != null)
                lineManager.isVisible.Value = false;
        }
    }

    private int CountVisibleManagers()
    {
        int visibleCount = 0;
        for (int i = 0; i < circleManagers.Count; i++)
        {
            if (circleManagers[i].isVisible.Value)
                visibleCount++;
        }

        if (lineManager != null && lineManager.isActive.Value)
            visibleCount++;

        return visibleCount;
    }

    private void SetExclusiveActiveManager(int index)
    {
        for (int i = 0; i < circleManagers.Count; i++)
            circleManagers[i].isActive.Value = (index == i);

        if (lineManager != null)
            lineManager.isActive.Value = (index == -1);
    }

    private IEnumerator ToggleActivationOnNextFrame(int index)
    {
        yield return null;

        for (int i = 0; i < circleManagers.Count; i++)
            circleManagers[i].isActive.Value = (index == i);

        lineManager.isActive.Value = (index == -1);
    }

    private void InstantiateCircleManagers()
    {
        circleManagers = new List<DrumGameManagerCircle>();

        for (int i = 0; i < numInstances; i++)
        {
            GameObject managerObj = Instantiate(circleManagerTemplate.gameObject, this.transform);
            managerObj.name = $"DrumGameManager {i}";
            DrumGameManagerCircle manager = managerObj.GetComponent<DrumGameManagerCircle>();
            circleManagers.Add(manager);

            manager.Initialize(levels[i].sequences[0], i, OnSequenceComplete, colorMap);

            //HACK: hardcoded positions for two active sequences
            //TODO: move position manipulation method to trigger when new Managers are hidden/revealed, sorting out
            //available screen space appropriately
            if (i == 0)
            {
                managerObj.transform.localPosition = managerObj.transform.localPosition;
            }
            else if (i == 1)
            {
                managerObj.transform.localPosition = managerObj.transform.localPosition + new Vector3(700, 0, 0);
            }

            manager.SetEventTriggerCallbacks(InvokeOnProgress, InvokeOnFinishRepetition, InvokeOnMistake);
        }

        //Hide the template
        circleManagerTemplate.gameObject.SetActive(false);
    }

    private void InitializeLineManager()
    {
        //Valid but empty state so the LineManager hides itself until loaded
        DrumSequence nullSequence = new DrumSequence();
        nullSequence.keys = new List<DrumSequence.DrumKey>();
        nullSequence.repetitions = 0;
        nullSequence.coords = new List<Vector3> { Vector3.zero };

        lineManager.Initialize(nullSequence, -1, OnSequenceComplete, colorMap);

        lineManager.SetEventTriggerCallbacks(InvokeOnProgress, InvokeOnFinishRepetition, InvokeOnMistake);
    }

    private void InvokeOnProgress()
    {
        onProgress.Invoke();
    }

    private void InvokeOnFinishRepetition()
    {
        onFinishRepetition.Invoke();
    }

    private void InvokeOnMistake()
    {
        onMistake.Invoke();
    }

    private void Update()
    {
        if (Input.GetKeyDown(refreshCurrentLevelKey))
            RefreshCurrentLevel();
        if (Input.GetKeyDown(skipToNextSequenceKey))
            AdvanceGlobalLevelIndex();
    }

    private void RefreshCurrentLevel()
    {
        activeManagerIndex.SetValueAndForceNotify(activeManagerIndex.Value);
        globalLevelIndex.SetValueAndForceNotify(globalLevelIndex.Value);
    }

    private void OnSequenceComplete(int managerIndex, int repeatsRemaining)
    {
        if (numVisibleManagers > 1)
        {
            if (activeManagerIndex.Value == numVisibleManagers - 1)
                activeManagerIndex.Value = 0;
            else
                activeManagerIndex.Value++;
        }

        if (
            managerIndex == -1 //Line
            || ((managerIndex == numVisibleManagers - 1) && repeatsRemaining == 0) //Last repeat of a circle
            )
        {
            AdvanceGlobalLevelIndex();
        }
    }

    private void AdvanceGlobalLevelIndex()
    {
        if (globalLevelIndex.Value == levels[0].sequences.Count - 1)
            globalLevelIndex.Value = 0;
        else
            globalLevelIndex.Value++;

        //TODO: see if we can move this somewhere cleaner like PushSequencesToManagers
        if (levels[0].sequences[globalLevelIndex.Value].type == DrumSequence.SequenceType.Line)
        {
            activeManagerIndex.Value = -1;
        }
    }
}
