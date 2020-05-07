using UnityEngine;
using UnityEditor;
[System.Serializable]
public class House : Building
{
    public static BuildingPlan BuildingPlan = new BuildingPlan("House", 8, 20);
    public int capacity = 2;
    public House(int width, int height) : base(width, height)
    {
    }

    public override string ToString()
    {
        return "House " + WorldPosition;
    }
}