using UnityEngine;
using UnityEditor;
[System.Serializable]
public class Rock : WorldObjectData, IProcedualGeneratedObject
{
    public override WorldObjects ObjID => WorldObjects.ROCK;
    public override string Name => "Rock";
    private GenerationRandom genRan;
    private float RockSize;
    public Rock(Vec2i worldPosition, float rockSize = 1) : base(worldPosition, null, null)
    {
        RockSize = rockSize;
        SetRandom(new GenerationRandom(worldPosition.GetHashCode() + 13));
    }
    public Rock(Vec2i worldPosition, Vector3 delta, float rockSize = 1) : base(worldPosition, delta, null, null)
    {
        RockSize = rockSize;
        SetRandom(new GenerationRandom(worldPosition.GetHashCode() + 13));
    }
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new Rock(pos, RockSize);

    }

    public override void OnObjectLoad(WorldObject obj)
    {
        Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
        float distort_amp = 2;
        float sin_freq = Mathf.PI * 2;
        float total_scale = 1;
        Vector3[] verticies = mesh.vertices;

        Vector3 seed_delta = new Vector3(WorldPosition.x + ObjectDeltaPosition.x * 17f, ObjectDeltaPosition.y * 2f, WorldPosition.z + ObjectDeltaPosition.z * 27f);
        Vector3 scale = genRan.RandomVector3(0.2f, 0.5f) * RockSize;
        scale.y *= 0.8f; //Bias to flatter canopies
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            float perlin = genRan.PerlinNoise3D(verticies[i] + seed_delta, 512f);
            float dist_f = 1 + distort_amp * perlin;
            verticies[i].Scale(scale * dist_f);
        }
        mesh.vertices = verticies;
        mesh.RecalculateNormals();

    }

    public void SetRandom(GenerationRandom ran)
    {
        genRan = ran;
    }
}