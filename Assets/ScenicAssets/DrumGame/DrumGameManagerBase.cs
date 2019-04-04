using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEngine.Events;
using UnityEngine.UI;

public class DrumGameManagerBase : MonoBehaviour
{
    public GameObject promptPrefab;

    protected List<DrumGamePrompt> prompts;

    protected ReactiveProperty<DrumSequence> reactiveSequence;

    protected int selfIndex;
    public delegate void OnSequenceComplete(int managerIndex, int repetitionsRemaining);
    protected OnSequenceComplete sequenceCompleteCallback;

    protected UnityAction onProgressCallback;
    protected UnityAction onFinishRepetitionCallback;
    protected UnityAction onMistakeCallback;

    public BoolReactiveProperty isVisible;
    public BoolReactiveProperty isActive;

    protected IntReactiveProperty sequenceIndex; //location in the current DrumSequence
    protected List<Color> colorMap;
    protected bool firstFrameActive = false;
    protected bool editing = false;

    public virtual void Initialize(
        DrumSequence firstSequence, 
        int selfIndex, 
        OnSequenceComplete sequenceCallback,
        List<Color> colorMap)
        { }

    public void LoadSequence(DrumSequence next)
    {
        //NOTE: ForceNotify is very useful here, in case [next] is the same as the current sequence!
        //We may need to add many more ForceNotify calls around these managers
        //Bruce says making the sequence an Observable instead of a ReactiveProperty would also work
        reactiveSequence.SetValueAndForceNotify(next);
        sequenceIndex.SetValueAndForceNotify(0);
    }

    /// <summary>
    /// Assign callbacks from the DrumGameSwitcher allowing each Manager to trigger individual Switcher events
    /// </summary>
    public void SetEventTriggerCallbacks(
        UnityAction progressTrigger,
        UnityAction finishRepetitionTrigger,
        UnityAction mistakeTrigger
        )
    {
        onProgressCallback = progressTrigger;
        onFinishRepetitionCallback = finishRepetitionTrigger;
        onMistakeCallback = mistakeTrigger;
    }

    protected void SetHiddenState(DrumSequence next)
    {
        isVisible.Value = (next.keys.Count > 0 || next.repetitions > 0);
    }

    /// <summary>
    /// Trigger error animation at current sequenceIndex.
    /// Should be called before assigning new sequenceIndex value
    /// </summary>
    protected void AnimateErrorAtCurrentPrompt()
    {
        prompts[sequenceIndex.Value].StartErrorAnimation();      
    }

    protected void SetPromptHighlight(int sequenceNum)
    {
        for (int i = 0; i < prompts.Count; i++)
            prompts[i].SetHighlightVisible(i == sequenceNum);
    }

    protected void ToggleActivePromptHighlight(bool isActive)
    {
        for (int i = 0; i < prompts.Count; i++)
            prompts[i].SetHighlightVisible(i == sequenceIndex.Value && isActive);
    }

    protected virtual void SpawnAndArrangePrompts(DrumSequence next) { }

    protected void LabelDrumPrompts(DrumSequence next)
    {
        for (int i = 0; i < prompts.Count; i++)
            prompts[i].SetText(((int)next.keys[i]).ToString());
    }

    protected void ColorDrumPrompts(DrumSequence next)
    {
        for (int i = 0; i < prompts.Count; i++)
        {
            int keyCode = (int)next.keys[i] - 1; //Enum is 1-indexed
            prompts[i].SetColor(colorMap[keyCode]);
        }
    }

    protected virtual void SetRepeatsValue(DrumSequence next) { }

    private void Update()
    {
        if (firstFrameActive)
        {
            //Is there a better way to skip inputs on the first active frame?
            //This fixes input issues when swapping Managers and works well with animation states
            firstFrameActive = false;
            return;
        }

        if (isActive.Value && !editing)
        {
            CheckDrumHit();
            //CheckDrumHitMidi();
        }
    }

    protected virtual void AdvanceSequence() { }

    protected virtual void ResetSequence() { }

    public float GetSequenceProgress()
    {
        return sequenceIndex.Value / (float)reactiveSequence.Value.keys.Count;
    }

    private void CheckDrumHit()
    {
        DrumSequence.DrumKey hit = DrumSequence.DrumKey.NONE;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            hit = DrumSequence.DrumKey.One;
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            hit = DrumSequence.DrumKey.Two;
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            hit = DrumSequence.DrumKey.Three;
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            hit = DrumSequence.DrumKey.Four;
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            hit = DrumSequence.DrumKey.Five;
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            hit = DrumSequence.DrumKey.Six;
        else if (Input.GetKeyDown(KeyCode.Alpha7))
            hit = DrumSequence.DrumKey.Seven;

        if (hit != DrumSequence.DrumKey.NONE)
        {
            if (hit == reactiveSequence.Value.GetCurrentDrumKey(sequenceIndex.Value))
                AdvanceSequence();
            else
            {
                onMistakeCallback.Invoke();
                ResetSequence();
            }
        }
    }

    private void CheckDrumHitMidi()
    {
        DrumSequence.DrumKey hit = DrumSequence.DrumKey.NONE;

        hit = DrumInputMIDIMap.ReadDrumKeyDown();

        if (hit != DrumSequence.DrumKey.NONE)
        {
            if (hit == reactiveSequence.Value.GetCurrentDrumKey(sequenceIndex.Value))
                    AdvanceSequence();
                else
                    ResetSequence();
        }
    }
}
