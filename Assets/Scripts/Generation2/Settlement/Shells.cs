using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
public abstract class Shell
{

    public abstract Vec2i GetSize();

    public GridPoint GridPoint { get; private set; }
    public Vec2i ChunkPosition { get { return GridPoint.ChunkPos; } }

    public List<Shell> NearestNeighbors { get; private set; }

    public SettlementGenerator2.LocationData LocationData { get; private set; }
    /// <summary>
    /// Array containing all chunk bases associated with this shell
    /// Used mainly to pass terrain data - namely rivers and biomes - 
    /// to the settlement builder
    /// </summary>
    public ChunkBase2[,] ChunkBases { get; private set; }

    public int KingdomID { get; private set; }
    public Kingdom GetKingdom() { return World.Instance?.GetKingdom(KingdomID); }
    public Shell(GridPoint gp, int kingdomID)
    {
        GridPoint = gp;
        gp.Shell = this;
        KingdomID = kingdomID;
    }

    public void SetNearestNeighbors(List<Shell> nn)
    {
        NearestNeighbors = nn;
    }

    public void SetLocationData(SettlementGenerator2.LocationData ld)
    {
        LocationData = ld;
    }
    public void SetChunkBases(ChunkBase2[,] cBases)
    {
        ChunkBases = cBases;
    }
}

/// <summary>
/// Contains all generation information for a settlement, except chunk data and entities
/// Used to contain this chunk information until the settlement has been generated fully
/// </summary>
public class SettlementShell : Shell
{
    public SettlementType Type { get; private set; }
    public List<BuildingPlan> RequiredBuildings;

    public SettlementEconomy Economy;

    public SettlementShell(GridPoint gp, int kingdomID, SettlementType type) : base(gp, kingdomID) {
        Type = type;
    }

    public override Vec2i GetSize()
    {
        return new Vec2i(1,1) * Type.GetSize();
    }
}
public class TacticalLocationShell : Shell
{
    public TacLocType Type { get; private set; }
    public TacticalLocationShell(GridPoint gp, int kingdomID, TacLocType tacType) : base(gp, kingdomID) { Type = tacType; }

    public override Vec2i GetSize()
    {
        switch (Type)
        {
            case TacLocType.tower:
                return new Vec2i(2, 2);
            case TacLocType.fort:
                return new Vec2i(4, 4);
        }
        //Shouldn't happen
        return new Vec2i(0, 0);
    }
}
