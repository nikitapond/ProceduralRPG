using UnityEngine;
using UnityEditor;

[System.Serializable]
public class Vec2i
{
    public int x;
    public int z;

    public Vec2i(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static readonly Vec2i Forward = new Vec2i(0, 1);

    public int QuickDistance(Vec2i b)
    {
        return Vec2i.QuickDistance(this, b);
    }

    public static Vec2i Rotate(Vec2i initial, Vec2i rotate)
    {
        float angle = Vector2.SignedAngle(Vector2.up, rotate.AsVector2());
        Vector2 final = Quaternion.Euler(0, 0, angle) * initial.AsVector2();
        return Vec2i.FromVector2(final);
       
    }
    /// <summary>
    /// Returns the angle (in degrees) between the two vectors
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static float Angle(Vec2i a, Vec2i b)
    {
        if (a.x == b.x && a.z == b.z)
            return 0;

        float ang = Mathf.Atan2(a.x * b.z - a.z * b.x, a.x * b.x + a.z * b.z);
        ang *= Mathf.Rad2Deg;
        int angInt = (int)ang;
        if (angInt == 90 || angInt == -90)
            angInt *= -1;
        return angInt;
            
        float dot = a.x * b.x + a.z * b.z;
        float mag = Mathf.Sqrt((a.x*a.x + a.z*a.z) * ( b.x*b.x + b.z*b.z));
        float angleRad = Mathf.Acos(dot / mag);
        return angleRad * Mathf.Rad2Deg;
    }

    public Vector2 AsVector2()
    {
        return Vec2i.ToVector2(this);
    }
    public Vector3 AsVector3()
    {
        return Vec2i.ToVector3(this);
    }


    public static Vec2i operator *(Vec2i a, int s)
    {
        return new Vec2i(a.x * s, a.z * s);
    }
    public static Vec2i operator /(Vec2i a, int s)
    {
        return new Vec2i(a.x / s, a.z / s);
    }
    public static Vec2i operator +(Vec2i a, Vec2i b)
    {
        return new Vec2i(a.x + b.x, a.z + b.z);
    }
    public static Vec2i operator -(Vec2i a, Vec2i b)
    {
        return new Vec2i(a.x - b.x, a.z - b.z);
    }
    public static bool operator ==(Vec2i a, Vec2i b)
    {
        if (System.Object.ReferenceEquals(a, null))
        {
            if (System.Object.ReferenceEquals(b, null))
            {
                return true;
            }
            return false;
        }
        return a.Equals(b);
    }
    public static bool operator !=(Vec2i a, Vec2i b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        if (!(obj is Vec2i))
            return false;

        Vec2i o = obj as Vec2i;

        return x == o.x && z == o.z;

    }

  

    public static Vector2 ToVector2(Vec2i v)
    {
        return new Vector2(v.x, v.z);
    }
    public static Vector3 ToVector3(Vec2i v)
    {
        return new Vector3(v.x, 0, v.z);
    }
    public static Vec2i FromVector3(Vector3 v)
    {
        return new Vec2i((int)v.x, (int)v.z);
    }
    public static Vec2i FromVector2(Vector2 v)
    {
        return new Vec2i((int)v.x, (int)v.y);
    }


    public static Vec2i Clamp(Vec2i original, int minX, int minZ, int maxX, int maxZ)
    {
        Vec2i clamped = new Vec2i(original.x, original.z);
        if (clamped.x < minX)
            clamped.x = minX;
        else if (clamped.x > maxX)
            clamped.x = maxX;

        if (clamped.z < minZ)
            clamped.z = minZ;
        else if (clamped.z > maxZ)
            clamped.z = maxZ;

        return clamped;
    }

    public static int QuickDistance(Vec2i a, Vec2i b)
    {
        return (a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z);
    }

    public override int GetHashCode()
    {
        return (x << 16) + z; //Allows x and z to be 16 bit values
       
    }
    public override string ToString()
    {
        return "("+x + "," + z+")";
    }
}