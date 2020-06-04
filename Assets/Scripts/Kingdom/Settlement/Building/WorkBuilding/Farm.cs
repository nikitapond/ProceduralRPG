using UnityEngine;
using UnityEditor;

public class Farm : Building, IWorkBuilding
{

    public static BuildingPlan WheatFarm = new BuildingPlan("Wheat Farm", 12, 20);
    public static BuildingPlan SilkFarm = new BuildingPlan("Silk Farm", 12, 20);
    public static BuildingPlan VegFarm = new BuildingPlan("Vegetable Farm", 12, 20);

    public static BuildingPlan CattleFarm = new BuildingPlan("Cattle Farm", 12, 20);
    public static BuildingPlan SheepFarm = new BuildingPlan("Sheep Farm", 12, 20);
    public Farm(int width, int height, Vec2i[] boundingWall = null) : base(width, height, boundingWall)
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