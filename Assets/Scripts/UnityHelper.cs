
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnityHelper {

	public static Vector3 GetWorldSpaceMousePosition()
    {
        Camera mainCamera = Camera.main;

        Vector3 mouseScreenPos = new Vector3
        {
            x = mainCamera.pixelWidth - Input.mousePosition.x,
            y = mainCamera.pixelHeight - Input.mousePosition.y,
            z = mainCamera.transform.position.z
        };

        return mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }
}
