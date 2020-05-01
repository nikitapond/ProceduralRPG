using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AbandonedCastleBuilder : GenerationBase, IChunkStructureGenerationBase
{
    private IInventoryObject MainLootChest;
    public AbandonedCastleBuilder(AbandonedCastle cast, GenerationRandom genRan) : base (cast.Position, cast.Size, genRan)
    {

    }
    /// <summary>
    /// Main generation function
    /// </summary>
    public override void Generate()
    {

        BoundryPoints = this.DefineRectangularWall(0);
        Vec2i[] internalPoints = DefineRectangularWall(3);
        WorldObjectData wall = new BrickWall(new Vec2i(0, 0));
        for(int i=0; i<4; i++)
        {
            int ip1 = (i + 1) % 4;
            ConnectPointsWithObject(wall, BoundryPoints[i], BoundryPoints[ip1]);
            ConnectPointsWithObject(wall, internalPoints[i], internalPoints[ip1]);
        }
        EntrancePosition = this.ChooseEntrancePoint();
        for (int x=-3; x<=3; x++)
        {
            for (int z = -3; z <= 3; z++)
            {
                if (InRectBounds(x+ EntrancePosition.x, z+ EntrancePosition.z))
                {
                    Objects[x+ EntrancePosition.x, z+ EntrancePosition.z] = null;
                }
            }
        }
        Chest chest = new Chest(BaseCoord + Size / 2);
        Objects[Size.x / 2, Size.z / 2] = chest;
        this.MainLootChest = chest;

    }

    public IInventoryObject GetMainLootChest()
    {
        return MainLootChest;
    }
}