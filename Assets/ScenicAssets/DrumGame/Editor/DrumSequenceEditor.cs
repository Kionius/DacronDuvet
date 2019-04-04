using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(DrumSequence))]
public class DrumSequenceEditor : Editor {

    SerializedProperty coords;
    SerializedProperty keys;
    int listSize;

    private Rect buttonRect;

    private void OnEnable()
    {
        coords = serializedObject.FindProperty("coords");
        keys = serializedObject.FindProperty("keys");
    }

    public override void OnInspectorGUI()
    {
        DrumSequence sequence = (DrumSequence)target;

        if (sequence.type == DrumSequence.SequenceType.Circle)
        {
            base.OnInspectorGUI();
        }
        else
        {
            int listSize = sequence.keys.Count;

            sequence.type = (DrumSequence.SequenceType)EditorGUILayout.EnumPopup("Sequence Type", sequence.type);

            sequence.zoomoutPosition = EditorGUILayout.Vector3Field("Zoomout Position", sequence.zoomoutPosition);

            if (GUILayout.Button("Generate elements"))
                PopupWindow.Show(buttonRect, new DrumSequenceGenerationPopup(sequence));

            if (GUILayout.Button("Delete elements"))
                PopupWindow.Show(buttonRect, new DrumSequenceDeletionPopup(sequence));

            if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();

            //List size field

            EditorGUILayout.BeginHorizontal();
            listSize = DrawListSizeField(sequence, listSize);
            EditorGUILayout.EndHorizontal();

            //Layout and padding for 3 columns

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            //Index label column

            DrawIndexColumn(sequence);
            EditorGUILayout.EndVertical();

            //Prompt key selection popup column

            EditorGUILayout.BeginVertical();
            DrawKeySelectionColumn(sequence);
            EditorGUILayout.EndVertical();

            //Coordinates column

            EditorGUILayout.BeginVertical(GUILayout.MinWidth(200));
            DrawCoordinatesColumn(sequence);

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        if (GUI.changed)
            EditorUtility.SetDirty(sequence);
    }

    private static void DrawIndexColumn(DrumSequence sequence)
    {
        for (int i = 0; i < sequence.keys.Count; i++)
        {
            EditorGUILayout.LabelField($"{i}", GUILayout.MinWidth(50));
        }
    }

    private static void DrawKeySelectionColumn(DrumSequence sequence)
    {
        EditorGUILayout.LabelField("Keys", GUILayout.MinWidth(20));
        for (int i = 0; i < sequence.keys.Count; i++)
        {
            sequence.keys[i] = (DrumSequence.DrumKey)EditorGUILayout.EnumPopup(sequence.keys[i], GUILayout.Width(80));
        }
    }

    private static void DrawCoordinatesColumn(DrumSequence sequence)
    {
        EditorGUILayout.PrefixLabel("Coords");
        for (int i = 0; i < sequence.coords.Count; i++)
            sequence.coords[i] = EditorGUILayout.Vector3Field("", sequence.coords[i], GUILayout.MinWidth(80));
    }

    private int DrawListSizeField(DrumSequence sequence, int listSize)
    {
        EditorGUILayout.PrefixLabel("Num elements");
        EditorGUI.BeginChangeCheck();
        listSize = EditorGUILayout.DelayedIntField(listSize);
        if (EditorGUI.EndChangeCheck())
        {
            while (sequence.keys.Count < listSize)
                sequence.keys.Add(DrumSequence.DrumKey.One);

            while (sequence.keys.Count > listSize)
                sequence.keys.RemoveAt(sequence.keys.Count - 1);

            while (sequence.coords.Count < listSize)
                sequence.coords.Add(Vector3.zero);

            while (sequence.coords.Count > listSize)
                sequence.coords.RemoveAt(sequence.coords.Count - 1);
        }

        return listSize;
    }
}

public class DrumSequenceGenerationPopup : PopupWindowContent
{
    private int startIndex;
    private int numElements;
    private Vector3 spacing;
    private Vector3 startCoord;
    private DrumSequence.DrumKey key = DrumSequence.DrumKey.One;
    private DrumSequence sequence;

    public DrumSequenceGenerationPopup(DrumSequence sequence)
    {
        this.sequence = sequence;
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(200, 180);
    }

    public override void OnGUI(Rect rect)
    {
        //GUILayout.Label("Popup Options Example", EditorStyles.boldLabel);
        startIndex = EditorGUILayout.IntField("Insert at index", startIndex);
        numElements = EditorGUILayout.IntField("Num Elements", numElements);
        startCoord = EditorGUILayout.Vector3Field("Initial position", startCoord);
        spacing = EditorGUILayout.Vector3Field("Spacing", spacing);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Key", GUILayout.MinWidth(30));
        key = (DrumSequence.DrumKey)EditorGUILayout.EnumPopup(key);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Autofill to build from last coord"))
        {
            startIndex = sequence.keys.Count;
            startCoord = sequence.coords[startIndex - 1] + spacing;
        }

        if (GUILayout.Button("Generate!"))
        {
            sequence = GenerateAtIndex(startIndex);
            //Some sort of callback to reassign the new Sequence to the asset it came from?
            //Nope! since it's a reference type, 
        }
    }

    public override void OnOpen()
    {
        
    }

    public override void OnClose()
    {
        
    }

    private DrumSequence GenerateAtIndex(int firstIndex)
    {
        //Treating these as immutable gives us quick access to Undo functionality which we probably want
        //List<DrumSequence.DrumKey> newKeys = new List<DrumSequence.DrumKey>(sequence.keys.Count);
        //List<Vector3> newCoords = new List<Vector3>(sequence.coords.Count);

        //newKeys = (List<DrumSequence.DrumKey>)sequence.keys.Select(_ => sequence.Clone().keys);
        //newCoords = (List<Vector3>)sequence.coords.Select(_ => sequence.Clone().coords);

        DrumSequence newCopy = sequence.Clone();

        for (int i = 0; i < numElements; i++)
        {
            Vector3 coordinate = startCoord + (spacing * i);
            newCopy.keys.Insert(firstIndex + i, key);
            newCopy.coords.Insert(firstIndex + i, coordinate);
        }

        return newCopy;
    }
}

public class DrumSequenceDeletionPopup : PopupWindowContent
{
    private int startIndex;
    private int numElements;
    private DrumSequence sequence;

    public DrumSequenceDeletionPopup(DrumSequence sequence)
    {
        this.sequence = sequence;
    }

    public override void OnGUI(Rect rect)
    {
        startIndex = EditorGUILayout.IntField("Start index", startIndex);
        numElements = EditorGUILayout.IntField("Num elements", numElements);

        if (GUILayout.Button("Delete!"))
            sequence = DeleteFromIndex(startIndex);
    }

    private DrumSequence DeleteFromIndex(int firstIndex)
    {
        DrumSequence newCopy = sequence.Clone();

        newCopy.keys.RemoveRange(firstIndex, numElements);
        newCopy.coords.RemoveRange(firstIndex, numElements);

        return newCopy;
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(200, 70);
    }

    public override void OnOpen()
    {

    }

    public override void OnClose()
    {

    }
}
