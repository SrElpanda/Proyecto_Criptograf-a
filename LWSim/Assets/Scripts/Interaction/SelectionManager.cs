using UnityEngine;
using System;

public class SelectionManager : MonoBehaviour
{
    public enum SelectMode { Move, Inspect }

    [Header("State")]
    public SelectMode currentMode = SelectMode.Inspect;

    public GameObject selectedObject { get; private set; }

    public event Action<GameObject> OnSelectionChanged;

    private Camera cam;
    private Color originalColor;
    private Renderer selectedRenderer;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        HandleModeToggle();

        if (Input.GetMouseButtonDown(0))
        {
            TrySelect();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Deselect();
        }
    }

    void HandleModeToggle()
    {
        if (Input.GetKeyDown(KeyCode.M))
            currentMode = SelectMode.Move;
        else if (Input.GetKeyDown(KeyCode.I))
            currentMode = SelectMode.Inspect;
    }

    void TrySelect()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            SensorNode node =
                hit.collider.GetComponent<SensorNode>();

            if (node != null)
            {
                Select(hit.collider.gameObject);
                return;
            }

            CentralHub hub =
                hit.collider.GetComponent<CentralHub>();

            if (hub != null)
            {
                Select(hit.collider.gameObject);
                return;
            }
        }

        Deselect();
    }

    void Select(GameObject obj)
    {
        if (selectedObject == obj)
            return;

        Deselect();

        selectedObject = obj;
        selectedRenderer = obj.GetComponent<Renderer>();

        if (selectedRenderer != null)
        {
            originalColor = selectedRenderer.material.color;
            selectedRenderer.material.color = Color.yellow;
        }

        OnSelectionChanged?.Invoke(selectedObject);
    }

    public void Deselect()
    {
        if (selectedObject == null)
            return;

        if (selectedRenderer != null)
        {
            selectedRenderer.material.color = originalColor;
        }

        selectedObject = null;
        selectedRenderer = null;

        OnSelectionChanged?.Invoke(null);
    }
}
