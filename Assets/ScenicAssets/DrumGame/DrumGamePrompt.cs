using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrumGamePrompt : MonoBehaviour {

    //Eventually, this will just show colors, maybe shapes...? 
    //For now we're going to display a text label with a number or a letter

    //This object should be loaded by prefab from the DrumGameManager as it loads a sequence
    //and is a reactive property of the sequence object itself

    public Text text;
    public Image backgroundImage;
    public Image highlightFrame;
    public float errorAnimTime = 0.5f;

    private int selfIndex; //Index in the sequence and Manager's object pool

    private Coroutine flashColorAnim;
    private bool animating = false;
    private bool queuedVisibleFlag = false;

    //TODO: bug when loading a parallel circle manager where all of the backgrounds are
    //lit up. should be off by default except for the first one
    //Also getting the bug where swapping active status to the second circle throws the error
    //animation on the second circle for that same frame's input

    public void SetText(string keyLabel)
    {
        text.text = keyLabel;
    }

    public void SetColor(Color c)
    {
        backgroundImage.color = c;
    }

    public void SetHighlightVisible(bool isVisible)
    {
        queuedVisibleFlag = isVisible;

        //TODO: add animation to this
        if (!isVisible && animating)
            DisableAfterAnimation();

        else
            highlightFrame.gameObject.SetActive(isVisible);
    }

    public int GetIndex()
    {
        return selfIndex;
    }

    public void SetIndex(int index)
    {
        selfIndex = index;
    }

    public void StartErrorAnimation()
    {
        if (flashColorAnim != null)
            StopCoroutine(flashColorAnim);

        flashColorAnim = StartCoroutine(FlashColor(Color.white, Color.red, Color.white));
    }

    public void FlashColorAnimation(Color baseColor, Color highlightColor, Color postColor)
    {
        if (flashColorAnim != null)
            StopCoroutine(flashColorAnim);

        flashColorAnim = StartCoroutine(FlashColor(baseColor, highlightColor, postColor));
    }

    private IEnumerator FlashColor(Color baseColor, Color highlightColor, Color postColor)
    {
        animating = true;
        float halfDuration = errorAnimTime / 2f;
        float timer = 0f;

        while (timer < halfDuration)
        {
            float t = timer / halfDuration;
            Color c = Color.Lerp(baseColor, highlightColor, t);
            highlightFrame.color = c;

            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;

        if (queuedVisibleFlag)
        {   //Fade back to base
            while (timer < halfDuration)
            {
                float t = timer / halfDuration;
                Color c = Color.Lerp(highlightColor, baseColor, t);
                highlightFrame.color = c;

                timer += Time.deltaTime;
                yield return null;
            }
        }
        else
        {   //Fade alpha out
            Color transparentOut = highlightColor;
            transparentOut.a = 0;

            while (timer < halfDuration)
            {
                float t = timer / halfDuration;
                Color c = Color.Lerp(highlightColor, transparentOut, t);
                highlightFrame.color = c;

                timer += Time.deltaTime;
                yield return null;
            }
        }
        

        highlightFrame.color = postColor;

        animating = false;
    }

    private void DisableAfterAnimation()
    {
        StartCoroutine(AwaitAnimEnd());
    }

    private IEnumerator AwaitAnimEnd()
    {
        while (animating)
            yield return null;

        highlightFrame.gameObject.SetActive(queuedVisibleFlag);

        //Restore transparency in case it was faded out
        Color c = highlightFrame.color;
        c.a = 1;
        highlightFrame.color = c;
    }
}
