using UnityEngine;
using UnityEditor;
[System.Serializable]
public abstract class WorldObjectData
{
    /// <summary>
    /// When the object is loaded this paramater will be set as
    /// the reference to the currently loaded object.
    /// </summary>
    [System.NonSerialized]
    public WorldObject LoadedObject;

    public GameObject ObjectPrefab { get { return ResourceManager.GetWorldObject((int)ID); } }


    /// <summary>
    /// Returns the Object ID of this object.
    /// </summary>
    public abstract WorldObjects ID { get; }

    /// <summary>
    /// The name of this object
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// The size of the object, measured from the minimum point to the maximum point
    /// </summary>
    public abstract SerializableVector3 Size { get; }

    /// <summary>
    /// Defines if this object blocks path in the path finder.
    /// </summary>
    public abstract bool IsCollision { get; }

    /// <summary>
    /// Defines if this objects height should be found by ray tracing on initiation
    /// </summary>
    public abstract bool AutoHeight { get;  }

    public SerializableVector3 Scale { get; protected set; }

    /// <summary>
    /// The position of this object, measured from the minimum point
    /// </summary>
    public SerializableVector3 Position { get; protected set; }
    /// <summary>
    /// The angle between this objects direction and Vector3.forward
    /// </summary>
    public float Rotation { get; protected set; }

    public WorldObjectData(Vector3 position, float rotation = 0)
    {
        Position = position;
        Rotation = rotation;
        Scale = Vector3.one;
        OnConstructor();
    }
    public WorldObjectData(float rotation = 0)
    {
        Position = Vector3.zero;
        Rotation = rotation;
        Scale = Vector3.one;
        OnConstructor();

    }

    public WorldObjectData SetPosition(Vector3 nPos)
    {
        this.Position = nPos;
        return this;
    }
    public WorldObjectData SetPosition(Vec2i nPos)
    {
        this.Position = nPos.AsVector3();
        return this;
    }

    public WorldObjectData SetRotation(float rot)
    {
        this.Rotation = rot;
        return this;
    }

    /// <summary>
    /// Called when this object is created.
    /// Used to initiate anything that needs to be initiated (for example, inventory)
    /// </summary>
    protected virtual void OnConstructor() { }

    /// <summary>
    /// Checks if the bounds of the two objects intersect
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool Intersects(WorldObjectData obj)
    {
        if (IsCollision == false)
            return false;
        else if (obj.IsCollision == false)
            return false;
        return Vector3.Distance(obj.Position, Position) <=
            Mathf.Max(obj.Size.x, obj.Size.y, obj.Size.z, Size.x, Size.y, Size.z);

        Vector3 aMin = Position;
        Vector3 bMin = obj.Position;
        Vector3 aMax = aMin + Size;
        Vector3 bMax = bMin + obj.Size;
        return aMin.x < bMax.x && aMin.x > bMin.x && aMin.y < bMax.y && aMin.y > bMin.y && aMin.z < bMax.z && aMin.z > bMin.z ||
            aMax.x < bMax.x && aMax.x > bMin.x && aMax.y < bMax.y && aMax.y > bMin.y && aMax.z < bMax.z && aMax.z > bMin.z;

    }

    public bool IntersectsPoint(Vec2i point)
    {
        return Vector3.Distance(point.AsVector3(), Position) <= Mathf.Max(Size.x, Size.y, Size.z);
    }

    public Recti CalculateIntegerBounds()
    {
        return new Recti((int)Position.x, (int)Position.z, (int)Size.x, (int)Size.z);
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