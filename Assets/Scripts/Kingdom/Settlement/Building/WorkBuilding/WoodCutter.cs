using UnityEngine;
using UnityEditor;

public class WoodCutter : Building, IWorkBuilding
{
    public static BuildingPlan BuildingPlan = new BuildingPlan("Wood Cutter", 8, 16);
    public WoodCutter(int width, int height, Vec2i[] boundingWall = null) : base(width, height, boundingWall)
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