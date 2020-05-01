using UnityEngine;
using UnityEditor;

[System.Serializable]
public class Water : WorldObjectData
{
    private float[] FlowDirection;
    private float[] UVOffset;
    public Water(Vec2i worldPosition) : base(worldPosition, null, null)
    {
    }

    public override WorldObjectData Copy(Vec2i pos)
    {
        if (pos == null)
            pos = WorldPosition;

        Water water = new Water(pos);
        water.FlowDirection = FlowDirection;
        water.UVOffset = UVOffset;
        return water;

    }

    /// <summary>
    /// Sets the UV offset for this water tile 
    /// </summary>
    /// <param name="off"></param>
    public void SetUVOffset(Vector2 off)
    {
        UVOffset = new float[] { off.x, off.y };
    }
    public void SetWaterFlowDirection(Vector2 flowDir)
    {
        FlowDirection = new float[] { flowDir.x, flowDir.y };
    }

    public override WorldObject CreateWorldObject(Transform transform = null)
    {
        WorldObject obj = base.CreateWorldObject(transform);
        obj.transform.position -= Vector3.up * 0.2f;
        if(UVOffset != null)
        {
            Mesh m = obj.GetComponentInChildren<MeshFilter>().mesh;
            Vector2[] uv = m.uv;

           // Vector2 dir = new Vector2(FlowDirection[0], FlowDirection[1]);
            Vector2 offset = new Vector2(UVOffset[0], UVOffset[1]);
            for (int i = 0; i < m.vertices.Length; i++)
            {
                uv[i] += offset;
            }
            m.uv = uv;
        }


        return obj;
        
    }


    public override string Name => "Water";

    public override WorldObjects ObjID => WorldObjects.WATER;
}