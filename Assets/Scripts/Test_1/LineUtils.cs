using System.Collections.Generic;
using UnityEngine;

public static class LineUtils
{
    public static Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        return (uuu * p0) + (3 * uu * t * p1) + (3 * u * tt * p2) + (ttt * p3);
    }

    public static void DrawBezierCurve(List<Vector3> points, LineRenderer lineRenderer, int segments = 20)
    {
        if (points.Count < 2) return;

        List<Vector3> curvePoints = new List<Vector3>();

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = points[i];
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];
            Vector3 p3 = points[i + 1];

            if (i > 0) p1 = points[i - 1];
            if (i < points.Count - 2) p3 = points[i + 2];

            for (int j = 0; j <= segments; j++)
            {
                float t = j / (float)segments;
                curvePoints.Add(CalculateCubicBezierPoint(t, p0, p1, p2, p3));
            }
        }

        lineRenderer.positionCount = curvePoints.Count;
        lineRenderer.SetPositions(curvePoints.ToArray());
    }

    public static void DrawLerpSmoothedLine(List<Vector3> points, LineRenderer lineRenderer, int segments = 5)
    {
        if (points.Count < 2) return;

        List<Vector3> smoothedPoints = new List<Vector3>();

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 start = points[i];
            Vector3 end = points[i + 1];

            for (int j = 0; j <= segments; j++)
            {
                float t = j / (float)segments;
                smoothedPoints.Add(Vector3.Lerp(start, end, t));
            }
        }

        lineRenderer.positionCount = smoothedPoints.Count;
        lineRenderer.SetPositions(smoothedPoints.ToArray());
    }

    public static void DrawStraightLine(List<Vector3> points, LineRenderer lineRenderer)
    {
        if (points.Count < 2) return;

        Vector3[] straightPoints = new Vector3[2];
        straightPoints[0] = points[0];
        straightPoints[1] = points[points.Count - 1];

        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(straightPoints);
    }
}
