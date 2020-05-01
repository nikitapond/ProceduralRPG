using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Base class for all World Objects
/// </summary>
public class WorldObject : MonoBehaviour
{

    public static int ObjectPositionHash(int x, int z)
    {
        return (x & (World.ChunkSize-1))*World.ChunkSize + (z & (World.ChunkSize - 1));
    }
    public static int ObjectPositionHash(Vec2i v)
    {
        return (v.x & (World.ChunkSize - 1)) * World.ChunkSize + (v.z & (World.ChunkSize - 1));
    }
    public static WorldObject CreateWorldObject(WorldObjectData data, Transform parent=null)
    {

        GameObject gameObject = Instantiate(ResourceManager.GetWorldObject(data.ID));
        //gameObject.layer = 8;
        WorldObject obj = gameObject.AddComponent<WorldObject>();
        

        if(parent != null)
        {
            gameObject.transform.parent = parent;
        }
        if (data.HasMetaData())
        {
            if (data.GetMetaData().Direction != null)
            {
                float angle = Vector2.SignedAngle(new Vector2(0, 1), data.GetMetaData().Direction.AsVector2());
                obj.transform.rotation = Quaternion.Euler(0, angle, 0);
            }
        }


        gameObject.transform.localPosition = new Vector3(data.WorldPosition.x%World.ChunkSize, 0, data.WorldPosition.z% World.ChunkSize) + data.ObjectDeltaPosition;
        if(data.Size != null)
        {
            float height = 1;
            if(data.HasMetaData())
            {
                height = data.GetMetaData().Height;
            }
            if(!(data is IMultiTileObject))
                gameObject.transform.localScale = new Vector3(data.Size.x, height, data.Size.z);
        }
        obj.Data = data;
        data.OnObjectLoad(obj);
        return obj;
    }

    public static GameObject InstansiatePrefab(GameObject source, Transform parent = null)
    {
        GameObject obj = Instantiate(source);
        if (parent != null)
            obj.transform.parent = parent;
        return obj;
    }


    public WorldObjectData Data { get; private set; }

    public void SetData(WorldObjectData data)
    {
        Data = data;
    }

    private void OnDestroy()
    {
        if(Data != null)
            Data.OnObjectUnload(this);
    }


    

}
public enum WorldObjects
{
    EMPTY_OBJECT_BASE,
    WALL,
    LOOT_SACK,
    TREE,TREE_CANOPY,TREE_BRANCH,
    BRIDGE, BRIDGE_BASE, BRIDGE_RAMP,
    WATER,
    ANVIL,
    GRASS,
    ROCK,
    WOOD_SPIKE,
    GLASS_WINDOW,
    ROOF,
    DOOR,
    DUNGEON_ENTRANCE,
    MARKET_STALL,
    BED,
    PRACTISE_DUMMY,
    BANDIT_GAURD_TOWER,
    CHEST,



}