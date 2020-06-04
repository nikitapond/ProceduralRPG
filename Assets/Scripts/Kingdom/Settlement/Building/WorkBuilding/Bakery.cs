using UnityEngine;
using UnityEditor;

public class Bakery : Building, IWorkBuilding
{
    public static BuildingPlan BuildingPlan = new BuildingPlan("Bakery", 12, 16);
    public Bakery(int width, int height, Vec2i[] boundingWall = null) : base(width, height, boundingWall)
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