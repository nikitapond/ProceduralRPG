using UnityEngine;
using UnityEditor;

[System.Serializable]
public class Tree : WorldObjectData
{
    public Tree(Vec2i worldPosition, WorldObjectMetaData meta = null, Vec2i size = null) : base(worldPosition, meta, size)
    {


    }
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new Tree(pos);

    }

    public override string Name => "tree";

    public override WorldObjects ObjID => WorldObjects.TREE;

    /// <summary>
    /// We override the create world object function for a tree. This is done as we need to load in multiple
    /// parts of the tree
    /// </summary>
    /// <param name="transform"></param>
    /// <returns></returns>
    public override WorldObject CreateWorldObject(Transform transform = null)
    {

        


        //Create random with seed unique to tree position - this ensures tree should be same every time.
        GenerationRandom genRan = new GenerationRandom(WorldPosition.x * World.WorldSize * World.ChunkSize + WorldPosition.z);
        int canopy = genRan.RandomInt(3, 7);
        float angleOffset = genRan.Random(0, Mathf.PI);
        
        float range = Mathf.Sqrt(canopy*0.5f)*0.8f;
        WorldObject treeBase = WorldObject.CreateWorldObject(this, transform);
        Vec2i zero = new Vec2i(0, 0);
        Vector3[] canopyPositions = new Vector3[canopy];

        for(int i=0; i<canopy; i++)
        {
            //Define the positions of each canopy as roughly circular about the middle of the tree.
            float angle = (Mathf.PI * 2 / (canopy)) * i + angleOffset;
            Vector2 delta = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))*range + genRan.RandomVector2(-0.1f, 0.1f);
            float height = 1.8f + genRan.Random(0, 1f);
            //Store the total position for use later, then generate the canopy itself.
            canopyPositions[i] = new Vector3(delta.x, height, delta.y);
            TreeCanopy tr = new TreeCanopy(zero, canopyPositions[i]);
            tr.SetRandom(genRan);
            tr.CreateWorldObject(treeBase.transform);
        }
        //Generate the trunk

        float trunkHeight = 1.2f;
        float trunkScale = 1.2f;
        TreeBranch trunk = new TreeBranch(zero);
        trunk.SetCharacteristic(Vector3.zero, new Vector3(0, 0, 0), new Vector3(1, trunkScale, 1));
        WorldObject trunkObj = trunk.CreateWorldObject(treeBase.transform);
        //MeshCollider mc  =trunkObj.gameObject.AddComponent<MeshCollider>();
        //mc.sharedMesh = trunkObj.gameObject.GetComponent<MeshFilter>().mesh;
        Vector3 trunkTop = Vector3.up* trunkHeight* trunkScale; //Scale to correct height

        foreach(Vector3 v in canopyPositions)
        {
            TreeBranch canopyBranch = new TreeBranch(zero);
            //Calculate the Euler angles from the branch base to the canopy
            Vector3 delta_pos = v - trunkTop;
            Quaternion.FromToRotation(Vector3.up, delta_pos);
            //Vector3 rot = Quaternion.FromToRotation(trunkTop, v*5f).eulerAngles;
            Vector3 rot = Quaternion.FromToRotation(Vector3.up, delta_pos).eulerAngles;
            float scale = Vector3.Distance(v, trunkTop)/ trunkHeight;
            canopyBranch.SetCharacteristic(trunkTop, rot, new Vector3(1/scale,scale,1/ scale));
            canopyBranch.CreateWorldObject(treeBase.transform);
        }

        return treeBase;


    }

    public override void OnObjectLoad(WorldObject obj)
    {
    }

    public override void OnObjectUnload(WorldObject obj)
    {
    }




}
[System.Serializable]
public class TreeCanopy : WorldObjectData, IProcedualGeneratedObject
{
    public TreeCanopy(Vec2i worldPosition, Vector3 delta, WorldObjectMetaData meta = null, Vec2i size = null) : base(worldPosition, delta, meta, size)
    {
    }
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new TreeCanopy(pos, this.ObjectDeltaPosition);

    }

    public override string Name => "Tree Canopy";

    public override WorldObjects ObjID => WorldObjects.TREE_CANOPY;


    private GenerationRandom genRan;

    /// <summary>
    /// Called when the tree canopy object is first created. Here we add the random scaling
    /// </summary>
    /// <param name="obj"></param>
    public override void OnObjectLoad(WorldObject obj)
    {
        
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        float distort_amp = 2;
        float sin_freq = Mathf.PI*2;
        float total_scale = 1;
        Vector3[] verticies = mesh.vertices;

        Vector3 seed_delta = new Vector3(WorldPosition.x + ObjectDeltaPosition.x*17f, ObjectDeltaPosition.y*2f, WorldPosition.z + ObjectDeltaPosition.z*27f);
        Vector3 scale = genRan.RandomVector3(0.2f, 0.5f);
        scale.y *= 0.8f; //Bias to flatter canopies
        for (int i=0; i<mesh.vertices.Length; i++)
        {
            float perlin = genRan.PerlinNoise3D(verticies[i] + seed_delta, 512f);
            float dist_f = 1 + distort_amp * perlin;
            verticies[i].Scale(scale*dist_f);
        }
        //obj.GetComponent<SphereCollider>().radius = Mathf.Max(scale.x, scale.y, scale.z);

        mesh.vertices = verticies;
        mesh.RecalculateNormals();
       

    }

    public void SetRandom(GenerationRandom ran)
    {
        genRan = ran;
    }
}
[System.Serializable]
public class TreeBranch : WorldObjectData
{

    public TreeBranch(Vec2i worldPosition, WorldObjectMetaData meta = null, Vec2i size = null) : base(worldPosition, meta, size)
    {
    }
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new TreeBranch(pos, this.MetaData, this.Size);

    }

    public override string Name => "Tree branch";

    public override WorldObjects ObjID => WorldObjects.TREE_BRANCH;

    [System.NonSerialized]
    private Vector3 rotation;
    [System.NonSerialized]
    private Vector3 internalDelta;
    [System.NonSerialized]
    private Vector3 scale;
    public void SetCharacteristic(Vector3 delta, Vector3 rotation, Vector3 scale)
    {
        this.internalDelta = delta;
        this.rotation = rotation;
        this.scale = scale;
    }

    public override void OnObjectLoad(WorldObject obj)
    {
        obj.transform.localScale = scale;
        obj.transform.rotation = Quaternion.Euler(rotation);
        obj.transform.position += internalDelta;


    }
}
