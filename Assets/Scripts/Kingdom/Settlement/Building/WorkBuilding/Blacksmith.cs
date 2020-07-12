using UnityEngine;
using UnityEditor;

public class Blacksmith : Building, IWorkBuilding
{
    public static BuildingPlan BuildingPlan = new BuildingPlan("Blacksmith", 12, 20);
    private WorkBuildingData WorkBuildingData;
    public Blacksmith(int width, int height) : base(width, height)
    {
    }

    public WorkBuildingData GetWorkData { get => WorkBuildingData;  }

    public Building WorkBuilding => this;

    public void SetWorkBuildingData(WorkBuildingData data)
    {
        WorkBuildingData = data;
    }
}