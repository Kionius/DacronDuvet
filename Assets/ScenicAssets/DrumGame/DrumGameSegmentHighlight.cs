using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DrumGameSegmentHighlight : MonoBehaviour {

    private static float DEFAULT_INTENSITY = 1f;

    public float flickerDuration = 0.5f;
    public float flickerMinIntensity = 2.5f;
    public float flickerMaxIntensity = 3.0f;

    public Sprite segmentSprite;
    public Material segmentMat;

    public Transform segmentParent;

    private List<Material> segmentMats;
    private List<GameObject> segmentObjs;
    //private List<Coroutine> animCoroutines; //This animation style won't need to come back for old coroutines

    public void Initialize()
    {
        segmentMats = new List<Material>();
        segmentObjs = new List<GameObject>();
    }

    public void DrawSegments(List<Vector3> coords, List<bool> showLine)
    {
        int i = 0;
        for (i = 0; i < coords.Count - 1; i++)
        {
            //Spawn a segment and a material if there aren't enough allocated
            GameObject segObj;
            if (i < segmentObjs.Count)
            {
                segObj = segmentObjs[i];
                segObj.SetActive(showLine[i]);
            }
            else
            {
                if (showLine.Count - 1 > i)
                    segObj = CloneSegmentAndMaterial(i, showLine[i]);
                else
                    segObj = CloneSegmentAndMaterial(i, true);
            }

            PositionAndRotateSegment(coords, i, segObj);
        }

        //Disable unused segments and pool for later
        while (i < segmentObjs.Count)
        {
            segmentObjs[i].SetActive(false);
            i++;
        }

        SnapHighlightsToDefault();
    }

    private void PositionAndRotateSegment(List<Vector3> coords, int segNum, GameObject segObj)
    {
        RectTransform rt = segObj.GetComponent<RectTransform>();
        Vector3 distance = coords[segNum + 1] - coords[segNum];
        Vector3 halfwayPoint = coords[segNum] + distance * 0.5f;
        segObj.transform.localPosition = halfwayPoint;

        if (distance == Vector3.zero)
        {
            Debug.Log("Warning: adjacent coordinates should not have a distance of 0.");
            return;
        }

        float xyHypotenuse = Mathf.Sqrt(Mathf.Pow(distance.x, 2) + Mathf.Pow(distance.y, 2));        
        float zAngle = Mathf.Asin(distance.y / xyHypotenuse) * Mathf.Rad2Deg;
        if (coords[segNum + 1].x < coords[segNum].x)
            zAngle = 180 - zAngle;
        
        //float yzHypotenuse = Mathf.Sqrt(Mathf.Pow(distance.y, 2) + Mathf.Pow(distance.z, 2));
        //float xAngle = Mathf.Asin(distance.z / yzHypotenuse) * Mathf.Rad2Deg;
        //if (coords[segNum + 1].y < coords[segNum].y)
        //    xAngle = 180 - xAngle;

        rt.sizeDelta = new Vector2(xyHypotenuse, 100);
        rt.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, zAngle));
    }

    private GameObject CloneSegmentAndMaterial(int i, bool showSegment)
    {
        GameObject segObj = new GameObject($"LineSegment {i + 1}-{i + 2}");
        segObj.transform.SetParent(segmentParent);
        segObj.transform.localPosition = Vector3.zero;
        segObj.transform.localScale = new Vector3(1, 0.5f, 1);
        segmentObjs.Add(segObj);

        Image img = segObj.AddComponent<Image>();
        img.sprite = segmentSprite;
        img.type = Image.Type.Sliced;

        //Clone the material reference for each new Image
        Material mat = Instantiate(segmentMat);
        segmentMat.CopyPropertiesFromMaterial(mat);
        segmentMats.Add(mat);
        img.material = mat;

        segObj.SetActive(showSegment);
        return segObj;
    }

    public void PermanentSegmentHighlight(int segmentNum)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (segmentNum >= segmentMats.Count)
            return;

        StartCoroutine(IlluminateAnimation(segmentNum));
    }

    private IEnumerator IlluminateAnimation(int segmentNum)
    {
        float halfDuration = flickerDuration / 2f;
        float timer = 0f;

        //Snap to min intensity, then scale up
        while (timer < halfDuration)
        {
            float t = timer / halfDuration;
            float intensity = Mathf.Lerp(flickerMinIntensity, flickerMaxIntensity, t);

            segmentMats[segmentNum].SetFloat("_ColorIntensity", intensity);

            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;

        while (timer < halfDuration)
        {
            float t = timer / halfDuration;
            float intensity = Mathf.Lerp(flickerMaxIntensity, flickerMinIntensity, t);

            segmentMats[segmentNum].SetFloat("_ColorIntensity", intensity);

            timer += Time.deltaTime;
            yield return null;
        }

        //Scale down

        //Snap to min intensity, leaving a permanent glow
        segmentMats[segmentNum].SetFloat("_ColorIntensity", flickerMinIntensity);
    }

    public void SnapHighlightsToDefault()
    {
        for (int i = 0; i < segmentMats.Count; i++)
            segmentMats[i].SetFloat("_ColorIntensity", DEFAULT_INTENSITY);
    }
}
