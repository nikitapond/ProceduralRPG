using UnityEngine;
using UnityEditor;


public struct Vec3i
{

    public int x, y, z;


    public Vec3i(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }



    public override string ToString()
    {
        return "[" + x + "," + y + "," + z + "]";
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Vec3i))
            return false;
        Vec3i v = (Vec3i)obj;
        return v == this;
    }

    public override int GetHashCode()
    {
        return x + y << 8 + z << 16;
        int result = (int)(x ^ (x >> 32));
        result = 31 * result + (int)(y ^ (y >> 32));
        result = 31 * result + (int)(z ^ (z >> 32));
        return result;

    }

    public static Vec3i operator *(Vec3i a, int s)
    {
        return new Vec3i(a.x * s, a.y*s,  a.z * s);
    }
    public static Vec3i operator /(Vec3i a, int s)
    {
        return new Vec3i(a.x / s, a.y/s, a.z / s);
    }
    public static Vec3i operator +(Vec3i a, Vec3i b)
    {
        return new Vec3i(a.x + b.x, a.y+b.y, a.z + b.z);
    }
    public static Vec3i operator -(Vec3i a, Vec3i b)
    {
        return new Vec3i(a.x - b.x, a.y-b.y, a.z - b.z);
    }
    public static bool operator ==(Vec3i a, Vec3i b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z;
    }
    public static bool operator !=(Vec3i a, Vec3i b)
    {
        return !(a == b);
    }

    public static implicit operator Vector3(Vec3i rValue)
    {
        return new Vector3(rValue.x, rValue.y, rValue.z);
    }
    public static implicit operator Vec3i(Vector3 rValue)
    {
        return new Vec3i((int)rValue.x, (int)rValue.y, (int)rValue.z);
    }
}
public static class Vec3iHelper
{
    public static Vec3i GetPerpendicularDirection(this Vec3i dir)
    {
        int x = dir.x == 0 ? 1 : 0;
        int y = dir.y == 0 ? 1 : 0;
        int z = dir.z == 0 ? 1 : 0;

        return new Vec3i(x, y, z);
    }

    public static int SquareDist(this Vec3i a, Vec3i b)
    {
        return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z);
    }



}