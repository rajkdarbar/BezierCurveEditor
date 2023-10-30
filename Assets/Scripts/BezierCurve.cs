using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BezierCurve : MonoBehaviour
{
    public GameObject anchorPointPrefab;
    public GameObject controlPointPrefab;

    public Vector3 anchorPoint1;
    public Vector3 anchorPoint2;
    private Vector3 controlPoint1;
    private Vector3 controlPoint2;

    private GameObject anchorInstance1;
    private GameObject anchorInstance2;
    private GameObject controlInstance1;
    private GameObject controlInstance2;

    private LineRenderer lineRenderer;
    public int resolution = 10;

    void Start()
    {
        // Instantiate the prefabs for visualization
        anchorInstance1 = Instantiate(anchorPointPrefab, anchorPoint1, Quaternion.identity);
        anchorInstance2 = Instantiate(anchorPointPrefab, anchorPoint2, Quaternion.identity);

        // Calculate control points, here's a basic way, you can modify as needed
        CalculateControlPoints();

        controlInstance1 = Instantiate(controlPointPrefab, controlPoint1, Quaternion.identity);
        controlInstance2 = Instantiate(controlPointPrefab, controlPoint2, Quaternion.identity);

        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        // Update positions in case they were moved
        anchorPoint1 = anchorInstance1.transform.position;
        anchorPoint2 = anchorInstance2.transform.position;
        controlPoint1 = controlInstance1.transform.position;
        controlPoint2 = controlInstance2.transform.position;

        DrawCurve();
    }

    void CalculateControlPoints()
    {
        // Example: setting control points at one third and two thirds the distance between anchors
        controlPoint1 = Vector3.Lerp(anchorPoint1, anchorPoint2, 1f / 3f);
        controlPoint2 = Vector3.Lerp(anchorPoint1, anchorPoint2, 2f / 3f);
    }

    void DrawCurve()
    {
        lineRenderer.positionCount = resolution + 1;
        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            lineRenderer.SetPosition(i, CalculateBezierPoint(t, anchorPoint1, controlPoint1, controlPoint2, anchorPoint2));
        }
    }

    Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }
}
