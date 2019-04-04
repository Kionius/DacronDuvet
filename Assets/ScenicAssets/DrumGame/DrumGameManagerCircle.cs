using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEngine.UI;
using UnityEngine.Events;

public class DrumGameManagerCircle : DrumGameManagerBase
{
    /* TODOLIST
     */

    public Transform wheelPivotT;
    public Transform highlightPivotT;
    public float rotateTime = 0.5f;
    public DrumGameArcHighlight arcHighlighter;

    public Text repeatsText;

    private GameObject uiParent;

    private Coroutine wheelAnimation;

    private float baseX;
    private float baseY;

    private IntReactiveProperty repeatsRemaining; //iterations left before advancing levelIndex

    public override void Initialize(DrumSequence firstSequence, int selfIndex, OnSequenceComplete sequenceCallback, List<Color> colorMap)
    {
        this.selfIndex = selfIndex;
        sequenceCompleteCallback = sequenceCallback;

        uiParent = transform.GetChild(0).gameObject;

        prompts = new List<DrumGamePrompt>();
        promptPrefab.SetActive(false);

        this.colorMap = colorMap;

        arcHighlighter.Initialize();

        reactiveSequence = new ReactiveProperty<DrumSequence>(firstSequence);
        repeatsRemaining = new IntReactiveProperty(reactiveSequence.Value.repetitions);
        sequenceIndex = new IntReactiveProperty(0);

        isVisible = new BoolReactiveProperty();
        isActive = new BoolReactiveProperty();

        isActive.Subscribe(active => ToggleActivePromptHighlight(active));
        isActive.Subscribe(active => firstFrameActive = active);
        isVisible.Subscribe(visible => SetVisibility(visible));

        reactiveSequence.Subscribe(sequence => SetHiddenState(sequence));
        reactiveSequence.Subscribe(sequence => SpawnAndArrangePrompts(sequence));
        reactiveSequence.Subscribe(sequence => LabelDrumPrompts(sequence));
        reactiveSequence.Subscribe(sequence => ColorDrumPrompts(sequence));
        reactiveSequence.Subscribe(sequence => SetRepeatsValue(sequence));
        reactiveSequence.Subscribe(sequence => arcHighlighter.DrawArcs(sequence.keys.Count));

        sequenceIndex.Subscribe(index => SetPromptHighlight(index));

        repeatsRemaining.Subscribe(remaining => SetRepeatsText(remaining));

        baseX = highlightPivotT.rotation.eulerAngles.x;
        baseY = highlightPivotT.rotation.eulerAngles.y;
    }

    public virtual void SetVisibility(bool isVisible)
    {
        //TODO: animate hide/unhide toggles
        arcHighlighter.SnapHighlightsToDefault();
        uiParent.SetActive(isVisible);
    }

    protected override void SpawnAndArrangePrompts(DrumSequence next)
    {
        if (next.type != DrumSequence.SequenceType.Circle)
            return;

        //Add needed prompts
        while (prompts.Count < next.keys.Count)
        {
            GameObject promptObj = Instantiate(promptPrefab, wheelPivotT);
            promptObj.SetActive(true);
            DrumGamePrompt prompt = promptObj.GetComponent<DrumGamePrompt>();
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

        //Arrange around a circle
        float radius = promptPrefab.transform.localPosition.y;
        for (int i = 0; i < prompts.Count; i++)
        {
            float phaseRadians = ((i / (float)prompts.Count) * 360f) * Mathf.Deg2Rad;
            float x = radius * Mathf.Sin(phaseRadians);
            float y = radius * Mathf.Cos(phaseRadians);

            prompts[i].transform.localPosition = new Vector3(x, y, 0);
        }
    }

    protected override void SetRepeatsValue(DrumSequence next)
    {
        repeatsRemaining.Value = next.repetitions;
    }

    private void SetRepeatsText(int remaining)
    {
        repeatsText.text = $"{remaining}x";
    }

    protected override void AdvanceSequence()
    {
        arcHighlighter.FlashSegmentHighlight(sequenceIndex.Value);

        if (sequenceIndex.Value == reactiveSequence.Value.keys.Count - 1)
        {
            sequenceIndex.Value = 0;
            repeatsRemaining.Value--;

            sequenceCompleteCallback(selfIndex, repeatsRemaining.Value);
            onFinishRepetitionCallback.Invoke();
        }
            
        else
        {
            sequenceIndex.Value++;

            onProgressCallback.Invoke();
        }
            
        //ApplyWheelRotation(GetSequenceProgress(), true);
    }

    protected override void ResetSequence()
    {
        AnimateErrorAtCurrentPrompt();
        sequenceIndex.Value = 0;

        //ApplyWheelRotation(GetSequenceProgress(), false);
    }

    private void ApplyWheelRotation(float sequenceProgress, bool shouldSpinClockwise)
    {
        if (!gameObject.activeInHierarchy)
            return;

        float targetEulerZ = GetWheelZRotation(sequenceProgress);

        if (wheelAnimation != null)
            StopCoroutine(wheelAnimation);

        wheelAnimation = StartCoroutine(AnimateWheelRotation(targetEulerZ, shouldSpinClockwise));
    }

    private float GetWheelZRotation(float sequenceProgress)
    {
        return sequenceProgress * 360f;
    }

    private IEnumerator AnimateWheelRotation(float endEuler, bool spinClockwise)
    {
        float startEuler = highlightPivotT.rotation.eulerAngles.z;

        if (endEuler < startEuler && spinClockwise)
        {
            //here's the hack: treat a successful return to 0 as a movement to 360
            //so it must go clockwise. When we apply the rotation, we should snap to 0 instantly as we finish the timer

            endEuler += 360f;
        }

        //Use Lerp with the rotation hack above to trick Unity into making the choice we want -_-
        Quaternion endRotation = Quaternion.Euler(baseX, baseY, endEuler);
        float time = 0f;
        while (time < rotateTime)
        {
            float t = time / rotateTime;

            highlightPivotT.rotation = Quaternion.Euler(baseX, baseY, Mathf.Lerp(startEuler, endEuler, t));

            time += Time.deltaTime;
            yield return null;
        }

        //Snap to the ideal rotation, as deltaTime can miss the mark
        if (endEuler >= 360f)
            endEuler -= 360f;

        highlightPivotT.rotation = Quaternion.Euler(baseX, baseY, endEuler);
    }
}
