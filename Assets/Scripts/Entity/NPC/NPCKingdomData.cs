using UnityEngine;
using UnityEditor;
[System.Serializable]
public class NPCKingdomData
{
    public KingdomHierarchy Rank { get; private set; }
    public int KingdomID { get; private set; }
    public int SettlementID { get; private set; }
    public NPCKingdomData(KingdomHierarchy  rank, int kingdomID, int settlementID)
    {
        Rank = rank;
        KingdomID = kingdomID;
        SettlementID = settlementID;
    }

    public Kingdom GetKingdom()
    {
        return WorldManager.Instance.World.GetKingdom(KingdomID);
    }
    public Settlement GetSettlement()
    {
        return WorldManager.Instance.World.GetSettlement(SettlementID);
    }
}

