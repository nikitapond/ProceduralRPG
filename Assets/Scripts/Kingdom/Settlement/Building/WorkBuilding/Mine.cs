using UnityEngine;
using UnityEditor;

public class Mine : Building, IWorkBuilding
{
    public static BuildingPlan IronMine = new BuildingPlan("Iron Mine", 8, 10);
    public static BuildingPlan SilverMine = new BuildingPlan("Silver Mine", 8, 10);
    public static BuildingPlan GoldMine = new BuildingPlan("Gold Mine", 8, 10);
    public Mine(int width, int height, Vec2i[] boundingWall = null) : base(width, height, boundingWall)
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