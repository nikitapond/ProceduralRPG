using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    public CharacterController TEST;

    public LoadedEntity LoadedPlayer { get; private set; }
    public Player Player { get; private set; }

    public Camera PlayerCamera { get; private set; }
    public PlayerCamera PlayerCameraScript { get; private set; }
    private NPC CurrentlySelected;



    private SettlementPathNode NearestNode;
    private List<SettlementPathNode> path;
    private List<Vec2i> Path_;
    bool doPath = false;

    private EntityPathFinder epf;

    private void OnDrawGizmos()
    {
        if (doPath)
        {
            if(epf == null)
            {
                epf = new EntityPathFinder(GameManager.PathFinder);
            }
            Path_ = null;
            if (epf.IsRunning)
            {
                epf.ForceStop();
            }
            else
            {
                epf.FindPath(Player.TilePos, Player.TilePos + GameManager.RNG.RandomVec2i(-100, 100));
            }

            /*Debug.Log("calculating path");
            GenerationRandom genRan = new GenerationRandom(Time.frameCount);
            Path_ = GameManager.PathFinder.GeneratePath(Vec2i.FromVector3(Player.Position), Vec2i.FromVector3(Player.Position) + genRan.RandomVec2i(-100, 100));
            */
            return;
            Settlement t = GameManager.TestSettle;
            if (t == null)
                return;
            int curDist = -1;
            Vec2i pPos = Vec2i.FromVector2(Player.Position2);
            foreach (SettlementPathNode pn in t.tNodes)
            {
                if (pn == null)
                    continue;

                if (NearestNode == null)
                {
                    NearestNode = pn;
                    curDist = Vec2i.QuickDistance(pn.Position + t.BaseCoord, pPos);
                }
                else
                {
                    if (Vec2i.QuickDistance(pn.Position + t.BaseCoord, pPos) < curDist)
                    {
                        curDist = Vec2i.QuickDistance(pn.Position + t.BaseCoord, pPos);
                        NearestNode = pn;
                    }
                }
            }

            path = new List<SettlementPathNode>();
            if (SettlementPathFinder.SettlementPath(NearestNode, t.IMPORTANT, out path, debug: true))
            {
                Debug.Log("Path found!");

            }

            Debug.Log("Path len: " + path.Count);


        }
        if(Path_ == null && epf != null)
        {
            if (epf.IsComplete())
            {
                Path_ = epf.GetPath();
                
            }
        }
        
        
        if (Path_ != null && Path_.Count > 1)
        {
            Color old = Gizmos.color;
            Gizmos.color = Color.yellow;
            //Debug.Log(Vec2i.ToVector3(path[0].Position + t.BaseCoord));
            //Gizmos.DrawCube(Vec2i.ToVector3(path[0].Position + GameManager.TestSettle.BaseCoord), Vector3.one * 2);

            Gizmos.DrawCube(Vec2i.ToVector3(Path_[0]), Vector3.one * 5);
            Gizmos.DrawCube(Vec2i.ToVector3(Path_[Path_.Count - 1]), Vector3.one * 5);

            for (int i = 0; i < Path_.Count - 1; i++)
            {
                Gizmos.DrawLine(Vec2i.ToVector3(Path_[i]), Vec2i.ToVector3(Path_[i + 1]));
                Gizmos.DrawCube(Vec2i.ToVector3(Path_[i]), Vector3.one * 0.5f);
            }
            Gizmos.color = old;
        }
        return;
        if(NearestNode == null)
        {
            Settlement t = GameManager.TestSettle;
            if (t == null)
                return;
            int curDist = -1;
            Vec2i pPos = Vec2i.FromVector2(Player.Position2);
            foreach(SettlementPathNode pn in t.tNodes)
            {
                if (pn == null)
                    continue;

                if(NearestNode == null)
                {
                    NearestNode = pn;
                    curDist = Vec2i.QuickDistance(pn.Position + t.BaseCoord, pPos);
                }
                else
                {
                    if(Vec2i.QuickDistance(pn.Position + t.BaseCoord, pPos) < curDist)
                    {
                        curDist = Vec2i.QuickDistance(pn.Position + t.BaseCoord, pPos);
                        NearestNode = pn;
                    }
                }
            }

            path = new List<SettlementPathNode>();
            if (SettlementPathFinder.SettlementPath(NearestNode, t.IMPORTANT, out path, debug:true)) {
                Debug.Log("Path found!");
            
            }

            Debug.Log("Path len: " + path.Count);
            
           

        }
  
        
    }


    
    private Vector3 WorldLookPosition;
    private GameObject LookObject;

    private void Awake()
    {
        Instance = this;

        PlayerCamera = transform.Find("PlayerCamera").GetComponent<Camera>();
        PlayerCameraScript = transform.Find("PlayerCamera").GetComponent<PlayerCamera>();
        
        
    }

    public void SetPlayer(Player player)
    {
        GameObject entityObject = Instantiate(player.GetEntityGameObject());
        entityObject.name = "Player";
        entityObject.transform.parent = transform;

        LoadedEntity loadedEntity = entityObject.GetComponent<LoadedEntity>();
        player.OnEntityLoad(loadedEntity, true);
        loadedEntity.SetEntity(player);
        entityObject.transform.position = player.Position;
        loadedEntity.SetLookBasedOnMovement(false);
        Player = player;
        //Player.CombatManager.AddSpell(new SpellFireball(), 0);
        Player.CombatManager.SpellManager.AddSpell(new SpellStoneWall(), 0);


        Player.Inventory.AddItem(new SteelLongSword());
        Player.Inventory.AddItem(new SimpleDungeonKey(0));
        //Player.Inventory.AddItem(new SteelLegs());
        Player.EquiptmentManager.AddDefaultItem(new Trousers());
        Player.EquiptmentManager.AddDefaultItem(new Shirt());
       // Player.EquiptmentManager.AddDefaultItem(new Trousers());
        LoadedPlayer = loadedEntity;

        GetComponent<ProceduralGridMover>().target = Player.GetLoadedEntity().transform;


       




    }
    public void Tick(float time)
    {
        Player.CombatManager.Tick(time);
        //GameManager.PathFinder.SetPlayerPosition(Player.TilePos);
    }

    private Vec2i LastChunk;

    void Update()
    {
        LoadedPlayer.SetIdle(false);
        if (Console.Instance != null && Console.Instance.Active)
            return;


        if (GameManager.GUIManager!=null && GameManager.GUIManager.DialogGUI.InConversation)
            return;


        if (Input.GetKeyDown(KeyCode.P))
        {
            doPath = true;
        }
        else
        {
            doPath = false;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            GUIManager.Instance.SetVisible(!GUIManager.Instance.OpenGUIVisible);

        }

        if (GameManager.Paused)
            return;

       // Debug.BeginDeepProfile("PlayerManagerUpdate");

        Vector3 worldMousePos = GetWorldMousePosition();
        

        Vec2i currentChunk = World.GetChunkPosition(Player.TilePos);
        if(currentChunk != LastChunk)
        {
            LastChunk = currentChunk;
            Tick(Time.deltaTime);
        }
        Player.Update();

        MovementUpdate();
        PlayerSelectUpdate();



        DebugGUI.Instance.SetData("world_mouse_pos", worldMousePos.ToString());
        LoadedPlayer.LookTowardsPoint(worldMousePos);

        if (Input.GetKey(KeyCode.Alpha1))
        {
            SpellCastData data = new SpellCastData();
            data.Source = Player;
            data.Target = GetWorldMousePosition();
            Player.CombatManager.SpellManager.CastSpell(0, data);
            
        }

        if (Input.GetMouseButtonDown(0))
        {
            LeftMouseButton();            
        }else if (Input.GetMouseButtonDown(1))
        {
            RightMouseButton();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadedPlayer.Jump();
        }

        LoadedPlayer.SetRunning(Input.GetKey(KeyCode.LeftShift));

        Debug.EndDeepProfile("PlayerManagerUpdate");
    }
    
    /// <summary>
    /// Controlls the movement of the player
    /// </summary>
    private void MovementUpdate()
    {



        float hor = Input.GetAxis("Horizontal"); //A,D
        float vert = Input.GetAxis("Vertical"); //S,W 



        //If the camera is a first person type
        if (PlayerCameraScript.CameraController is FirstPersonCC)
        {
            float dz = hor * Mathf.Sin(-Player.LookAngle * Mathf.Deg2Rad) + vert * Mathf.Cos(-Player.LookAngle * Mathf.Deg2Rad);
            float dx = hor * Mathf.Cos(-Player.LookAngle * Mathf.Deg2Rad) - vert * Mathf.Sin(-Player.LookAngle * Mathf.Deg2Rad);

            LoadedPlayer.MoveInDirection(new Vector2(dx, dz));

        }
    }

    /// <summary>
    /// Finds the gameobject the player is currently looking at, and 
    /// deals with it
    /// </summary>
    void PlayerSelectUpdate()
    {
        LookObject = PlayerCameraScript.CameraController.GetViewObject();
        if(LookObject == null)
        {
            DebugGUI.Instance.ClearData("player_view");
            GUIManager.Instance.IngameGUI.SetNPC(null);
        }
        else
        {

            DebugGUI.Instance.SetData("player_view", LookObject.ToString());
            if(LookObject.GetComponent<LoadedEntity>()!= null)
            {
                if(LookObject.GetComponent<LoadedEntity>().Entity is NPC)
                {
                    GUIManager.Instance.IngameGUI.SetNPC(LookObject.GetComponent<LoadedEntity>().Entity as NPC);
                }
                else
                {
                    GUIManager.Instance.IngameGUI.SetNPC(null);
                }
            }
            else
            {
                GUIManager.Instance.IngameGUI.SetNPC(null);

            }
        }

        WorldLookPosition = PlayerCameraScript.CameraController.GetWorldLookPosition();
        DebugGUI.Instance.SetData("look_wpos", WorldLookPosition);
    }


    public Vector3 GetWorldMousePosition()
    {
        Ray ray = PlayerCamera.ScreenPointToRay(Input.mousePosition);
        //We need to know the x and z components of the position when the z is 0.
        //So, the ray is a Point + float * direction
        Vector3 start = PlayerCamera.transform.position;
        float mult = start.y / ray.direction.y;
        float x = start.x  - ray.direction.x * mult;
        float z = start.z  - ray.direction.z * mult;
        //float x = start.x  + ray.direction.x * mult;
        //float z = start.z  + ray.direction.z * mult;
        return new Vector3(x, 0, z);
    }

 

    /// <summary>
    /// Called if the left mouse button pressed
    /// </summary>
    public void LeftMouseButton()
    {
        
        //Weapon in sheath but not in hand, so we put in hand
        if(Player.EquiptmentManager.HasWeapon && !Player.EquiptmentManager.WeaponReady)
        {
            Player.EquiptmentManager.UnsheathWeapon(LoadedEquiptmentPlacement.weaponSheath);
            return;
        }


        if (Player.CombatManager.CanAttack())
        {
            //LoadedPlayer.WeaponController.PlayWeaponAnimation();
            Player.CombatManager.UseEquiptWeapon();
        }

    }

    public void RightMouseButton()
    {

        if(LookObject != null)
        {

            LoadedEntity lEnt = LookObject.GetComponent<LoadedEntity>();
            if(lEnt != null)
            {
                Entity ent = lEnt.Entity;
                if(ent is NPC)
                {
                    NPC npc = ent as NPC;

                    if (npc.HasDialog())
                    {
                        StartDialog(npc);
                    }


                }
            }


        }
        return;

        Ray ray = PlayerCamera.ScreenPointToRay(Input.mousePosition);
        bool entSelect = false;
        Debug.Log("hmmm");
        RaycastHit[] raycasthit = Physics.RaycastAll(ray);
        foreach(RaycastHit hit in raycasthit)
        {
            Debug.Log(hit);
            LoadedEntity hitEnt = hit.transform.gameObject.GetComponent<LoadedEntity>();
            if (hitEnt != null)
            {
                entSelect = true;
                //GameManager.DebugGUI.SetSelectedEntity(hitEnt);
                Entity hitEntity = hitEnt.Entity;
                if (hitEntity is NPC)
                {
                    NPC npc = hitEntity as NPC;
                    //If we have selected a different entities
                    if (npc != CurrentlySelected)
                    {
                        Debug.Log(hitEntity.Name + " has been clicked by the player");
                        GameManager.EventManager.InvokeNewEvent(new PlayerTalkToNPC(npc));
                        CurrentlySelected = npc;

                        if (npc.HasDialog())
                        {
                            Debug.Log("TRUE DIALOG");
                            GameManager.GUIManager.StartDialog(npc);
                            if (!Player.InConversation())
                            {
                                if (Player.CurrentDialogNPC() != npc)
                                {
                                    NPCDialog dial = npc.Dialog;
                                    dial.StartDialog();
                                }
                            }
                        }
                    }




                }
            }
            else
            {
                CurrentlySelected = null;
            }
            Debug.Log(hit.collider.gameObject);
            if (hit.collider.gameObject.GetComponent<WorldObject>() != null)
            {
                WorldObject obj = hit.collider.gameObject.GetComponent<WorldObject>();
                Debug.Log(obj);
                Debug.Log(obj.Data);
                if (obj.Data is IOnEntityInteract)
                {
                    Debug.Log("here2");
                    (obj.Data as IOnEntityInteract).OnEntityInteract(Player);
                }
            }
        }

        if (!entSelect)
            GameManager.DebugGUI.SetSelectedEntity(null);
    }


    private void StartDialog(NPC npc)
    {
        npc.Dialog.StartDialog();
        GUIManager.Instance.StartDialog(npc);
        EventManager.Instance.InvokeNewEvent(new PlayerTalkToNPC(npc));
        EventManager.Instance.InvokeNewEvent(new GamePause(true));
    }

    public void EndDialog()
    {
        GameManager.EventManager.InvokeNewEvent(new GamePause(false));
    }


    
}
