using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Holds onto all the resources that are needed in the game
/// </summary>
public class ResourceManager
{

    private static Dictionary<string, Shader> Shaders;
    private static Dictionary<string, Material> Materials;
    private static Dictionary<string, GameObject> EntityGameObjects;
    private static Dictionary<int, GameObject> AllWorldObjects;
    private static Dictionary<int, Texture2D> ItemImages;
    private static Dictionary<ItemID, GameObject> EquiptableItemObjects;
    private static Dictionary<string, Sprite[]> SpriteSheets;
    private static Dictionary<string, GameObject> ProjectileObjects;
    private static Dictionary<string, GameObject> BeamObjects;
    private static Dictionary<string, GameObject> MiscPrefabs;

    private static Dictionary<Voxel, Material> VoxelMaterials;
    public static void LoadAllResources()
    {
        LoadShaders();
        CreateMaterials();
        LoadEntityGameObjects();
        LoadWorldObjects();
        LoadItemImages();
        LoadEquiptableItemObjects();
        LoadSpriteSheets();
        LoadProjectileObjects();
        LoadBeamObjects();
        LoadMiscPrefabs();
        LoadVoxelMaterials();
    }

    private static void LoadVoxelMaterials()
    {
        VoxelMaterials = new Dictionary<Voxel, Material>();
        VoxelMaterials.Add(Voxel.stone, Resources.Load<Material>("Shaders/Voxel/StoneWallVoxelMat"));
        VoxelMaterials.Add(Voxel.wood, Resources.Load<Material>("Shaders/Voxel/WoodVoxelMat"));
        VoxelMaterials.Add(Voxel.glass, Resources.Load<Material>("Shaders/Voxel/GlassVoxelMat"));
        VoxelMaterials.Add(Voxel.thatch, Resources.Load<Material>("Shaders/Voxel/ThatchVoxelMat"));


    }
    private static void LoadShaders()
    {
        Shaders = new Dictionary<string, Shader>();
        string root = "Shaders/";
        Shaders.Add("ChunkShader", Resources.Load<Shader>(root + "ChunkShader"));
    }

    private static void CreateMaterials()
    {
        Materials = new Dictionary<string, Material>();
        //Materials.Add("Chunk", new Material(Shaders["ChunkShader"]));
        Materials.Add("Chunk", Resources.Load<Material>("Shaders/Chunk/ChunkMaterial"));
        Materials.Add("brick", Resources.Load<Material>("Shaders/WorldObject/Simple/Brick/BrickMaterial"));

    }

    private static void LoadEntityGameObjects()
    {
        EntityGameObjects = new Dictionary<string, GameObject>();
        string root = "Entity/";
        EntityGameObjects.Add("healthbar", Resources.Load<GameObject>(root + "EntityHealthBar"));
        EntityGameObjects.Add("speechbubble", Resources.Load<GameObject>(root + "EntitySpeechBubble"));
        EntityGameObjects.Add("default", Resources.Load<GameObject>(root + "default"));

        EntityGameObjects.Add("bear", Resources.Load<GameObject>(root + "bear"));
        EntityGameObjects.Add("spider", Resources.Load<GameObject>(root + "Creature/Spider/Spider_pref"));
        EntityGameObjects.Add("human", Resources.Load<GameObject>(root + "Human/VoxelHuman"));


    }

