using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// An area defined by a rectangle with only integer values allowed
/// </summary>
/// 

[System.Serializable]
public class Recti
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }



    public Recti(Vec2i min, Vec2i size)
    {
        X = min.x;
        Y = min.z;
        Width = size.x;
        Height = size.z;
    }
    public Recti(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;

    }


    public bool ContainsPoint(int x, int z)
    {
        return x >= X && x <= X + Width && z >= Y && z <= Y + Height;
    }

    public bool ContainsPoint(Vec2i v)
    {
        return v.x >= X && v.x <= X + Width && v.z >= Y && v.z <= Y + Height;
    }



    public bool Intersects(Recti r)
    {
        if (r.X > X + Width || r.X + r.Width < X)
            return false;
        if (r.Y > Y + Height || r.Y + r.Height < Y)
            return false;
        return true;
    }

    public override string ToString()
    {
        return "Recti: " + X + "," + Y + " - " + Width + "," + Height;
    }

}