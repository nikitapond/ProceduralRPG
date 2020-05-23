using UnityEngine;
using UnityEditor;
[System.Serializable]
public class NPC : HumanoidEntity
{
    public NPCDialog Dialog { get; private set; }
    public BasicNPCData NPCData { get; private set; }
    public NPCKingdomData NPCKingdomData { get; private set; }
    public EntityRelationshipManager EntityRelationshipManager { get; private set; }

    public override string EntityGameObjectSource => "human";

    public NPC(string name = "un-named_entity", bool isFixed = true) : base(new NonAggresiveNPCCombatAI(), new BasicNPCTaskAI(),new EntityMovementData(4,7,2), name:name, isFixed:isFixed)
    {
        NPCData = new BasicNPCData();
        EntityRelationshipManager = new EntityRelationshipManager(this);
    }
   
    public void SetPersonality(EntityPersonality personality)
    {
        if(EntityRelationshipManager == null)
        {
            EntityRelationshipManager = new EntityRelationshipManager(this, personality);
        }
        else
        {
            EntityRelationshipManager.SetPersonality(personality);
        }
    }


    public void SetKingdomData(NPCKingdomData npckdat)
    {
        NPCKingdomData = npckdat;
    }
    public void SetDialog(NPCDialog di)
    {
        Dialog = di;
    }
    public bool HasDialog()
    {
        return Dialog != null;
    }

    protected override void KillInternal()
    {
        Debug.Log(ToString() + " was killed!");
    }

}