    private static void LoadWorldObjects()
    {
        AllWorldObjects = new Dictionary<int, GameObject>();
        string root = "WorldObjects/";
        AllWorldObjects.Add((int)WorldObjects.EMPTY_OBJECT_BASE, Resources.Load<GameObject>(root + "Null"));
        GameObject placeholder = Resources.Load<GameObject>(root + "PlaceHolder");
        AllWorldObjects.Add((int)WorldObjects.WALL, Resources.Load<GameObject>(root + "BrickWall"));
        AllWorldObjects.Add((int)WorldObjects.LOOT_SACK, Resources.Load<GameObject>(root + "InventoryObjects/LootSack"));
        AllWorldObjects.Add((int)WorldObjects.TREE, Resources.Load<GameObject>(root + "Natural/Tree/TreeBase"));
        AllWorldObjects.Add((int)WorldObjects.TREE_CANOPY, Resources.Load<GameObject>(root + "Natural/Tree/TreeCanopy"));
        AllWorldObjects.Add((int)WorldObjects.TREE_BRANCH, Resources.Load<GameObject>(root + "Natural/Tree/TreeBranch"));
        AllWorldObjects.Add((int)WorldObjects.WATER, Resources.Load<GameObject>(root + "Natural/Terrain/Water"));
        AllWorldObjects.Add((int)WorldObjects.BRIDGE, Resources.Load<GameObject>(root + "Structure/Bridge/Bridge"));
        AllWorldObjects.Add((int)WorldObjects.BRIDGE_BASE, Resources.Load<GameObject>(root + "Structure/Bridge/Bridge"));
        AllWorldObjects.Add((int)WorldObjects.BRIDGE_RAMP, Resources.Load<GameObject>(root + "Structure/Bridge/BridgeRamp"));
        AllWorldObjects.Add((int)WorldObjects.GRASS, Resources.Load<GameObject>(root + "Natural/Terrain/Grass/Grass"));
        AllWorldObjects.Add((int)WorldObjects.ROCK, Resources.Load<GameObject>(root + "Natural/Rock/Rock"));
        AllWorldObjects.Add((int)WorldObjects.WOOD_SPIKE, Resources.Load<GameObject>(root + "Structure/Wood/WoodSpike"));
        AllWorldObjects.Add((int)WorldObjects.ANVIL, Resources.Load<GameObject>(root + "Building/Work/Blacksmith/Anvil"));
        AllWorldObjects.Add((int)WorldObjects.WEAPON_STAND, placeholder);
        AllWorldObjects.Add((int)WorldObjects.ARMOUR_STAND, placeholder);

        AllWorldObjects.Add((int)WorldObjects.GLASS_WINDOW, Resources.Load<GameObject>(root + "Structure/Glass/Window"));
        AllWorldObjects.Add((int)WorldObjects.ROOF, Resources.Load<GameObject>(root + "Building/Roof"));
        AllWorldObjects.Add((int)WorldObjects.DOOR, Resources.Load<GameObject>(root + "Structure/Door/Door"));
        AllWorldObjects.Add((int)WorldObjects.DUNGEON_ENTRANCE, Resources.Load<GameObject>(root + "Null"));
        AllWorldObjects.Add((int)WorldObjects.BED, Resources.Load<GameObject>(root + "Building/House/Bed"));
        AllWorldObjects.Add((int)WorldObjects.DOUBLE_BED, Resources.Load<GameObject>(root + "Building/House/DoubleBed"));

        AllWorldObjects.Add((int)WorldObjects.MARKET_STALL, Resources.Load<GameObject>(root + "Building/Work/Market/MarketStall"));
        AllWorldObjects.Add((int)WorldObjects.WALL_TORCH, Resources.Load<GameObject>(root + "Building/Misc/Torch"));



        AllWorldObjects.Add((int)WorldObjects.BANDIT_GAURD_TOWER, placeholder);
        AllWorldObjects.Add((int)WorldObjects.CHEST, placeholder);

        /*
        foreach(KeyValuePair<int, GameObject> kvp in AllWorldObjects)
        {
            kvp.Value.gameObject.layer = 8;
        }*/

        /*
        WorldObjects.Add(WorldObject.FORGE.ID, Resources.Load<GameObject>(root + "Work/Blacksmith/Forge"));
        WorldObjects.Add(WorldObject.BED.ID, Resources.Load<GameObject>(root + "Building/House/Bed"));
        WorldObjects.Add(WorldObject.MARKETSTALL.ID, Resources.Load<GameObject>(root + "Work/Market/MarketStall"));
        WorldObjects.Add(WorldObject.CHEST.ID, Resources.Load<GameObject>(root + "InventoryObjects/Chest"));*/
    }

