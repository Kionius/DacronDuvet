using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UniRx;
using UnityEngine.Events;
using System;

public class DrumGameLineEditor : MonoBehaviour {

    /* TODO List
     * Maybe add click-grab click-release mode instead of drag and drop
     * Refactor to pass 'editing' responsibilities of LineEditor into this class
     */

    public Vector2 gridSnapVector = new Vector3(5f, 5f, 0f);
    public BoolReactiveProperty isEditing;
    public bool gridSnapping;
    public bool autoRedrawSegments;
    public DrumSequence currentSequence;
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    public KeyCode toggleSnapToGrid = KeyCode.S;
    public KeyCode toggleAutoRedraw = KeyCode.R;
    public KeyCode addNewPrompt = KeyCode.N;
    public KeyCode delLastPrompt = KeyCode.D;

    [Header("Hotkeys with Left Shift")]
    public KeyCode saveZoomoutPositionKey = KeyCode.C;
    public KeyCode undoKey = KeyCode.Z;
    public KeyCode redoKey = KeyCode.Y;

    public delegate void OnMovePrompt();
    private OnMovePrompt onMovePrompt;

    private Camera mainCamera;
    private DrumGamePrompt selectedPrompt;
    private Coroutine draggingPrompt;

    private List<DrumGamePrompt> prompts; //ref from DrumGameManagerLine

    private Stack<int> undoIndexStack;
    private Stack<int> redoIndexStack;
    private Stack<Vector3> undoCoordStack;
    private Stack<Vector3> redoCoordStack;

    private void Awake()
    {
        mainCamera = Camera.main;

        undoIndexStack = new Stack<int>();
        redoIndexStack = new Stack<int>();
        undoCoordStack = new Stack<Vector3>();
        redoCoordStack = new Stack<Vector3>();

        isEditing = new BoolReactiveProperty();

        isEditing.Subscribe(DropPromptWhenEditEnds);
    }

    public void SetSequence(DrumSequence next)
    {
        if (next == null)
            return;

        currentSequence = next;

        if (currentSequence.showLine == null || currentSequence.showLine.Count != currentSequence.coords.Count)
        {
            currentSequence.showLine = new List<bool>(currentSequence.coords.Count);
            while (currentSequence.showLine.Count < currentSequence.coords.Count)
                currentSequence.showLine.Add(true);
        }

        //Debug.Log($"currentSequence name = {currentSequence.name}");
        //Debug.Log($"currentSequence coords count = {currentSequence.coords.Count}");
        //Debug.Log($"currentSequence showLine count = {currentSequence.showLine.Count}");
    }

    public void SetPromptsRef(List<DrumGamePrompt> promptList)
    {
        prompts = promptList;
    }

    public void SetRefreshDelegate(OnMovePrompt action)
    {
        onMovePrompt = action;
    }
	
	void Update ()
    {
        if (isEditing.Value)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                DrumGamePrompt selectedPrompt = TryRaycastToPrompt();

                if (selectedPrompt != null && !Input.GetKey(KeyCode.LeftShift))
                    StartDragPrompt(selectedPrompt);
                else if (selectedPrompt != null && Input.GetKey(KeyCode.LeftShift))
                    ToggleShowLine(selectedPrompt.GetIndex());
            }

