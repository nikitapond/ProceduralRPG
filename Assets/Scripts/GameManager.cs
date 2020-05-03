using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
public class GameManager : MonoBehaviour
{

    public static bool DEBUG = false;

    public GameObject TEST_PREF;

    public static bool IsPlaying { get; private set; }
    public bool[] toDraw = { false, false, false, false };

    public Texture2D[] toDrawTexts = { null, null, null, null };
    public static Settlement TestSettle;

    public static string GameToLoad = "none";

    /// <summary>
    /// Here we define various static objects. These link to instances that are frequently
    /// needed throughtout the game
    /// </summary>
    public static GameManager Game { get; private set; }

    public static GameGenerator GameGenerator { get; private set; }
    public static WorldManager WorldManager { get; private set; }
    public static EntityManager EntityManager { get; private set; }
    public static PlayerManager PlayerManager { get; private set; }
    public static QuestManager QuestManager { get; private set; }
    public static EventManager EventManager { get; private set; }
    public static DebugGUI DebugGUI { get; private set; }
    public static GUIManager2 GUIManager { get; private set; }
    public static PathFinder PathFinder { get; private set; }
    public static LoadSave LoadSave { get; private set; }
    public static bool Paused { get; private set; }

    public static Console Console { get; private set; }

    public static GenerationRandom RNG { get; private set; }

    public static ChunkRegionGenerator ChunkRegionGenerator { get; private set; }
    public static void SetPause(bool pause)
    {
        Paused = pause;
        EventManager.InvokeNewEvent(new GamePause(pause));
    }



    /// <summary>
    /// Initial awake function. 
    /// Used to set all static fields.
    /// Also loads all required resources via the <see cref="ResourceManager"/>
    /// </summary>
    void Awake()
    {

        IsPlaying = true;

        Debug.Log(Application.persistentDataPath);
        //ZeroFormatter.Formatters.Formatter.Register
        Game = this;
        DebugGUI = GetComponent<DebugGUI>();

        WorldManager = transform.Find("WorldManager").GetComponent<WorldManager>();
        EntityManager = transform.Find("EntityManager").GetComponent<EntityManager>();
        PlayerManager = transform.Find("PlayerManager").GetComponent<PlayerManager>();
        QuestManager = GetComponent<QuestManager>();
        GUIManager = GetComponentInChildren<GUIManager2>();
        EventManager = new EventManager();

        Console = GetComponentInChildren<Console>();
        ResourceManager.LoadAllResources();

        LoadSave = new LoadSave("test");
    }
    /// <summary>
    /// Starts the main part of the game.
    /// Checks if a game exists to load, and if so loads it.
    /// If not, we generate a game <see cref="GameManager.GenerateGame(int)"/>
    /// If we generate a game, at current we set the players location
    /// </summary>
    void Start()
    {
        Debug.BeginDeepProfile("game_start");
        if (GameToLoad == null || GameToLoad == "none")
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            //UnityEngine.Profiling.CustomSampler gen = UnityEngine.Profiling.CustomSampler.Create("GenSampler");
            int seed = 0;
            Debug.Log("No game to load, generating with seed " + seed);
            
            GenerateGame(seed);
            PathFinder = new PathFinder(WorldManager.World);
            PathFinder.SetPlayerPosition(PlayerManager.Player.TilePos);

            System.GC.Collect();
            s.Stop();
            Debug.Log("Generation took total time: " + s.ElapsedMilliseconds / 1000f);
            DebugGUI.SetData("genTime", s.ElapsedMilliseconds / 1000f);

        }
        else
        {
            UnityEngine.Profiling.CustomSampler load = UnityEngine.Profiling.CustomSampler.Create("LoadSampler");
            load.Begin();
            GameLoadSave gls = LoadSave.Load();
            World world = new World();
            world.LoadWorld(gls);
            EntityManager.Load(gls);
            WorldManager.SetWorld(world);
            Player player = new Player();
            TestSettle = WorldManager.World.GetSettlement(0);
            Vec2i set = TestSettle.Centre;
            //player.SetPosition(new Vector3(set.x * World.ChunkSize, 0, set.z * World.ChunkSize));
            player.SetPosition(new Vector3(World.WorldSize / 2 * World.ChunkSize, 0, World.WorldSize/2 * World.ChunkSize));
            PlayerManager.SetPlayer(player);

            load.End();
        }
        Debug.EndDeepProfile("game_start");



