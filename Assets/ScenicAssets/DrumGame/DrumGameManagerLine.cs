using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEngine.UI;

public class DrumGameManagerLine : DrumGameManagerBase
{
    /* TODOLIST
     */

    public DrumGameSegmentHighlight segmentHighlighter;
    public Transform promptParent;
    public float cameraMoveDuration = 1f;
    public float cameraZoomoutDuration = 3f; //Might be individually defined per Sequence
    public float cameraZoomoutHold = 3f;
    public AnimationCurve cameraEaseCurve;
    public KeyCode toggleEditingKey;
    public DrumGameLineEditor lineEditor;

    private GameObject uiParent;
    private Vector3 originalCameraPosition;
    private Camera mainCamera;
    private Coroutine cameraFollowCoroutine;
    private Coroutine cameraZoomingOutCoroutine;

    public override void Initialize(DrumSequence firstSequence, int selfIndex, OnSequenceComplete sequenceCallback, List<Color> colorMap)
    {
        this.selfIndex = selfIndex;
        sequenceCompleteCallback = sequenceCallback;

        uiParent = transform.GetChild(0).gameObject;

        mainCamera = Camera.main;
        originalCameraPosition = mainCamera.transform.localPosition;

        segmentHighlighter.Initialize();

        prompts = new List<DrumGamePrompt>();
        promptPrefab.SetActive(false);

        this.colorMap = colorMap;

        reactiveSequence = new ReactiveProperty<DrumSequence>(firstSequence);
        sequenceIndex = new IntReactiveProperty(0);

        isVisible = new BoolReactiveProperty();
        isActive = new BoolReactiveProperty();

        isActive.Subscribe(active => ToggleActivePromptHighlight(active));
        isActive.Subscribe(active => firstFrameActive = active);
        isActive.Subscribe(active => SetCamToOriginOnDisable(active));

        isVisible.Subscribe(visible => SetVisibility(visible));

        reactiveSequence.Subscribe(sequence => SetHiddenState(sequence));
        reactiveSequence.Subscribe(sequence => SpawnAndArrangePrompts(sequence));
        reactiveSequence.Subscribe(sequence => LabelDrumPrompts(sequence));
        reactiveSequence.Subscribe(sequence => ColorDrumPrompts(sequence));
        reactiveSequence.Subscribe(sequence => SetRepeatsValue(sequence));
        reactiveSequence.Subscribe(delegate { if (cameraZoomingOutCoroutine != null) StopCoroutine(cameraZoomingOutCoroutine); });
        if (lineEditor != null)
            reactiveSequence.Subscribe(sequence => lineEditor.SetSequence(sequence));

        sequenceIndex.Subscribe(index => SetPromptHighlight(index));
        sequenceIndex.Subscribe(index => AnimateCameraFollow(index));

        var toggleEditStream = Observable.EveryUpdate().Where(_ => Input.GetKeyDown(toggleEditingKey));
        toggleEditStream.Subscribe(_ => editing = !editing);
        toggleEditStream.Subscribe(_ => lineEditor.isEditing.Value = editing);
        toggleEditStream.Subscribe(_ => Debug.Log($"Line mode Editing set to {editing}"));

        if (lineEditor != null)
            lineEditor.SetPromptsRef(prompts);
    }

    public void SetVisibility(bool isVisible)
    {
        //TODO: animate hide/unhide toggles
        uiParent.SetActive(isVisible);
    }

