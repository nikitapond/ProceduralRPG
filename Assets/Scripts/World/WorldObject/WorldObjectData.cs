using UnityEngine;
using UnityEditor;
[System.Serializable]
public abstract class WorldObjectData
{

    [System.NonSerialized]
    public WorldObject LoadedObject;

    public float GroundHeight { get; private set; }
    public abstract WorldObjects ObjID { get; }
    public int ID { get { return (int)ObjID; } }
    public abstract string Name { get; }
   
    /// <summary>
    /// Used by pathfinder to check if the tile containing this game object is passable.
    /// </summary>
    public bool IsCollision { get; protected set; }

    public Vec2i WorldPosition { get; private set; }
    private float[] ObjectDeltaPosition_;
    public Vector3 ObjectDeltaPosition { get { return new Vector3(ObjectDeltaPosition_[0], ObjectDeltaPosition_[1], ObjectDeltaPosition_[2]); } }
    protected WorldObjectMetaData MetaData;
    public Vec2i Size { get; protected set; }

    public abstract WorldObjectData Copy(Vec2i pos=null);
    

    public WorldObjectData(Vec2i worldPosition, Vector3 delta, WorldObjectMetaData meta = null, Vec2i size = null)
    {
        WorldPosition = worldPosition;
        ObjectDeltaPosition_ = new float[]{ delta.x,delta.y,delta.z};
        MetaData = meta;
        Size = size;
        IsCollision = true;
        GroundHeight = -1;
    }
    public WorldObjectData(Vec2i worldPosition, WorldObjectMetaData meta=null, Vec2i size=null)
    {
        WorldPosition = worldPosition;
        MetaData = meta;
        ObjectDeltaPosition_ = new float[] { 0,0,0 };
        Size = size;
        IsCollision = true;
        GroundHeight = -1;

    }

    public void SetGroundHeight(float gh)
    {
        GroundHeight = gh;
    }

    public void SetPosition(Vec2i worldPos)
    {
        WorldPosition = worldPos;
    }

    public virtual WorldObject CreateWorldObject(Transform transform=null)
    {
        WorldObject obj = WorldObject.CreateWorldObject(this, transform);
        LoadedObject = obj;
       

        return obj;
    }

    public bool HasMetaData()
    {
        return MetaData != null;
    }
    public WorldObjectMetaData GetMetaData(bool create=true)
    {
        if (create && MetaData == null)
            MetaData = new WorldObjectMetaData();
        return MetaData;
    }

    /// <summary>
    /// Called when the object is instansiated. We pass the world object 
    /// so we can make changes if required
    /// </summary>
    /// <param name="obj"></param>
    public virtual void OnObjectLoad(WorldObject obj) { }
    /// <summary>
    /// Called when the object is deleted. We pass the world object 
    /// so we can make changes if required
    /// </summary>
    /// <param name="obj"></param>
    public virtual void OnObjectUnload(WorldObject obj) { }

}