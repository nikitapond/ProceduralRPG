using UnityEngine;
using UnityEditor;


[System.Serializable]
public class ChunkBase
{
    public Vec2i Position { get; private set; }
    public bool IsLand { get; private set; }
    public bool IsBorder { get; private set; }
    public ChunkBiome Biome { get; private set; }
    public ChunkStructure ChunkStructure { get; private set; }
    public RiverNode RiverNode { get; private set; }

    public WetLand WetLand { get; private set; }

    public Lake Lake { get; private set; }

    //Perhaps unneeded (TODO-Check if can delete)
    public Kingdom Kingdom;
    public bool HasKingdom { get { return Kingdom != null; } }

    public bool HasSettlement { get; private set; }

    public float BaseHeight { get; private set; }
    public ChunkBase(Vec2i position, float baseHeight=5, bool isLand=false)
    {
        Position = position;
        IsLand = isLand;
        if (!isLand)
            Biome = ChunkBiome.ocean;
        else
            Biome = ChunkBiome.grassland;

        BaseHeight = baseHeight;
    }

    public void SetHasSettlement(bool set)
    {
        HasSettlement = set;
    }
    public void SetLand(bool isLand)
    {
        IsLand = isLand;
    }
    public void SetBiome(ChunkBiome biome)
    {
        Biome = biome;
    }
    public void SetBorder(bool border)
    {
        IsBorder = border;
    }


    /// <summary>
    /// Checks if the node specified is valid. If so, sets this chunks river to this one.
    /// </summary>
    /// <param name="node"></param>
    public void AddRiver(RiverNode node)
    {
        if(node.NodePosition != Position)
        {
            throw new System.Exception("River Node must have same position as chunk it's added to");
        }
        RiverNode = node;
    }

    /// <summary>
    /// Checks if the lake is within the relevent distance, if it is then it adds
    /// the lake to this chunk
    /// </summary>
    /// <param name="lake"></param>
    public void AddLake(Lake lake)
    {
        if(Vec2i.QuickDistance(Position, lake.Centre) > lake.Radius * lake.Radius)
        {
            return;
        }
        Lake = lake;
    }

    public void AddChunkStructure(ChunkStructure cStruct)
    {
        ChunkStructure = cStruct;
    }

    public override string ToString()
    {
        return "CB:" + this.Position.ToString();
    }


}