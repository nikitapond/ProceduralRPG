using UnityEngine;
using UnityEditor;
[System.Serializable]
public class CaveDungeonEntrance : DungeonEntrance
{
    public CaveDungeonEntrance(Vec2i worldPosition, Dungeon dungeon = null, WorldObjectMetaData meta = null) : 
        base(worldPosition, DungeonType.CAVE, dungeon, meta)
    {
    }


    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new CaveDungeonEntrance(pos, Dungeon);

    }

    /// <summary>
    /// Checks if the entity is allowed to enter
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public override bool CanEntityEnter(Entity entity)
    {
        if (Dungeon == null)
        {
            Debug.Log("Entity " + entity + " Cannot enter dungeon - null");
            return false;
        }

            
        if (!Dungeon.IsLocked)
            return true;
        if (Dungeon.RequiresKey())
        {
            if (entity.Inventory.ContainsItemStack(Dungeon.GetKey()) == null)
            {
                Debug.Log("Do not have required key");
                return false;
            }
        }
        return true;
    }

    public override WorldObject CreateWorldObject(Transform transform = null)
    {
        WorldObject baseObj = new EmptyObjectBase(WorldPosition, meta: MetaData).CreateWorldObject(transform);
        Vec2i dir = new Vec2i(0, 1);
        if (HasMetaData() && GetMetaData().Direction != null)
            dir = GetMetaData().Direction;
        WorldObjectMetaData door1Dir = new WorldObjectMetaData(direction: Vec2i.Rotate(new Vec2i(0, 1), dir));
        WorldObjectMetaData door2Dir = new WorldObjectMetaData(direction: Vec2i.Rotate(new Vec2i(0, -1), dir));

        WorldObject baseDoor = new WoodDoor(new Vec2i(1, 0), door1Dir, onDoorOpen: OnEntityEnter).CreateWorldObject(baseObj.transform);
        WorldObject secondDoor = new WoodDoor(new Vec2i(3, 0), door2Dir, onDoorOpen: OnEntityEnter).CreateWorldObject(baseObj.transform);

        Vec2i[] rockPos = new Vec2i[] {new Vec2i(0,0), new Vec2i(0, 1), new Vec2i(0, 2), new Vec2i(0, 3),
            new Vec2i(1, 3), new Vec2i(2, 3),new Vec2i(3, 3),new Vec2i(3, 3),new Vec2i(3, 2),new Vec2i(3, 1),new Vec2i(3, 0) };
        GenerationRandom genRan = new GenerationRandom(WorldPosition.x << 16 + WorldPosition.z);
        foreach (Vec2i v in rockPos)
        {
            for (int y = 0; y < 3; y++)
            {
                Rock r = new Rock(v, Vector3.up * y + new Vector3(1, 0, 1) * 0.5f, rockSize: 1.5f);
                r.SetRandom(genRan);
                r.CreateWorldObject(baseObj.transform);
            }
        }
        for (int x = 1; x < 4; x++)
        {
            for (int z = 1; z < 4; z++)
            {
                new Rock(new Vec2i(x, z), new Vector3(0, 2, 0), rockSize: 2).CreateWorldObject(baseObj.transform);
            }
        }

        return baseObj;
    }



}