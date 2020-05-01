using UnityEngine;
using UnityEditor;

public class Player : HumanoidEntity
{
    private NPC CurrentDialog;


    public Player(): base(null, null, new EntityMovementData(5,25,5), name:"Player")
    {
        Debug.Log("RUN!!!: " + this.MovementData.RunSpeed);
    }

    public override string EntityGameObjectSource => "human";

    public override void Update()
    {
        
        Debug.BeginDeepProfile("player_update_main");
        Vec2i cPos = World.GetChunkPosition(Position);
        if (cPos != LastChunkPosition)
        {

            GameManager.EntityManager.UpdateEntityChunk(this, LastChunkPosition, cPos);
            LastChunkPosition = cPos;
        }
        Debug.EndDeepProfile("player_update_main");

        SpellCastData data = new SpellCastData();
        data.Source = this;
        data.Target = GameManager.PlayerManager.GetWorldMousePosition();
        CombatManager.SpellManager.Update(data);

    }

    public NPC CurrentDialogNPC()
    {
        return CurrentDialog;
    }
    public bool InConversation()
    {
        return CurrentDialog != null;
    }


    protected override void KillInternal()
    {
        
        Debug.Log("Player deaaad");
    }

}