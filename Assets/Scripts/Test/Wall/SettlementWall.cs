using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class SettlementWall
{

    public List<Vec2i> WallPoints;
    public SettlementWall(List<Vec2i> wallPoints)
    {

        this.WallPoints = wallPoints;
    }

}