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

    public Vec2i[] BoundedPoints { get; private set; }
    public Vec2i[] BoundaryPoints { get; private set; }
    public Recti(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        List<Vec2i> BoundedPoints = new List<Vec2i>();
        List<Vec2i> BoundaryPoints = new List<Vec2i>();
        for(int x_=x; x_<x+width; x_++)
        {
            for(int y_=y; y_<y+height; y_++)
            {
                if(x_==x || x_==x+width-1 || y_==y || y_ == y + height - 1)
                {
                    BoundaryPoints.Add(new Vec2i(x_, y_));
                }
                else
                {
                    BoundedPoints.Add(new Vec2i(x_, y_));
                }
            }
        }
        this.BoundaryPoints = BoundaryPoints.ToArray();
        this.BoundedPoints = BoundedPoints.ToArray();
    }

    public bool ContainsPoint(int x, int z)
    {
        return x >= X && x <= X + Width && z >= Y && z <= Y + Height;
    }

    public bool ContainsPoint(Vec2i v)
    {
        return System.Array.IndexOf(BoundedPoints, v) > -1;
        return v.x >= X && v.x < X + Width && v.z >= Y && v.z < Y + Height;
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