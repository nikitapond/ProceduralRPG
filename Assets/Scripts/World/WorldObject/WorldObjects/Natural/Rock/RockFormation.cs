using UnityEngine;
using UnityEditor;

[System.Serializable]
public class RockFormation : WorldObjectData
{

    public override WorldObjects ObjID => WorldObjects.ROCK;
    public override string Name => "Rock Formation";

    private IMultiTileObjectChild[,] Children;
    private float MaxSize;
    public RockFormation(Vec2i worldPosition, float maxSize=1) : base(worldPosition)
    {
        MaxSize = maxSize;
    }
    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        return new RockFormation(pos,MaxSize);

    }
    public override WorldObject CreateWorldObject(Transform transform = null)
    {
        GenerationRandom genRan = new GenerationRandom(WorldPosition.GetHashCode() * 13);
        int count = genRan.RandomInt(2, 5);

        WorldObject r1 = null;

        for(int i=0; i<count; i++)
        {
            Vector3 pos = genRan.RandomVector3(-MaxSize, MaxSize, (i) * 0.1f, (i) * 0.2f + 0.1f, -MaxSize, MaxSize)/100f;
            float rSize = genRan.Random(0.5f, 1.5f);
            Rock r = new Rock(WorldPosition, pos, rSize);

            if (i == 0)
            {
                r1 = r.CreateWorldObject(transform);
            }
            else
            {
                WorldObject rn = r.CreateWorldObject(r1.transform);
                rn.transform.localPosition = pos;
            }

        }
        return r1;
    }


  
}