    private static void LoadItemImages()
    {
        ItemImages = new Dictionary<int, Texture2D>();
        string root = "ItemImages/";
        ItemImages.Add(0, Resources.Load<Texture2D>(root + "item1"));
        ItemImages.Add(1, Resources.Load<Texture2D>(root + "item2"));

    }

    private static void LoadEquiptableItemObjects()
    {
        EquiptableItemObjects = new Dictionary<ItemID, GameObject>();
        string root = "Items/Equiptable/";
        EquiptableItemObjects.Add(ItemID.Trousers, Resources.Load<GameObject>(root + "Default/Clothes/Trousers"));
        EquiptableItemObjects.Add(ItemID.Shirt, Resources.Load<GameObject>(root + "Default/Clothes/Shirt"));

        EquiptableItemObjects.Add(ItemID.Torch, Resources.Load<GameObject>(root + "Other/Torch"));


        EquiptableItemObjects.Add(ItemID.SteelLongSword, Resources.Load<GameObject>(root + "Combat/Weapon/Sword/Longsword"));
    }

    private static void LoadSpriteSheets()
    {
        SpriteSheets = new Dictionary<string, Sprite[]>
        {
            { "weapons", Resources.LoadAll<Sprite>("Items/Equiptable/Combat/Weapon/WeaponSpriteSheet") },
            { "armour", Resources.LoadAll<Sprite>("Items/Equiptable/Combat/Armour/ArmourSpriteSheet") }
        };
    }
    private static void LoadProjectileObjects()
    {
        ProjectileObjects = new Dictionary<string, GameObject>();
        string root = "Projectiles/";
        ProjectileObjects.Add("fire_ball", Resources.Load<GameObject>(root + "fire_ball"));
        ProjectileObjects.Add("Arrow", Resources.Load<GameObject>(root + "Arrow"));

    }
    private static void LoadBeamObjects()
    {
        BeamObjects = new Dictionary<string, GameObject>();
        string root = "Beams/";
        BeamObjects.Add("fire_breath", Resources.Load<GameObject>(root + "fire_breath"));

    }

    private static void LoadMiscPrefabs()
    {
        MiscPrefabs = new Dictionary<string, GameObject>();
        MiscPrefabs.Add("march_sone_wall", Resources.Load<GameObject>("Misc/MarchStoneWall"));
    }
    public static Material GetVoxelMaterial(Voxel vox)
    {
    
        return VoxelMaterials[vox];
    }
    public static Material GetMaterial(string name)
    {
        return Materials[name];
    }
    public static GameObject GetEntityGameObject(string name)
    {

        return EntityGameObjects[name];
    }

    public static GameObject GetWorldObject(int id)
    {
        return AllWorldObjects[id];
    }
    public static Texture2D GetItemImage(int id)
    {
        return ItemImages[id];
    }
    public static Sprite GetItemSprite(string sheetTag, string itemTag)
    {
        try
        {
            Sprite[] sheet = SpriteSheets[sheetTag];
            Sprite sprite = sheet.Single(s => s.name == itemTag);
            return sprite;

        }
        catch (System.Exception e)
        {
            Debug.Error("Sprite coulf not be found: " + sheetTag + "/" + itemTag);
        }
        
        return null;
    }

    public static GameObject GetEquiptableItemObject(ItemID key)
    {
        return EquiptableItemObjects[key];
    }
    public static GameObject GetProjectileObject(string key)
    {
        return ProjectileObjects[key];
    }
    public static GameObject GetBeamObject(string key)
    {
        return BeamObjects[key];
    }
    public static GameObject GetMiscPrefab(string key)
    {
        return MiscPrefabs[key];
    }
}