        RNG = new GenerationRandom(System.DateTime.Now.Millisecond);
    }

    private void OnApplicationQuit()
    {
        TestSettle = null;
        IsPlaying = false;
    }

    /// <summary>
    /// The main generation function for the whole game
    /// Creates a <see cref="GameGenerator"/> and then creates a world using <see cref="GameGenerator.GenerateWorld"/>
    /// Then generates all quests using <see cref="GameGenerator.GenerateQuests(World)"/>
    /// </summary>
    /// <param name="seed">Seed fed to the <see cref="GameGenerator"/> that defines the generation</param>
    private void GenerateGame(int seed)
    {
        //Initiate GameGenerator, then generate and set world
        //Debug.BeginDeepProfile("generate_world");

        GameGenerator = new GameGenerator(seed);
        GameGenerator.GenerateWorld(WorldManager);
        
        GameGenerator.GenerateEntities(WorldManager.World);
        GameGenerator.GenerateDungeons();
        GameGenerator.GenerateWorldMap();
        //QuestManager.SetQuests(GameGenerator.GenerateQuests(WorldManager.World));


        //Vec2i wpos = Vec2i.FromVector2(QuestManager.Unstarted[0].Initiator.GetNPC().Position2);
        //Vec2i wpos = WorldManager.World.GetChunkStructure(0).Position * World.ChunkSize + new Vec2i(2, 2);

        Vec2i wpos = WorldManager.World.GetSettlement(0).Centre * World.ChunkSize;
        //Vec2i wpos = new Vec2i(1, 1) * World.ChunkSize * World.WorldSize / 2;
       // Vec2i wEntr = WorldManager.World.GetSubworld(1).WorldEntrance;
        //TestSettle = QuestManager.Unstarted[0].Initiator.GetNPC().NPCKingdomData.GetSettlement();
        Debug.Log(TestSettle);


        Vec2i playerStartreg = World.GetRegionCoordFromChunkCoord(World.GetChunkPosition(wpos));


        ChunkRegionGenerator = GameGenerator.GenerateChunks(playerStartreg);
        GeneratePlayer(wpos);
    }

    private void GeneratePlayer(Vec2i start)
    {
        Player player = new Player();
        player.SetPosition(start);
        PlayerManager.SetPlayer(player);
        player.Inventory.AddItem(new SteelLongSword());
        player.Inventory.AddItem(new Torch());
    }
 

    private void LoadGame(string game)
    {

    }

    private void Update()
    {
       // Debug.Log("LoadedPathCount: " + PathFinder.COUNT);
    }

    bool hasBuilt = false;
    private void OnDrawGizmos()
    {
        return;
        if(TestSettle != null && !hasBuilt)
        {
            for(int x=0; x<TestSettle.PathNodes.GetLength(0); x++)
            {
                for (int z = 0; z < TestSettle.PathNodes.GetLength(0); z++)
                {


                    if(TestSettle.SettlementPathFinder.PathNodes[x,z] > 0)
                    {
                        Vector3 pos = Vec2i.ToVector3(new Vec2i(x,z)*World.ChunkSize + TestSettle.BaseCoord);
                        GameObject obj = Instantiate(TEST_PREF);
                        obj.transform.position = pos;
                        PathFindingNodeTest pnt = obj.GetComponent<PathFindingNodeTest>();
                        pnt.map = TestSettle.SettlementPathFinder.PathNodes;
                        pnt.x = x;
                        pnt.z = z;
                    }else if(TestSettle.SettlementPathFinder.PathNodes[x,z] == -1)
                    {
                        Vector3 pos = Vec2i.ToVector3(new Vec2i(x, z) * World.ChunkSize + TestSettle.BaseCoord);
                        GameObject obj = Instantiate(TEST_PREF);
                        obj.transform.position = pos;
                        obj.transform.localScale = Vector3.one * 5;
                        PathFindingNodeTest pnt = obj.GetComponent<PathFindingNodeTest>();
                        pnt.map = TestSettle.SettlementPathFinder.PathNodes;
                        pnt.x = x;
                        pnt.z = z;
                    }

                }
            }/*
            foreach (SettlementPathNode spn in TestSettle.tNodes)
            {
                if (spn == null)
                    continue;
                Vector3 pos = Vec2i.ToVector3(spn.Position + TestSettle.BaseCoord);
                GameObject obj = Instantiate(TEST_PREF);
                obj.transform.position = pos;
                obj.GetComponent<PathFindingNodeTest>().spn = spn;
            }*/
            hasBuilt = true;

        }
        return;
        if(TestSettle != null)
        {
            foreach(SettlementPathNode spn in TestSettle.tNodes)
            {
                if (spn == null)
                    continue;
                Vector3 pos = Vec2i.ToVector3(spn.Position + TestSettle.BaseCoord);
                Gizmos.DrawSphere(pos, 0.5f);

                foreach(SettlementPathNode con in spn.Connected)
                {
                    if(con != null)
                    {
                        //Debug.Log("not null");
                        Gizmos.DrawLine(pos, Vec2i.ToVector3(con.Position + TestSettle.BaseCoord));

                    }
                }
            }
        }
    }


    private void OnGUI()
    {
        if(GameGenerator.MAP!= null)
        {
            GUI.DrawTexture(new Rect(0, 0, World.WorldSize, World.WorldSize), GameGenerator.MAP);
        }
        for(int i=0; i<4; i++)
        {
            if(toDraw[i] && toDrawTexts[i] != null)
            {
                GUI.DrawTexture(new Rect(0, 0, toDrawTexts[i].width, toDrawTexts[i].height), toDrawTexts[i]);
            }
        }
    }

}
