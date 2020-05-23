using UnityEngine;
using UnityEditor;

public class NPCJobMerchant : NPCJob
{

    private Vector3 CurrentTargetPosition;
    private float CurrentPositionTime;
    private float CurrentPositionTimeOut;

    private NPCDialogNode ShopNode;

    private bool Init;

    public NPCJobMerchant(IWorkBuilding workLocation, string title=null) : base(title ?? "Merchant", workLocation, KingdomHierarchy.Citizen)
    {
        Init = false;
    }

    public override Color GetShirtColor => Color.white;

    public override void JobTick(NPC npc)
    {
        if (!Init)
        {
            Init = true;
            ShopNode = new NPCDialogNode("What do you have for sale?", "Take a look");
            ShopNode.SetOnSelectFunction(() => {
                //Debug.Log("test?");
                GUIManager.Instance.StartShop(npc, WorkLocation.WorkBuilding.Inventory);
            });
            if(npc.Dialog == null)
            {
                NPCDialog dialog = new NPCDialog(npc, "Hello, how can I help you today?");
                dialog.AddNode(ShopNode);
                npc.SetDialog(dialog);
                NPCDialogNode exitNode = new NPCDialogNode("I'll be on my way", "");
                exitNode.IsExitNode = true;
                dialog.AddNode(exitNode);

            }
            else
            {
                npc.Dialog.AddNode(ShopNode);
            }
                
            
        }


        float deltaTime = Time.time - CurrentPositionTime;
        if(deltaTime > CurrentPositionTimeOut)
        {
            ChooseNewPosition();
        }
    }

    public override void JobUpdate(NPC npc)
    {
        Vector3 pathTarget = npc.GetLoadedEntity().LEPathFinder.Target;

        Vector3 entPos = npc.Position;
        //if we are already at our target position, do nothing
        if (entPos.WithinDistance(pathTarget, 1f))
            return;
        //If the path finder is already correctly targeted, do nothing
        if (pathTarget.WithinDistance(CurrentTargetPosition, 1f))
            return;
        //If the finder isn't targeted, set it
        npc.GetLoadedEntity().LEPathFinder.SetTarget(CurrentTargetPosition);
    }


    private void ChooseNewPosition()
    {
        //Debug.Log(WorkLocation);
        //Debug.Log(WorkLocation.AsBuilding().GetSpawnableTiles());
        //Choose random point
        CurrentTargetPosition = GameManager.RNG.RandomFromList(WorkLocation.AsBuilding().GetSpawnableTiles()).AsVector3();
        CurrentPositionTime = Time.deltaTime;
        CurrentPositionTimeOut = GameManager.RNG.RandomInt(40, 60);
    }
    public override void OnTaskEnd(NPC npc)
    {
        npc.Dialog.RemoveNode(ShopNode);
        base.OnTaskEnd(npc);
    }
}