using UnityEngine;
using UnityEditor;

public class TrainingDummy : WorldObjectData, IWorkEquiptmentObject
{
    public override WorldObjects ID => WorldObjects.TRAINING_DUMMY;

    public override string Name => "Training Dummy";

    public override SerializableVector3 Size => new Vector3(1,2,1);

    public override bool IsCollision => true;

    public override bool AutoHeight => true;

    private Entity CurrentUser_;
    public Entity CurrentUser { get => CurrentUser_; set => CurrentUser_ = value; }

    public Vector3 DeltaPosition => new Vector3(0,0,1);

    protected override void OnConstructor()
    {
        Scale = Size;
    }
}