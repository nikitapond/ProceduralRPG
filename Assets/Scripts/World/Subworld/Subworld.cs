using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// A subworld is a seperate world that the player (or entities) can enter.
/// It contains a 2d array of chunks, which are all loaded on entrance.
/// A subworld also contains a list of entities, all of which are loaded when entering the subworld.
/// </summary>
[System.Serializable]
public class Subworld
{


    public ChunkData[,] SubworldChunks { get; protected set; }
    public Vec2i ExternalEntrancePos { get; protected set; } //Where in the world the dungeon entrance/exit is

    public ISubworldEntranceObject Exit; //The object that is used to exit the subworld, its ID must be set to this ID.


    public Vec2i InternalEntrancePos { get; protected set; } //Where in the local dungeon space the player goes when entering

    public Vec2i ChunkSize { get; private set; }
    public int SubworldID { get; private set; }


    public Subworld(ChunkData[,] subChunks, Vec2i internalEntrance, Vec2i externalEtrnace)
    {
        SubworldChunks = subChunks;
        ExternalEntrancePos = externalEtrnace;
        InternalEntrancePos = internalEntrance;
        ChunkSize = new Vec2i(subChunks.GetLength(0), subChunks.GetLength(1));


    }
    public void SetSubworldID(int id)
    {
        if (Exit == null)
        {
            throw new System.Exception("Can only set ID of subworld once entrance is set");
        }
        SubworldID = id;
        Exit.SetSubworld(this);


    }

    public void SetWorldEntrance(Vec2i ent)
    {
        InternalEntrancePos = ent;
    }

    public ChunkData GetChunkSafe(int x, int z)
    {
        if (SubworldChunks.GetLength(0) <= x || x < 0)
            return null;
        if (SubworldChunks.GetLength(1) <= z || z < 0)
            return null;
        return SubworldChunks[x, z];
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        Subworld ot = obj as Subworld;
        if (ot == null)
            return false;
        return ot.SubworldID == SubworldID;
    }
}