    protected override void SpawnAndArrangePrompts(DrumSequence next)
    {
        if (next.type != DrumSequence.SequenceType.Line)
            return;

        //Add needed prompts
        while (prompts.Count < next.keys.Count)
        {
            GameObject promptObj = Instantiate(promptPrefab, this.transform);
            promptObj.SetActive(true);
            promptObj.transform.SetParent(promptParent.transform);
            DrumGamePrompt prompt = promptObj.GetComponent<DrumGamePrompt>();
            prompt.SetIndex(prompts.Count);
            prompt.SetHighlightVisible(false);
            prompts.Add(prompt);
        }

        //Remove extras
        while (prompts.Count > next.keys.Count)
        {
            DrumGamePrompt last = prompts[prompts.Count - 1];
            prompts.Remove(last);
            Destroy(last.gameObject);
        }

        //Arrange according to coordinate info

        for (int i = 0; i < prompts.Count; i++)
        {   
            prompts[i].transform.localPosition = next.coords[i];
        }

        segmentHighlighter.DrawSegments(next.coords, next.showLine);
    }

    protected override void SetRepeatsValue(DrumSequence next)
    {
        //Unused by this class
    }

    protected override void AdvanceSequence()
    {
        segmentHighlighter.PermanentSegmentHighlight(sequenceIndex.Value);

        if (sequenceIndex.Value == reactiveSequence.Value.keys.Count - 1)
        {
            //Delay the sequence completion -- placed instead at the end of a coroutine that shows the shape in a camera zoomout
            StartZoomout();

            //This callback might make more sense at the end of the zoomout coroutine depending on use case
            onFinishRepetitionCallback.Invoke();
            return;
        }

        else
        {
            sequenceIndex.Value++;

            onProgressCallback.Invoke();
        }
    }

    private void StartZoomout()
    {
        if (cameraZoomingOutCoroutine != null)
            StopCoroutine(cameraZoomingOutCoroutine);

        if (cameraFollowCoroutine != null)
            StopCoroutine(cameraFollowCoroutine);

        cameraZoomingOutCoroutine = StartCoroutine(ZoomingOut());
    }

    private IEnumerator ZoomingOut()
    {
        float timer = 0f;
        Vector3 lastSequencePosition = mainCamera.transform.localPosition;

        while (timer < cameraZoomoutDuration)
        {
            float t = timer / cameraZoomoutDuration;
            Vector3 destination = Vector3.Lerp(lastSequencePosition, reactiveSequence.Value.zoomoutPosition, t);
            mainCamera.transform.position = destination;

            timer += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = reactiveSequence.Value.zoomoutPosition;

        //Wait additional time before changing sequence
        timer = 0f;
        while (timer < cameraZoomoutHold)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        sequenceIndex.Value = 0;
        sequenceCompleteCallback(selfIndex, 0);
    }

    protected override void ResetSequence()
    {
        //Line style should have the option to NOT reset the sequence on an error
        //For now this will be the default behavior, but we should still animate the error

        AnimateErrorAtCurrentPrompt();
    }

    private void SetCamToOriginOnDisable(bool active)
    {
        if (!active)
            mainCamera.transform.localPosition = originalCameraPosition;
    }

    private void AnimateCameraFollow(int destinationIndex)
    {
        if (editing)
            return;

        if (cameraFollowCoroutine != null)
            StopCoroutine(cameraFollowCoroutine);

        if (destinationIndex == 0)
        {
            Vector3 destination = reactiveSequence.Value.coords[0];
            destination.z = originalCameraPosition.z;
            mainCamera.transform.localPosition = destination;
            return;
        }

        cameraFollowCoroutine = StartCoroutine(CameraFollowing(destinationIndex));
    }

    private IEnumerator CameraFollowing(int destinationIndex)
    {
        float timer = 0f;
        Vector3 startPosition = mainCamera.transform.localPosition;
        Vector3 destination;

        while (timer < cameraMoveDuration)
        {
            float t = timer / cameraMoveDuration;
            t = cameraEaseCurve.Evaluate(t);
            destination = Vector3.Lerp(startPosition, reactiveSequence.Value.coords[destinationIndex], t);
            destination.z = originalCameraPosition.z;
            mainCamera.transform.localPosition = destination;

            timer += Time.deltaTime;
            yield return null;
        }

        destination = reactiveSequence.Value.coords[destinationIndex];
        destination.z = originalCameraPosition.z;
        mainCamera.transform.localPosition = destination;
    }
}