            if (selectedPrompt != null && Input.GetKeyUp(KeyCode.Mouse0))
            {
                DropDraggedPrompt();
            }

            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                ChangeCameraZoom(25);
            }

            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                ChangeCameraZoom(-25);
            }

            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(saveZoomoutPositionKey))
                SaveZoomoutPosition();

            //TODO: add these to the undo/redo history... might require changing the model
            if (Input.GetKeyDown(addNewPrompt))
                AddPromptUnderCursor(DrumSequence.DrumKey.One);

            if (Input.GetKeyDown(KeyCode.Alpha1))
                AddPromptUnderCursor(DrumSequence.DrumKey.One);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                AddPromptUnderCursor(DrumSequence.DrumKey.Two);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                AddPromptUnderCursor(DrumSequence.DrumKey.Three);

            if (Input.GetKeyDown(KeyCode.Alpha4))
                AddPromptUnderCursor(DrumSequence.DrumKey.Four);

            if (Input.GetKeyDown(KeyCode.Alpha5))
                AddPromptUnderCursor(DrumSequence.DrumKey.Five);

            if (Input.GetKeyDown(KeyCode.Alpha6))
                AddPromptUnderCursor(DrumSequence.DrumKey.Six);

            if (Input.GetKeyDown(KeyCode.Alpha7))
                AddPromptUnderCursor(DrumSequence.DrumKey.Seven);

            if (Input.GetKeyDown(delLastPrompt))
                DeleteLastPrompt();

            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(undoKey))
                Undo();

            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(redoKey))
                Redo();
        }

        if (Input.GetKeyDown(toggleSnapToGrid))
            gridSnapping = !gridSnapping;

        if (Input.GetKeyDown(toggleAutoRedraw))
            autoRedrawSegments = !autoRedrawSegments;
	}

    private void ChangeCameraZoom(int zChange)
    {
        Vector3 zoomDistance = mainCamera.transform.localPosition;
        zoomDistance.z += zChange;
        mainCamera.transform.localPosition = zoomDistance;
    }

    /// <summary>
    /// Returns the first prompt hit by a raycast, or null if none available
    /// </summary>
    private DrumGamePrompt TryRaycastToPrompt()
    {
        //Raycast into scene
        var pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();

        raycaster.Raycast(pointerEventData, results);

        if (results.Count > 0)
        {
            //Check for collisions with objects holding a Prompt
            DrumGamePrompt prompt = results[0].gameObject.GetComponentInParent<DrumGamePrompt>();
            return prompt;
        }

        else return null;
    }

    private void StartDragPrompt(DrumGamePrompt prompt)
    {
        //If successful, cache a ref to scene Prompt obj
        selectedPrompt = prompt;

        //Save its current position to our undo history
        AddToUndoHistory(prompt.GetIndex(), prompt.transform.localPosition);
        ClearRedoHistory();

        //Start dragging!
        if (draggingPrompt != null)
            StopCoroutine(draggingPrompt);

        draggingPrompt = StartCoroutine(DraggingPrompt());
    }

    private void ToggleShowLine(int promptIndex)
    {
        currentSequence.showLine[promptIndex] = !currentSequence.showLine[promptIndex];

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(currentSequence);
#endif

        //Trigger redraw event whether or not auto redraw is on,
        //or else we won't know we just did something!
        onMovePrompt.Invoke();
    }

    private void DropPromptWhenEditEnds(bool editing)
    {
        if (!editing && selectedPrompt != null)
            DropDraggedPrompt();
    }

    private void DropDraggedPrompt()
    {
        if (gridSnapping)
        {
            Vector3 droppedPosition = selectedPrompt.transform.localPosition;

            Vector3 gridPosition = GetGridSnappedPosition(droppedPosition);

            StopCoroutine(draggingPrompt);
            selectedPrompt.transform.localPosition = gridPosition;
        }

        SavePromptLocationToAsset(selectedPrompt.GetIndex(), selectedPrompt.transform.localPosition);

        //clear the cached reference
        selectedPrompt = null;
    }

    private Vector3 GetGridSnappedPosition(Vector3 input)
    {
        int gridX = Mathf.RoundToInt(input.x / gridSnapVector.x) * (int)gridSnapVector.x;
        int gridY = Mathf.RoundToInt(input.y / gridSnapVector.y) * (int)gridSnapVector.y;

        return new Vector3 { x = gridX, y = gridY, z = 0 };
    }

    private void SavePromptLocationToAsset(int index, Vector3 position)
    {
#if UNITY_EDITOR
        currentSequence.coords[index] = position;
        UnityEditor.EditorUtility.SetDirty(currentSequence);
#endif

        if (autoRedrawSegments)
            onMovePrompt.Invoke();
    }

    private void SaveZoomoutPosition()
    {
#if UNITY_EDITOR
        currentSequence.zoomoutPosition = Camera.main.transform.localPosition;
        UnityEditor.EditorUtility.SetDirty(currentSequence);
#endif
    }

    IEnumerator DraggingPrompt()
    {
        while (selectedPrompt != null)
        {
            //Transform mouse position into world position -- drag the prompt to that world position

            Vector3 mouseWorldPos = UnityHelper.GetWorldSpaceMousePosition();
            mouseWorldPos.z = 0;
            selectedPrompt.transform.localPosition = mouseWorldPos;

            yield return null;
        }
    }


    private void DeleteLastPrompt()
    {
        currentSequence.keys.RemoveAt(currentSequence.keys.Count - 1);
        currentSequence.coords.RemoveAt(currentSequence.coords.Count - 1);
        currentSequence.showLine.RemoveAt(currentSequence.showLine.Count - 1);

        onMovePrompt.Invoke();
    }

    private void AddPromptUnderCursor(DrumSequence.DrumKey keyNum)
    {
        Vector3 mouseWorldPos = UnityHelper.GetWorldSpaceMousePosition();
        mouseWorldPos.z = 0;

        if (gridSnapping)
        {
            Vector3 gridPosition = GetGridSnappedPosition(mouseWorldPos);
            mouseWorldPos = gridPosition;
        }

        currentSequence.keys.Add(keyNum);
        currentSequence.coords.Add(mouseWorldPos);
        currentSequence.showLine.Add(true);

        onMovePrompt.Invoke();
    }

    private void AddToUndoHistory(int index, Vector3 position)
    {
        undoIndexStack.Push(index);
        undoCoordStack.Push(position);
    }

    private void AddToRedoHistory(int index, Vector3 position)
    {
        redoIndexStack.Push(index);
        redoCoordStack.Push(position);
    }

    private void ClearRedoHistory()
    {
        redoIndexStack.Clear();
        redoCoordStack.Clear();
    }

    private void Undo()
    {
        if (undoIndexStack.Count <= 0)
            return;

        //pop an index and a vector off their respective stacks
        int promptIndex = undoIndexStack.Pop();
        Vector3 lastPosition = undoCoordStack.Pop();

        //Save the current position of that prompt to the Redo history
        AddToRedoHistory(promptIndex, prompts[promptIndex].transform.localPosition);

        //Move the prompt and save its location
        prompts[promptIndex].transform.localPosition = lastPosition;
        SavePromptLocationToAsset(promptIndex, lastPosition);
    }

    private void Redo()
    {
        if (redoIndexStack.Count <= 0)
            return;

        //pop an index and a vector
        int promptIndex = redoIndexStack.Pop();
        Vector3 lastPosition = redoCoordStack.Pop();

        //Save the current position back to the Undo history
        AddToUndoHistory(promptIndex, prompts[promptIndex].transform.localPosition);

        //Move the prompt and save its location
        prompts[promptIndex].transform.localPosition = lastPosition;
        SavePromptLocationToAsset(promptIndex, lastPosition);
    }
}
