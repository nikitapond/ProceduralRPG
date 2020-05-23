using UnityEngine;
using UnityEditor;

public class Tavern : Building, IWorkBuilding
{
    public static BuildingPlan BuildingPlan = new BuildingPlan("Tavern", 12, 22);

    public Tavern(int width, int height, Vec2i[] boundingWall = null) : base(width, height, boundingWall)
    {
    }

    private WorkBuildingData WorkBuildingData;

    public WorkBuildingData GetWorkData => WorkBuildingData;

    public Building WorkBuilding => this;

    public void SetWorkBuildingData(WorkBuildingData data)
    {
        WorkBuildingData = data;
    }
}