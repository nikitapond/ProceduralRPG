using UnityEngine;
using UnityEditor;

public class LumberMill : Building, IWorkBuilding
{

    public static BuildingPlan BuildingPlan = new BuildingPlan("Lumber Mill", 16, 20);
    public LumberMill(int width, int height, Vec2i[] boundingWall = null) : base(width, height, boundingWall)
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