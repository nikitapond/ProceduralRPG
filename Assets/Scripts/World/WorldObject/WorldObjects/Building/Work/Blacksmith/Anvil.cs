using UnityEngine;
using UnityEditor;

[System.Serializable]
public class Anvil : WorldObjectData, IWorkEquiptmentObject, IOnEntityInteract
{
    public override WorldObjects ID => WorldObjects.ANVIL;

    public override string Name => "Anvil";

    public override SerializableVector3 Size => Vector3.one;
    public override bool AutoHeight => true;
    public override bool IsCollision => true;


    private Entity CurrentUser_;
    public Entity CurrentUser { get => CurrentUser_; set => CurrentUser_=value; }

    public Vector3 DeltaPosition => new Vector3(1,0,0.5f);

    public void OnEntityInteract(Entity entity)
    {
        Debug.Log("hello!");
    }
}