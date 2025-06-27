using UnityEngine;
using System.Collections.Generic;

public class PlayerRaycast : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private float offsetDistance = 0.1f;
    [SerializeField] private float lineThickness = 0.1f; // Default thickness

    [SerializeField] private Material material1;  // First material
    [SerializeField] private Material material2;  // Second material

    private Material currentMaterial;
    private List<Vector3> points = new List<Vector3>();
    private GameObject currentLineObject;
    private LineRenderer currentLineRenderer;
    private bool isDrawing = false;
    private int drawingType = 1;

    void Start()
    {
        // Set the initial material
        currentMaterial = material1;
    }

    void Update()
    {
        getInput();
    }

    void getInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) drawingType = 1;
        if (Input.GetKeyDown(KeyCode.Alpha2)) drawingType = 2;
        if (Input.GetKeyDown(KeyCode.Alpha3)) drawingType = 3;
        if (Input.GetKeyDown(KeyCode.Alpha4)) drawingType = 4;

        if (Input.GetMouseButtonDown(0))
        {
            StartDrawing();
        }

        if (isDrawing && Input.GetMouseButton(0))
        {
            UpdateDrawing();
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDrawing = false;
        }

        if (Input.mouseScrollDelta.y > 0)
        {
            IncreaseThickness();
        }
        else if (Input.mouseScrollDelta.y < 0)
        {
            DecreaseThickness();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            // Switch between materials when 'C' is pressed
            SwitchMaterial();
        }
    }

    void StartDrawing()
{
    isDrawing = true;
    points.Clear();

    currentLineObject = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
    currentLineRenderer = currentLineObject.GetComponent<LineRenderer>();

    // Set initial material and render queue
    currentLineRenderer.material = currentMaterial;
    currentLineRenderer.material.renderQueue = 3000; // Ensure it renders on top

    // Set initial line thickness and count
    currentLineRenderer.startWidth = lineThickness;
    currentLineRenderer.endWidth = lineThickness;
    currentLineRenderer.positionCount = 0;
}

    void SwitchMaterial()
    {
        // Toggle the current material between material1 and material2
        if (currentMaterial == material1)
        {
            currentMaterial = material2;
        }
        else
        {
            currentMaterial = material1;
        }

        // Apply the current material to the line renderer's material and set render queue.
        if (currentLineRenderer != null)
        {
            currentLineRenderer.material = currentMaterial;
            currentLineRenderer.material.renderQueue = 3000; // Ensure it renders on top
        }
    }

    void UpdateDrawing()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Calculate direction towards the camera
            Vector3 directionToCamera = (mainCamera.transform.position - hit.point).normalized;
            Vector3 newPoint = hit.point + directionToCamera * offsetDistance;

            // Only add a new point if it's far enough from the last one
            if (points.Count == 0 || Vector3.Distance(newPoint, points[points.Count - 1]) > 0.1f)
            {
                points.Add(newPoint);
                UpdateLineRenderer();
            }
        }
    }

    void UpdateLineRenderer()
    {
        if (currentLineRenderer == null) return;

        // Apply the line thickness based on the direction of drawing
        Vector3 lineDirection = (points.Count > 1) ? points[points.Count - 1] - points[points.Count - 2] : Vector3.zero;
        float lineLength = lineDirection.magnitude;

        // Adjust line thickness in the direction of the line
        float dynamicThickness = Mathf.Clamp(lineThickness * lineLength, 0.05f, 1.0f);
        currentLineRenderer.startWidth = dynamicThickness;
        currentLineRenderer.endWidth = dynamicThickness;

        // Choose the drawing method based on the drawing type
        switch (drawingType)
        {
            case 1:
                currentLineRenderer.positionCount = points.Count;
                currentLineRenderer.SetPositions(points.ToArray());
                break;
            case 2:
                LineUtils.DrawBezierCurve(points, currentLineRenderer);
                break;
            case 3:
                LineUtils.DrawLerpSmoothedLine(points, currentLineRenderer);
                break;
            case 4:
                LineUtils.DrawStraightLine(points, currentLineRenderer);
                break;
        }
    }

    void IncreaseThickness()
    {
        lineThickness = Mathf.Min(lineThickness + 0.05f, 1.0f); 
        UpdateLineRenderer();
    }

    void DecreaseThickness()
    {
        lineThickness = Mathf.Max(lineThickness - 0.05f, 0.05f);
        UpdateLineRenderer();
    }
}
