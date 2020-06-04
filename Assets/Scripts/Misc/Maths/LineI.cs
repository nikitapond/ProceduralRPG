using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class LineI
{

    public Vec2i Start, End;
    public float Mag { get; private set; }
    public Vector2 Grad { get; private set; }

    /// <summary>
    /// Coefficients that define the equation of this line:
    /// Ax + By + C = 0
    /// </summary>
    private float a, b, c;
    public LineI(Vec2i start, Vec2i end)
    {
        Start = start;
        End = end;
        Grad = (end - start).AsVector2();
        Mag = Grad.magnitude;
        Grad /= Mag;
        int dx = end.x - start.x;
        int dz = end.z - start.z;
        b = dx;
        a = -dz;
        c = dz * start.x - dx * start.z;

    }


    public List<Vec2i> ConnectPoints()
    {
        List<Vec2i> points = new List<Vec2i>();
        int x = Start.x;
        int z = Start.z;

        int xDir = (int)Mathf.Sign(End.x - Start.x);
        int zDir = (int)Mathf.Sign(End.z - Start.z);
        points.Add(new Vec2i(x, z));
        while (!(x == End.x && z == End.z))
        {
            //data[x, z] = copy.Copy(globalPos + new Vec2i(x, z));



            int dx = (int)Mathf.Abs(End.x - x);
            int dz = (int)Mathf.Abs(End.z - z);

            if (dx > dz)
            {
                x += xDir;
            }
            else if (dz > dx)
            {
                z += zDir;

            }
            else
            {
                x += xDir;
                z += zDir;
            }

            points.Add(new Vec2i(x, z));
        }


        /*
        Vector2 disp = new Vector2(End.x - Start.x, End.z - Start.z);
        Vector2 grad = disp.normalized;

        int size = (int)disp.magnitude + 1;
       
        int x = Start.x;
        int z = Start.z;
        float dx = x;
        float dz = z;
        float deltaX = Mathf.Abs(grad.x);
        float deltaZ = Mathf.Abs(grad.y);

        int xIncr = (int)Mathf.Sign(grad.x);
        if (deltaX < 0.0001f)
            xIncr = 0;
        int zIncr = (int)Mathf.Sign(grad.y);
        if (deltaZ < 0.0001f)
            zIncr = 0;
        List<Vec2i> points = new List<Vec2i>();

        for(int i=0; i<size * 2; i++)
        {
            dx += grad.x;
            dz += grad.y;
            int x_ = (int)dx;
            int z_ = (int)dz;

            if (x_ != x)
                x = x_;
            if (z_ != z)
                z = z_;
            points.Add(new Vec2i(x, z));
            if (x == End.x && z == End.z)
            {
                break;
            }
            /*
            dx += deltaX;
            dz += deltaZ;
            if(dx > 1)
            {
                x += xIncr;
                dx = 0;
            }
            if(dz > 1) {
                z += zIncr;
                dz = 0;
            }
            /*
            if(dx > dz)
            {
                x+= xIncr;
                dx = 0;
            }else if(dz > dx)
            {
                z+= zIncr;
                dz = 0;
            }
            else
            {
                x+=xIncr;
                z+=zIncr;
                dz = 0;
                dx = 0;
            }
           
            points.Add(new Vec2i(x, z));
            if(x == End.x && z == End.z)
            {
                break;
            }*/

        
        return points;

    }

    /// <summary>
    /// returns the shortest distance from the specified point
    /// to this line
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
   public float Distance(Vec2i point)
    {
        return Mathf.Abs(a * point.x + b * point.z + c) / Mathf.Sqrt(a * a + b * b);
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