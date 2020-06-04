using UnityEngine;
using UnityEditor;

public class Smelter : Building, IWorkBuilding
{
    public static BuildingPlan BuildingPlan = new BuildingPlan("Smelter", 8, 12);
    public Smelter(int width, int height, Vec2i[] boundingWall = null) : base(width, height, boundingWall)
    {
    }

    private WorkBuildingData WBD;

    public WorkBuildingData GetWorkData => WBD;

    public Building WorkBuilding => this;

    public void SetWorkBuildingData(WorkBuildingData data)
    {
        WBD = data;
    }
}