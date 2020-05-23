using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
[System.Serializable]
public abstract class Entity
{


    #region variables
    [SerializeField]
    private float[] Position_;
    public Vector3 Position { get { return new Vector3(Position_[0], Position_[1], Position_[2]); } }
    public Vector2 Position2 { get { return new Vector2(Position_[0], Position_[2]); } }
    public Vec2i TilePos { get { return new Vec2i((int)Position_[0], (int)Position_[2]); } }
    public Vec2i LastChunkPosition { get; protected set; }


    public float LookAngle { get; private set; }
    public float fov = 30; //Angle either side of look direction entity can see
    public string Name { get; private set; }
    public int ID { get; private set; }
    public bool IsFixed { get; private set; }
    public abstract string EntityGameObjectSource { get; }

    public EntityCombatManager CombatManager { get; private set; }

    public EntityAI EntityAI;

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
    #endregion
    public Entity(EntityCombatAI combatAI, EntityTaskAI taskAI, EntityMovementData movementData, string name = "un-named_entity", bool isFixed = false)
    {
        Name = name;
        IsFixed = isFixed;
        SetPosition(Vector3.zero);

        EntityAI = new EntityAI(this, combatAI, taskAI);


        Inventory = new Inventory();
        CombatManager = new EntityCombatManager(this);
        SkillTree = new SkillTree();
        MovementData = movementData;
    }



    /// <summary>
    /// Main update loop for entity, 
    /// </summary>
    public virtual void Update()
    {
        Vec2i cPos = World.GetChunkPosition(Position);
        if(cPos != LastChunkPosition)
        {
            EntityManager.Instance.UpdateEntityChunk(this, LastChunkPosition, cPos);
            LastChunkPosition = cPos;
        }
        EntityAI.Update();
    }

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
        EntityManager.Instance.UnloadEntity(GetLoadedEntity());
        if (!Inventory.IsEmpty)
        {
            LootSack loot = new LootSack(Vec2i.FromVector3(GetLoadedEntity().transform.position).AsVector3());
            loot.GetInventory().AddAll(Inventory);
            GameManager.WorldManager?.AddNewObject(loot);
        }
        EventManager.Instance.InvokeNewEvent(new EntityDeath(this));
  
    }
    #region setters

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

    public void SetPosition(Vec2i position)
    {
        Position_ = new float[] { position.x, 0.5f, position.z };
        if (LoadedEntity != null)
        {
            LoadedEntity.transform.position = Position;
        }

    }
    public void SetPosition(Vector3 position)
    {
        Position_ = new float[] { position.x, position.y, position.z };
    }
    public void SetPosition(Vector2 position)
    {
        Position_ = new float[] { position.x, 0, position.y };
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
/// Data structure containing variables associated with EntityMovement
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