using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
public class SettlementPath
{
    public enum PrefDir
    {
        X, Z
    }

    public Vec2i Start;
    public Vec2i End;
    public int Width;
    public Vec2i[] Nodes;



    /// <summary>
    /// An array representing all free points on the edge of this path
    /// </summary>
    public Vec2i[] FreePoints;


    public SettlementPath(Vec2i start, Vec2i end, int width)
    {
        Start = start;
        End = end;
    }

    public void DoublePath(float cut1=0.3f, float cut2 = 0.6f, PrefDir prefDir = PrefDir.X)
    {
        //Delta value of x and z from Start->End
        int deltaX = End.x - Start.x;
        int deltaZ = End.z - Start.z;
        //Direction that end lies in, relative to start
        int dx = deltaX == 0 ? 0 : (int)Mathf.Sign(deltaX);
        int dz = deltaZ == 0 ? 0 : (int)Mathf.Sign(deltaZ);
        if (dx == 0 || dz == 0)
        {
            Nodes = new Vec2i[(int)Mathf.Abs(deltaX + deltaZ)];
            for (int i = 0; i < Nodes.Length; i++)
            {
                Nodes[i] = Start + new Vec2i(dx * i, dz * i);
            }
        }
        else
        {
            List<Vec2i> path = new List<Vec2i>();
            if (prefDir == PrefDir.X)
            {
                Vec2i v1 = Start + new Vec2i((int)(deltaX * cut1), 0);
                Vec2i v2 = new Vec2i(v1.x, v1.z + (int)(deltaZ * cut2));
                Vec2i v3 = new Vec2i(End.x, v2.z);
                StraightLine(Start, v1, path);
                StraightLine(v1, v2, path, includeStart: false);
                StraightLine(v2, v3, path, includeStart: false);
                StraightLine(v3, End, path, includeStart: false);

            }
            else if (prefDir == PrefDir.Z)
            {
                Vec2i v1 = Start + new Vec2i(0, (int)(deltaZ * cut1));
                Vec2i v2 = new Vec2i(v1.x + deltaX, v1.z);
                Vec2i v3 = new Vec2i(v2.x, End.z);
                StraightLine(Start, v1, path);
                StraightLine(v1, v2, path, includeStart: false);
                StraightLine(v2, v3, path, includeStart: false);
                StraightLine(v3, End, path, includeStart: false);

            }



            Nodes = path.ToArray();
        }
    }
    public void CalculatePath(float cut=1f, PrefDir preDirection = PrefDir.X)
    {
        //Delta value of x and z from Start->End
        int deltaX = End.x - Start.x;
        int deltaZ = End.z - Start.z;
        //Direction that end lies in, relative to start
        int dx = deltaX == 0 ? 0 : (int)Mathf.Sign(deltaX);
        int dz = deltaZ == 0 ? 0 : (int)Mathf.Sign(deltaZ);
        if(dx == 0 || dz == 0)
        {
            Nodes = new Vec2i[(int)Mathf.Abs(deltaX + deltaZ)];
            for(int i=0; i<Nodes.Length; i++)
            {
                Nodes[i] = Start + new Vec2i(dx * i, dz * i);
            }
        }
        else
        {
            List<Vec2i> path = new List<Vec2i>();
            if(preDirection == PrefDir.X)
            {
                Vec2i v1 = Start + new Vec2i((int)(deltaX * cut), 0);
                Vec2i v2 = new Vec2i(v1.x, v1.z + deltaZ);
                StraightLine(Start,v1, path);
                StraightLine(v1, v2, path, includeStart:false);
                StraightLine(v2, End, path, includeStart:false);
            }else if (preDirection == PrefDir.Z)
            {
                Vec2i v1 = Start + new Vec2i(0, (int)(deltaZ * cut));
                Vec2i v2 = new Vec2i(v1.x + deltaX, v1.z);
                StraightLine(Start, v1, path);
                StraightLine(v1, v2, path, includeStart: false);
                StraightLine(v2, End, path, includeStart: false);
            }



            Nodes = path.ToArray();
        }
    }

    public void CalculateFreePoints(SettlementBuilder2 builder)
    {

        List<Vec2i> freePoints = new List<Vec2i>();
        for(int i=0; i<Nodes.Length-1; i++)
        {
            //Find direction
            Vec2i dir = Nodes[i + 1] - Nodes[i];
            Vec2i perp = GetPerpendicularDirection(dir);
            int length = Width / 2;
            Vec2i pos = Nodes[i] + perp * length;
            Vec2i neg = Nodes[i] - perp * length;

            //if (Array.IndexOf(Nodes, pos) == -1)
            //{
                if(builder.IsTileFree(pos.x, pos.z))
                    freePoints.Add(pos);
            //}

            //if (Array.IndexOf(Nodes, neg) == -1)
           // {
                if (builder.IsTileFree(neg.x, neg.z))
                    freePoints.Add(neg);
            //}

        }
        FreePoints = freePoints.ToArray();

    }

    public List<Vec2i> PathPoints()
    {
        List<Vec2i> path = new List<Vec2i>();


        return null;
    }


    private static void StraightLine(Vec2i start, Vec2i end, List<Vec2i> path, bool includeStart=true, bool includeEnd=true)
    {
        //Delta value of x and z from Start->End
        int deltaX = end.x - start.x;
        int deltaZ = end.z - start.z;
        //Direction that end lies in, relative to start
        int dx = deltaX == 0 ? 0 : (int)Mathf.Sign(deltaX);
        int dz = deltaZ == 0 ? 0 : (int)Mathf.Sign(deltaZ);
        if (dx == 0 || dz == 0)
        {
            int length = (int)Mathf.Abs(deltaX + deltaZ);
            for (int i = 0; i < length; i++)
            {
                /*
                if (i == 0 && !includeStart)
                    continue;
                if (i == length - 1 && !includeEnd)
                    continue;*/
                path.Add(start + new Vec2i(dx * i, dz * i));
            }
        }
    }
    private static Vec2i GetPerpendicularDirection(Vec2i v)
    {
        if (v.x == 0 && v.z == 0)
            return v;
        if (v.x != 0 && v.z != 0)
            return new Vec2i(0, 0);
        return new Vec2i(v.x == 0 ? 1 : 0, v.z == 0 ? 1:0);
    }
}
public class PathNode
{

    public Vec2i Position;

    public Vec2i[] AdjacentPoints;


    

    public PathNode(Vec2i position, Vec2i forward, int width)
    {

    }
}