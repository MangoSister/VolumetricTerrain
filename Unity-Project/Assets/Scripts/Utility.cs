using UnityEngine;
using System.Collections;

public static class MathHelper
{
    public static bool InfLineIntersection(Vector2 l1p1, Vector2 l1p2, Vector2 l2p1, Vector2 l2p2, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        float a1 = l1p2.y - l1p1.y;
        float b1 = l1p1.x - l1p2.x;
        float a2 = l2p2.y - l2p1.y;
        float b2 = l2p1.x - l2p2.x;
        float det = a1 * b2 - a2 * b1; //determinant (denominator)

        if (det == 0f) return false;//coincidence or parallel, two segments on the same line
        float c1 = a1 * l1p1.x + b1 * l1p1.y;
        float c2 = a2 * l2p1.x + b2 * l2p1.y;
        float det1 = c1 * b2 - c2 * b1; //determinant (numerator 2)
        float det2 = a1 * c2 - a2 * c1; //determinant (numerator 1)
        intersection.x = det1 / det;
        intersection.y = det2 / det;
        return true;
    }
}
