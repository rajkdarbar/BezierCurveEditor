using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class InteractiveBezierCurve : MonoBehaviour
{
    public GameObject anchorPointPrefab;
    public GameObject controlPointPrefab;

    public bool toggleClosed = false;
    private bool previousToggleClosed = false;

    public enum MouseClickRole { AddAnchorPoint, DeleteAnchorPoint, SelectPoint }
    public MouseClickRole currentClickRole = MouseClickRole.AddAnchorPoint;

    private List<Vector3> anchorPoints = new List<Vector3>();
    private List<Vector3> controlPoints = new List<Vector3>();

    private List<GameObject> anchorInstances = new List<GameObject>();
    private List<GameObject> controlInstances = new List<GameObject>();

    private LineRenderer lineRenderer;
    public int resolution = 1000;

    private GameObject selectedPoint = null;
    private bool isDragging = false;

    private GameObject controlPointInstance1 = null;
    private GameObject controlPointInstance2 = null;


    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        HandleToggleClosed();

        if (!previousToggleClosed)
        {
            HandleMouseInputs();
        }
    }

    void HandleToggleClosed()
    {
        if (toggleClosed != previousToggleClosed)
        {
            if (toggleClosed)
            {
                if (anchorPoints.Count > 1)
                {
                    DrawBezierSegment(anchorPoints.Count); // This creates a closed bezier curve
                }
                else
                {
                    Debug.Log("You need atleast two anchor points.");
                }
            }
            else
            {
                if (anchorPoints.Count > 1)
                {
                    int positionsToRemove = resolution;
                    lineRenderer.positionCount -= positionsToRemove; // It removes the curve segment between first and last anchor points
                    Destroy(controlPointInstance1);
                    Destroy(controlPointInstance2);
                }
            }

            previousToggleClosed = toggleClosed;
        }
    }

    void HandleMouseInputs()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (currentClickRole == MouseClickRole.AddAnchorPoint)
            {
                Vector3 mousePositionInWorld = Camera.main.ScreenToWorldPoint(
                    new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z)
                );
                mousePositionInWorld.z = 0;
                AddAnchorPoint(mousePositionInWorld);
            }

            else if (currentClickRole == MouseClickRole.DeleteAnchorPoint)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject.CompareTag("AnchorPoint"))
                    {
                        DeleteAnchorPoint(hit.collider.gameObject);
                    }
                }
            }

            else if (currentClickRole == MouseClickRole.SelectPoint)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject.CompareTag("AnchorPoint") || hit.collider.gameObject.CompareTag("ControlPoint"))
                    {
                        selectedPoint = hit.collider.gameObject;
                        isDragging = true;
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && selectedPoint)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
            mousePos.z = 0;
            selectedPoint.transform.position = mousePos;

            // Now update the bezier curve points based on the moved point (consider it as an edit mode)
            UpdateBezierCurvePoints();
        }
    }


    void AddAnchorPoint(Vector3 newPoint)
    {
        anchorPoints.Add(newPoint);
        GameObject anchorInstance = Instantiate(anchorPointPrefab, newPoint, Quaternion.identity);
        anchorInstances.Add(anchorInstance);

        CalculateControlPoints(anchorPoints.Count - 1);

        DrawBezierSegment(anchorPoints.Count - 1);
    }


    void CalculateControlPoints(int anchorIndex, bool isEditMode = false)
    {
        if (anchorIndex <= 0 || anchorIndex >= anchorPoints.Count) return;

        Vector3 dir = (anchorPoints[anchorIndex] - anchorPoints[anchorIndex - 1]).normalized;
        Vector3 midPoint = (anchorPoints[anchorIndex - 1] + anchorPoints[anchorIndex]) / 2;
        float distance = Vector3.Distance(anchorPoints[anchorIndex - 1], anchorPoints[anchorIndex]);

        Vector3 perpDir = new Vector3(-dir.y, dir.x, 0); // Perpendicular direction in 2D.  

        // Place controlPoint1 at 1/3 and controlPoint2 at 2/3 of the distance between the anchor points.
        Vector3 oneThird = anchorPoints[anchorIndex - 1] + dir * (distance / 3);
        Vector3 twoThird = anchorPoints[anchorIndex - 1] + dir * (2 * distance / 3);

        Vector3 controlPoint1, controlPoint2;

        if (anchorIndex > 1)
        {
            // If there are previous control points, use them to guide the placement of the next control point for smoothness.
            Vector3 directionToLastControlPoint = anchorPoints[anchorIndex - 1] - controlPoints[2 * (anchorIndex - 1) - 1];

            controlPoint1 = anchorPoints[anchorIndex - 1] + directionToLastControlPoint; // Place the control point collinearly in the opposite direction
            controlPoint2 = twoThird + perpDir * distance * 0.25f;
        }
        else
        {
            controlPoint1 = oneThird + perpDir * distance * 0.25f;
            controlPoint2 = twoThird + perpDir * distance * 0.25f;
        }

        controlPoint1.z = 0;
        controlPoint2.z = 0;

        if (isEditMode)
        {
            controlPoints[2 * (anchorIndex - 1)] = controlPoint1;
            controlPoints[2 * (anchorIndex - 1) + 1] = controlPoint2;

            controlInstances[2 * (anchorIndex - 1)].transform.position = controlPoint1;
            controlInstances[2 * (anchorIndex - 1) + 1].transform.position = controlPoint2;
        }
        else
        {
            controlPoints.Add(controlPoint1);
            controlPoints.Add(controlPoint2);

            GameObject controlInstance1 = Instantiate(controlPointPrefab, controlPoint1, Quaternion.identity);
            GameObject controlInstance2 = Instantiate(controlPointPrefab, controlPoint2, Quaternion.identity);

            controlInstances.Add(controlInstance1);
            controlInstances.Add(controlInstance2);
        }
    }


    void UpdateBezierCurvePoints()
    {
        int index;

        if (selectedPoint.CompareTag("AnchorPoint"))
        {
            index = anchorInstances.IndexOf(selectedPoint);
            anchorPoints[index] = selectedPoint.transform.position;

            if (index == 0 && anchorPoints.Count > 1) // First anchor point
            {
                CalculateControlPoints(index + 1, true);
                DrawBezierSegment(index + 1);

                if (anchorPoints.Count > 2)
                {
                    CalculateControlPoints(index + 2, true);
                    DrawBezierSegment(index + 2);
                }
            }

            else if (index > 0 && index < anchorPoints.Count - 1) // Middle anchor points
            {
                // Update control points for the segment before and after this anchor
                CalculateControlPoints(index, true);
                CalculateControlPoints(index + 1, true);

                // Redraw the two affected segments
                DrawBezierSegment(index);
                DrawBezierSegment(index + 1);
            }

            else if (index == anchorPoints.Count - 1) // Last anchor point
            {
                CalculateControlPoints(index, true);
                DrawBezierSegment(index);
            }
        }

        else if (selectedPoint.CompareTag("ControlPoint"))
        {
            index = controlInstances.IndexOf(selectedPoint);
            controlPoints[index] = selectedPoint.transform.position;

            if (index == 0) // First control point
            {
                DrawBezierSegment(index + 1);
            }

            else if (index > 0 && index < controlPoints.Count - 1) // Middle control points
            {
                int associatedAnchorIndex;
                int oppositeControlIndex;

                if (index % 2 == 0)  // If it's an even control point (first control point of the segment)
                {
                    associatedAnchorIndex = index / 2;
                    oppositeControlIndex = index - 1;
                }

                else  // If it's an odd control point (second control point of the segment)
                {
                    associatedAnchorIndex = (index + 1) / 2;
                    oppositeControlIndex = index + 1;
                }

                Vector3 directionToDraggedControl = anchorPoints[associatedAnchorIndex] - controlPoints[index];
                controlPoints[oppositeControlIndex] = anchorPoints[associatedAnchorIndex] + directionToDraggedControl;

                // Update the position of the control instance in the scene
                controlInstances[oppositeControlIndex].transform.position = controlPoints[oppositeControlIndex];

                // Redraw the two affected segments
                DrawBezierSegment(associatedAnchorIndex);
                DrawBezierSegment(associatedAnchorIndex + 1);
            }

            else if (index == controlPoints.Count - 1) // Last control point
            {
                DrawBezierSegment(anchorPoints.Count - 1);
            }
        }
    }

    void DeleteAnchorPoint(GameObject pointToDelete)
    {
        if (!pointToDelete.CompareTag("AnchorPoint")) return;

        int index = anchorInstances.IndexOf(pointToDelete);

        if (index < 0) return;

        Destroy(anchorInstances[index]);
        anchorInstances.RemoveAt(index);
        anchorPoints.RemoveAt(index); // With each deletion, "anchorPoints" list is getting updated

        if (index == 0) // First anchor point
        {
            if (controlInstances.Count > 0)
            {
                Destroy(controlInstances[0]);
                Destroy(controlInstances[1]);

                controlInstances.RemoveAt(0);
                controlInstances.RemoveAt(0); // Notice '0' here. While removing, the index of the list is shifting towards left.

                controlPoints.RemoveAt(0);
                controlPoints.RemoveAt(0);
            }
        }

        else if (index > 0 && index < anchorPoints.Count) // Middle anchor points
        {
            Destroy(controlInstances[2 * index - 2]);
            Destroy(controlInstances[2 * index - 1]);

            controlInstances.RemoveAt(2 * index - 2);
            controlInstances.RemoveAt(2 * index - 2);

            controlPoints.RemoveAt(2 * index - 2);
            controlPoints.RemoveAt(2 * index - 2);

            // Update control points for the segment before and after the deleted anchor
            CalculateControlPoints(index, true);

            if (index + 1 < anchorPoints.Count)
            {
                CalculateControlPoints(index + 1, true);
            }
        }

        else if (index == anchorPoints.Count) // Last anchor point
        {
            if (controlInstances.Count > 0)
            {
                Destroy(controlInstances[controlInstances.Count - 2]);
                Destroy(controlInstances[controlInstances.Count - 1]);

                controlInstances.RemoveAt(controlInstances.Count - 1);
                controlInstances.RemoveAt(controlInstances.Count - 1);

                controlPoints.RemoveAt(controlPoints.Count - 1);
                controlPoints.RemoveAt(controlPoints.Count - 1);
            }
        }

        DrawAllSegments();
    }

    void DrawAllSegments()
    {
        lineRenderer.positionCount = 0;
        for (int i = 1; i < anchorPoints.Count; i++)
        {
            DrawBezierSegment(i);
        }
    }


    void DrawBezierSegment(int anchorIndex)
    {
        Vector3 p0, p1, p2, p3;

        if (anchorIndex <= 0 || anchorIndex > anchorPoints.Count)
        {
            return;
        }

        if (toggleClosed)
        {
            p0 = anchorPoints[anchorIndex - 1];

            Vector3 directionToLastControlPoint = p0 - controlPoints[controlPoints.Count - 1];
            p1 = p0 + directionToLastControlPoint;

            Vector3 directionToFirstControlPoint = anchorPoints[0] - controlPoints[0];
            p2 = anchorPoints[0] + directionToFirstControlPoint;

            p3 = anchorPoints[0];

            controlPointInstance1 = Instantiate(controlPointPrefab, p1, Quaternion.identity);
            controlPointInstance2 = Instantiate(controlPointPrefab, p2, Quaternion.identity);
        }
        else
        {
            p0 = anchorPoints[anchorIndex - 1];
            p1 = controlPoints[2 * (anchorIndex - 1)];
            p2 = controlPoints[2 * (anchorIndex - 1) + 1];
            p3 = anchorPoints[anchorIndex];
        }


        int segmentResolution = resolution;

        // Calculate the starting position for the new segment.
        int startLineIndex = (anchorIndex - 1) * segmentResolution;

        for (int i = 0; i < segmentResolution; i++)
        {
            float t = i / (float)segmentResolution;
            Vector3 position = CalculateBezierPoint(t, p0, p1, p2, p3);

            if (startLineIndex + i >= lineRenderer.positionCount)
            {
                lineRenderer.positionCount++;
            }

            lineRenderer.SetPosition(startLineIndex + i, position);
        }

        lineRenderer.positionCount = Mathf.Max(lineRenderer.positionCount, startLineIndex + segmentResolution);
    }


    Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 result = uuu * p0; // (1-t) * (1-t) * (1-t) * p0
        result += 3 * uu * t * p1; // 3 * (1-t) * (1-t) * t * p1
        result += 3 * u * tt * p2; // 3 * (1-t) * t * t * p2
        result += ttt * p3; // t * t * t * p3

        return result;
    }
}
