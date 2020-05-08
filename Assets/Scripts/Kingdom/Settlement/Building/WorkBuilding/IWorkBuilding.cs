using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// If a work building is a store, then it can sell items.
/// </summary>
public interface IStore
{/*
    public abstract Inventory GetSellable();
    public void RefreshStock();
    public List<NPC> GetShopMerchants();
    */
}
public interface IWorkBuilding
{

    /// <summary>
    /// Each work building must hold onto a WorkBuildingData struct
    /// </summary>
    WorkBuildingData GetWorkData { get; }

    void SetWorkBuildingData(WorkBuildingData data);
    Building WorkBuilding { get; }

}

public static class WorkBuildingHelper
{
    public static Building AsBuilding(this IWorkBuilding b)
    {
        return b as Building;
    }
}

/// <summary>
/// Contains details related to a workbuilding
/// </summary>
[System.Serializable]
public struct WorkBuildingData
{
    
    public NPCJob[] BuildingJobs { get; private set; }
    public int WorkCapacity { get { return BuildingJobs.Length; } }
    public Inventory WorkInventory { get; private set; }

    public WorkBuildingData(NPCJob[] buildingJobs)
    {
        BuildingJobs = buildingJobs;
        WorkInventory = new Inventory();
    }

}