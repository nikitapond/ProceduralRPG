﻿using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
public abstract class Shell
{
    public GridPoint GridPoint { get; private set; }
    public Vec2i ChunkPosition { get { return GridPoint.ChunkPos; } }

    public List<Shell> NearestNeighbors { get; private set; }

    public SettlementGenerator2.LocationData LocationData { get; private set; }


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

   

}
public class TacticalLocationShell : Shell
{
    public TacLocType Type { get; private set; }
    public TacticalLocationShell(GridPoint gp, int kingdomID, TacLocType tacType) : base(gp, kingdomID) { Type = tacType; }
  
}
