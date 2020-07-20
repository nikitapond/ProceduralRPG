using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public abstract class Entity
{


    #region variables
    [SerializeField]
    private SerializableVector3 Position_;
    public Vector3 Position { get { return Position_; } }
    public Vector2 Position2 { get { return new Vector2(Position_.x, Position_.z); } }
    public Vec2i TilePos { get { return new Vec2i((int)Position_.x, (int)Position_.z); } }
    public Vec2i LastChunkPosition { get; protected set; }

    public bool IsAlive { get; private set; }
    public float LookAngle { get; private set; }
    public float fov = 30; //Angle either side of look direction entity can see
    public string Name { get; private set; }
    public int ID { get; private set; }
    public bool IsFixed { get; private set; }
    public abstract string EntityGameObjectSource { get; }

    public EntityCombatManager CombatManager { get; private set; }

    public EntityAI EntityAI;
    /// <summary>
    /// ID of the current subworld the entity is in
    /// </summary>
    public virtual int CurrentSubworldID { get; private set; }





    /// <summary>
    /// Inventory - Current total inventory
    /// </summary>
    public Inventory Inventory { get; private set; }
    public EntityFaction EntityFaction { get; private set; }


    public SkillTree SkillTree { get; private set; }

    public EntityMovementData MovementData { get; private set; }

    [System.NonSerialized]
    private LoadedEntity LoadedEntity;
    public LoadedEntity GetLoadedEntity()
    {
        return LoadedEntity;
    }

    public bool IsLoaded { get { return LoadedEntity != null; } }

    #endregion
    public Entity(EntityCombatAI combatAI, EntityTaskAI taskAI, EntityMovementData movementData, string name = "un-named_entity", bool isFixed = false)
    {
        Name = name;
        IsFixed = isFixed;
        // MoveEntity(Vector3.zero);
        Debug.Log("Base entity created");
        EntityAI = new EntityAI(this, combatAI, taskAI);


        Inventory = new Inventory();
        CombatManager = new EntityCombatManager(this);
        SkillTree = new SkillTree();
        MovementData = movementData;
        CurrentSubworldID = -1;
        IsAlive = true;
    }

    

    /// <summary>
    /// Main update loop for entity, 
    /// </summary>
    public virtual void Update()
    {
        Debug.Log("IsUpdated");
        Vec2i cPos = World.GetChunkPosition(Position);
        if(cPos != LastChunkPosition)
        {

            EntityManager.Instance.UpdateEntityChunk(this, LastChunkPosition, cPos);
            LastChunkPosition = cPos;
        }
        EntityAI.Update();
    }
    /// <summary>
    /// The time since game start that the last tick occured at.
    /// </summary>
    private float LastTick=-1;

    public void Tick()
    {
        if (LastTick == -1)
            LastTick = Time.time;

        float dt = Time.time - LastTick;

        CombatManager.Tick(dt);
        EntityAI.Tick();
        LastTick = Time.time;

    }




    public void OnEntityLoad(LoadedEntity e, bool player=false)
    {
        LoadedEntity = e;
        if(!player)
            EntityAI.OnEntityLoad();
    }

    public void UnloadEntity(bool player = false)
    {
        LoadedEntity = null;
        if(!player)
            EntityAI.OnEntityUnload();
    }



    protected abstract void KillInternal();
    public void Kill()
    {
        KillInternal();
        IsAlive = false;
        EntityManager.Instance.UnloadEntity(GetLoadedEntity(), killEntity:true);
        if (!Inventory.IsEmpty)
        {
            LootSack loot = new LootSack(Vec2i.FromVector3(GetLoadedEntity().transform.position).AsVector3());
            loot.GetInventory().AddAll(Inventory);
            GameManager.WorldManager?.AddNewObject(loot);
        }
        
        EventManager.Instance.InvokeNewEvent(new EntityDeath(this));
        Debug.Log("Entity  " + this + " is killed");
  
    }
    #region setters and gets

    

    public void SetLastChunk(Vec2i chunk)
    {
        Debug.Log("Setting last chunk to " + chunk);
        LastChunkPosition = chunk;
    }

    public void SetSubworld(int id)
    {
        CurrentSubworldID = id;
    }
    public void SetSubworld(Subworld subworld)
    {
        if (subworld == null)
            CurrentSubworldID = -1;
        else
            CurrentSubworldID = subworld.SubworldID;
    }

    public Subworld GetSubworld()
    {
        if (CurrentSubworldID == -1)
            return null;
        return World.Instance.GetSubworld(CurrentSubworldID);
    }

    public void SetEntityFaction(EntityFaction entFact)
    {
        EntityFaction = entFact;
    }
    public void SetName(string name)
    {
        Name = name;
    }
    public void SetEntityID(int id)
    {
        ID = id;
    }


    /// <summary>
    /// Directly sets the position of the entity, this should only be called from
    /// <see cref="EntityManager.MoveEntity(Entity, int, Vec2i)"/>
    /// </summary>
    /// <param name="position"></param>
    public void SetPosition(Vector3 position)
    {
        Position_ = position;
        if(LoadedEntity != null)
        {
            LoadedEntity.transform.position = position;
        }
    }

    /// <summary>
    /// Used to move the entity. We call <see cref="EntityManager.MoveEntity(Entity, int, Vec2i)"/>.
    /// If <paramref name="newWorldID"/> is 0, then we stay in the same world.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="newWorldID"></param>
    public void MoveEntity(Vec2i position, int newWorldID=0)
    {
        EntityManager.Instance.MoveEntity(this, newWorldID == 0 ? CurrentSubworldID : newWorldID, Vec2i.FromVector2(position));
    }
    /// <summary>
    /// Used to move the entity. We call <see cref="EntityManager.MoveEntity(Entity, int, Vec2i)"/>.
    /// If <paramref name="newWorldID"/> is 0, then we stay in the same world.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="newWorldID"></param>
    public void MoveEntity(Vector3 position, int newWorldID=0)
    {
        EntityManager.Instance.MoveEntity(this, newWorldID == 0 ? CurrentSubworldID : newWorldID, Vec2i.FromVector3(position));        
    }
    /// <summary>
    /// Used to move the entity. We call <see cref="EntityManager.MoveEntity(Entity, int, Vec2i)"/>.
    /// If <paramref name="newWorldID"/> is 0, then we stay in the same world.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="newWorldID"></param>
    public void MoveEntity(Vector2 position, int newWorldID=0)
    {
        EntityManager.Instance.MoveEntity(this, newWorldID == 0 ? CurrentSubworldID : newWorldID, Vec2i.FromVector2(position));

    }

    public void SetLookAngle(float angle)
    {
        this.LookAngle = angle;
    }

    public void LookAt(Vector2 position)
    {
        float dot = Vector2.Dot(Vector2.up, position - Position2);
        LookAngle = Mathf.Acos(dot) * Mathf.Rad2Deg;
        if (LoadedEntity != null)
            LoadedEntity.LookTowardsPoint(new Vector3(position.x, 1, position.y));
        
    }
    public void SetFixed(bool isFixed){
        IsFixed = isFixed;
    }

    #endregion
    public GameObject GetEntityGameObject()
    {

        return ResourceManager.GetEntityGameObject(EntityGameObjectSource);
    }

    public override string ToString()
    {
        return "Entity " + Name;
    }

    public string[] EntityDebugData()
    {
        List<string> debugOut = new List<string>();
        debugOut.Add("Position: " + Position);
 
        if(this is NPC)
        {
            NPC npc = (this as NPC);

            if (npc.NPCData.HasJob)
            {
                debugOut.Add("JobTitle: " + npc.NPCData.NPCJob.Title);
            }

            NPCKingdomData kData = npc.NPCKingdomData;
            if(kData != null)
            {
                debugOut.Add("Kingdom: " + kData.GetKingdom().ToString());
                debugOut.Add("Settlement: " + kData.GetSettlement().ToString());
                debugOut.Add("Rank: " + kData.Rank.ToString());

            }
        }
        
        return debugOut.ToArray();
    }

}
/// <summary>
/// Data structure containing variables associated with Entity Movement
/// </summary>
[System.Serializable]
public struct EntityMovementData
{

    public float WalkSpeed;
    public float RunSpeed;
    public float JumpVelocity;


    public EntityMovementData(float walkSpeed, float runSpeed, float jumpVel)
    {
        Debug.Log("runspeed" + runSpeed);
        WalkSpeed = walkSpeed;
        RunSpeed = runSpeed;
        JumpVelocity = jumpVel;
    }


}