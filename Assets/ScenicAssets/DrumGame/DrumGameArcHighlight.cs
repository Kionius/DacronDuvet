using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies color intensity changes to highlight arc / line segments
/// </summary>
public class DrumGameArcHighlight : MonoBehaviour {

    private static float DEFAULT_INTENSITY = 1f;

    public GameObject arcSegmentPrefab;
    public Color arcColor;
    public float highlightTime = 0.5f;
    public float highlightIntensity = 2.5f;
    public int maxNumArcs = 8;

    private List<GameObject> arcObjects;
    private List<Material> arcMats;

    private List<Coroutine> animCoroutines;

    public void Initialize()
    {
        if (arcSegmentPrefab != null)
            InstantiateArcs();
        else
            Debug.LogError("DrumGameArcHighlight needs an Arc Segment prefab assigned!");

        //DrawArcs(4);

        InstantiateEmptyCoroutines();
    }

    void InstantiateArcs()
    {
        arcObjects = new List<GameObject>(maxNumArcs);
        arcMats = new List<Material>(maxNumArcs);

        Material templateMat = arcSegmentPrefab.GetComponent<Image>().material;

        for (int i = 0; i < maxNumArcs; i++)
        {
            GameObject arcClone = Instantiate(arcSegmentPrefab, this.transform);
            arcObjects.Add(arcClone);

            //Clone the template mat
            Material mat = Instantiate(templateMat);
            mat.CopyPropertiesFromMaterial(templateMat);
            mat.SetColor("_Color", arcColor);

            //Use the cloned mat, discarding the ref to the original
            Image img = arcClone.GetComponent<Image>();
            img.material = mat;
            arcMats.Add(mat);

            //Turn off everything as it clones, a la light object pooling
            arcClone.SetActive(false);
        }
    }

    public void DrawArcs(int numArcs)
    {
        for (int i = 0; i < arcObjects.Count; i++)
            arcObjects[i].SetActive(false);

        for (int i = 0; i < numArcs; i++)
        {
            //Enable and rotate to the proper position along the circle
            arcObjects[i].SetActive(true);
            Vector3 eulerAngle = new Vector3(0f, 0f, -360f / numArcs * i);
            Quaternion nextRotation = Quaternion.Euler(eulerAngle);
            arcObjects[i].transform.rotation = nextRotation;

            //Adjust the fill property of the fragment shader
            arcMats[i].SetFloat("_Fill", 1f / numArcs);
        }
    }

    void InstantiateEmptyCoroutines()
    {
        animCoroutines = new List<Coroutine>();

        for (int i = 0; i < arcMats.Count; i++)
            animCoroutines.Add(StartCoroutine(NullCoroutine()));
    }

    //TODO: this sucks. surely there's a better way to avoid array access errors at the start of the scene
    IEnumerator NullCoroutine()
    {
        yield return null;
    }

    public void FlashSegmentHighlight(int segmentNum)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (animCoroutines[segmentNum] != null)
            StopCoroutine(animCoroutines[segmentNum]);

        animCoroutines[segmentNum] = StartCoroutine(FlashAnimation(segmentNum));
    }

    private IEnumerator FlashAnimation(int segmentNum)
    {
        float halfDuration = highlightTime / 2f;
        float timer = 0f;

        //Intensity up
        //while (timer < halfDuration)
        //{
        //    float t = timer / halfDuration;
        //    float intensity = Mathf.Lerp(DEFAULT_INTENSITY, highlightIntensity, t);

        //    segmentMats[segmentNum].SetFloat("_ColorIntensity", intensity);

        //    timer += Time.deltaTime;
        //    yield return null;
        //}

        //timer = 0f;

        //Intensity down
        while (timer < halfDuration)
        {
            float t = timer / halfDuration;
            float intensity = Mathf.Lerp(highlightIntensity, DEFAULT_INTENSITY, t);

            arcMats[segmentNum].SetFloat("_ColorIntensity", intensity);

            timer += Time.deltaTime;
            yield return null;
        }

        //Snap to 1f when finished
        arcMats[segmentNum].SetFloat("_ColorIntensity", DEFAULT_INTENSITY);
    }

    public void SnapHighlightsToDefault()
    {
        for (int i = 0; i < arcMats.Count; i++)
            arcMats[i].SetFloat("_ColorIntensity", DEFAULT_INTENSITY);
    }
}
