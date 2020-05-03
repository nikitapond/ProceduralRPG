using UnityEngine;
using UnityEditor;

public class LineI
{

    public Vec2i Start, End;
    public float Mag { get; private set; }
    public Vector2 Grad { get; private set; }
    public LineI(Vec2i start, Vec2i end)
    {
        Start = start;
        End = end;
        Grad = (end - start).AsVector2();
        Mag = Grad.magnitude;
        Grad /= Mag;
    }


   

    public bool Intersects(LineI l2)
    {
        return doIntersect(Start, End, l2.Start, l2.End);
    }

    static bool onSegment(Vec2i p, Vec2i q, Vec2i r)
    {
        if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
            q.z <= Mathf.Max(p.z, r.z) && q.z >= Mathf.Min(p.z, r.z))
            return true;

        return false;
    }

    // To find orientation of ordered triplet (p, q, r). 
    // The function returns following values 
    // 0 --> p, q and r are colinear 
    // 1 --> Clockwise 
    // 2 --> Counterclockwise 
    static int orientation(Vec2i p, Vec2i q, Vec2i r)
    {
        // See https://www.geeksforgeeks.org/orientation-3-ordered-points/ 
        // for details of below formula. 
        int val = (q.z - p.z) * (r.x - q.x) -
                (q.x - p.x) * (r.z - q.z);

        if (val == 0) return 0; // colinear 

        return (val > 0) ? 1 : 2; // clock or counterclock wise 
    }

    // The main function that returns true if line segment 'p1q1' 
    // and 'p2q2' intersect. 
    static bool doIntersect(Vec2i p1, Vec2i q1, Vec2i p2, Vec2i q2)
    {
        // Find the four orientations needed for general and 
        // special cases 
        int o1 = orientation(p1, q1, p2);
        int o2 = orientation(p1, q1, q2);
        int o3 = orientation(p2, q2, p1);
        int o4 = orientation(p2, q2, q1);

        // General case 
        if (o1 != o2 && o3 != o4)
            return true;

        // Special Cases 
        // p1, q1 and p2 are colinear and p2 lies on segment p1q1 
        if (o1 == 0 && onSegment(p1, p2, q1)) return true;

        // p1, q1 and q2 are colinear and q2 lies on segment p1q1 
        if (o2 == 0 && onSegment(p1, q2, q1)) return true;

        // p2, q2 and p1 are colinear and p1 lies on segment p2q2 
        if (o3 == 0 && onSegment(p2, p1, q2)) return true;

        // p2, q2 and q1 are colinear and q1 lies on segment p2q2 
        if (o4 == 0 && onSegment(p2, q1, q2)) return true;

        return false; // Doesn't fall in any of the above cases 
    }
}