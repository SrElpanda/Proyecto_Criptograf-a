using UnityEngine;

public class SelectionMover : MonoBehaviour
{
    private SelectionManager selectionManager;
    private Camera cam;
    private bool isDragging;

    void Start()
    {
        selectionManager = GetComponent<SelectionManager>();
        cam = Camera.main;
    }

    void Update()
    {
        if (selectionManager == null)
            return;

        if (selectionManager.currentMode != SelectionManager.SelectMode.Move)
            return;

        if (selectionManager.selectedObject == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            DragOnPlane();
        }
    }

    void DragOnPlane()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Plane plane = new Plane(Vector3.up, new Vector3(0, 0.5f, 0));

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 point = ray.GetPoint(distance);

            Vector3 pos = selectionManager.selectedObject.transform.position;
            pos.x = point.x;
            pos.z = point.z;
            selectionManager.selectedObject.transform.position = pos;
        }
    }
}
