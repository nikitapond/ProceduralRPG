using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Base class for all World Objects
/// </summary>
public class WorldObject : MonoBehaviour
{
    public static int GROUND_LAYER_MASK = -1;
    public static int ObjectPositionHash(int x, int z)
    {
        return (x & (World.ChunkSize-1))*World.ChunkSize + (z & (World.ChunkSize - 1));
    }
    public static int ObjectPositionHash(Vec2i v)
    {
        return (v.x & (World.ChunkSize - 1)) * World.ChunkSize + (v.z & (World.ChunkSize - 1));
    }
    public static WorldObject CreateWorldObject(WorldObjectData data, Transform parent=null, float heightOffset=0)
    {

        if(GROUND_LAYER_MASK == -1)
            GROUND_LAYER_MASK = LayerMask.GetMask("Ground");

        GameObject gameObject = Instantiate(data.ObjectPrefab);
        //gameObject.layer = 8;
        WorldObject obj = gameObject.GetComponent<WorldObject>();        

        if(parent != null)
        {
            gameObject.transform.parent = parent;
        }
        gameObject.transform.RotateAround(data.Position + data.Size / 2, Vector3.up, data.Rotation);
        gameObject.transform.localPosition = data.Position.Mod(World.ChunkSize);
        gameObject.transform.localScale = data.Scale;
        obj.Data = data;
        obj.Data.LoadedObject = obj;
        data.OnObjectLoad(obj);
        obj.CoroutineAdjust();
        return obj;
    }



    public void AdjustHeight()
    {
        Vector3 basePos = new Vector3(transform.position.x,256, transform.position.z);
        RaycastHit hit;
        if (Physics.Raycast(new Ray(basePos, Vector3.down), out hit, 257, layerMask: GROUND_LAYER_MASK))
        {
           
            basePos.y = hit.point.y + Data.Position.y;
            transform.position = basePos;
        }
        else
        {
            StartCoroutine(WaitAndAdjust());

        }

    }

    private void CoroutineAdjust()
    {
        StartCoroutine(WaitAndAdjust());
    }

    private IEnumerator WaitAndAdjust()
    {
        yield return new WaitForSeconds(0.5f);
        AdjustHeight();
    }


    public static GameObject InstansiatePrefab(GameObject source, Transform parent = null)
    {
        GameObject obj = Instantiate(source);
        if (parent != null)
            obj.transform.parent = parent;
        return obj;
    }


    public WorldObjectData Data { get; private set; }


    public void OnEntityInteract(Entity ent)
    {
        if (ent is Player)
            return;
        if(Data is IOnEntityInteract)
        {
            (Data as IOnEntityInteract).OnEntityInteract(ent);
        }else if(Data is ISubworldEntranceObject)
        {
            ISubworldEntranceObject ob = Data as ISubworldEntranceObject;
            Key key = ob.GetSubworldKey();
            if(key == null || ent.Inventory.ContainsItemStack(key)!=null)
            {

                Subworld current = ent.GetSubworld();
                if (current == null)
                    WorldManager.Instance.EntityEnterSubworld(ent, ob.GetSubworld());
                else
                    WorldManager.Instance.EntityExitSubworld(ent, ob.GetSubworld());
            }

        }
    }

    private void OnDestroy()
    {
        if(Data != null)
            Data.OnObjectUnload(this);
    }
    public bool RePlace = false;
    private void Update()
    {
        if (RePlace)
        {
            RePlace = false;
            AdjustHeight();
        }
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
    WEAPON_STAND,
    ARMOUR_STAND, 
    GRASS,
    ROCK,
    WOOD_SPIKE,
    GLASS_WINDOW,
    ROOF,
    DOOR,
    DUNGEON_ENTRANCE,
    MARKET_STALL,
    BED,
    DOUBLE_BED,
    PRACTISE_DUMMY,
    BANDIT_GAURD_TOWER,
    CHEST,
    WALL_TORCH,
    FIRE_PLACE,
    TRAINING_DUMMY,
    TRAP_DOOR,
    NO_WALK_ZONE



}