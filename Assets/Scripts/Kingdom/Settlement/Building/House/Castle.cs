using UnityEngine;
using UnityEditor;
/// <summary>
/// Castle exists in the capital and in cities.
/// Very large, very expensive, monarchs live in capital castles
/// nobles live in city castles
/// </summary>
[System.Serializable]
public class Castle : House
{
    public static BuildingPlan BuildingPlanSmall = new BuildingPlan("Castle", 32, 32);
    public static BuildingPlan BuildingPlanBig = new BuildingPlan("Castle", 48, 48);

    public Castle(int width, int height) : base(width, height)
    {
    }
}