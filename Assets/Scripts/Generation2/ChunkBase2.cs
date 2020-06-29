using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class ChunkBase2 
{

    public Vec2i Pos { get; private set; }
    public float Height { get; private set; }

    public ChunkBiome Biome { get; private set; }

    public ChunkFeature ChunkFeature { get; private set; }

    private Dictionary<ChunkResource, float> ResourceAmounts;

    private List<EntityGroup> EntityGroups;

    public int KingdomID = -1;

    public bool HasSettlement;
    public ChunkBase2(Vec2i position, float height, ChunkBiome biome)
    {
        Pos = position;
        Height = height;
        Biome = biome;
        ResourceAmounts = new Dictionary<ChunkResource, float>();
    }

    public bool HasEntityGroups()
    {
        return !(EntityGroups == null);
    }
    public List<EntityGroup> GetEntityGroups()
    {
        return EntityGroups;
    }
    public void AddEntityGroup(EntityGroup group)
    {
        if (EntityGroups == null)
            EntityGroups = new List<EntityGroup>();
        EntityGroups.Add(group);
    }
    public void RemoveEntityGroup(EntityGroup group)
    {
        EntityGroups?.Remove(group);
    }
    public void ClearEntityGroups()
    {
        EntityGroups?.Clear();
    }

    public void SetHeight(float height)
    {
        Height = height;
    }

    public bool ProducesResource(ChunkResource res)
    {
        return ResourceAmounts.ContainsKey(res) && ResourceAmounts[res] != 0;
    }
    public void SetResourceAmount(ChunkResource res, float amount)
    {
        if (ResourceAmounts.ContainsKey(res))
            ResourceAmounts[res] = amount;
        else
            ResourceAmounts.Add(res, amount);
    }
    public float GetResourceAmount(ChunkResource res)
    {
        if (ResourceAmounts.TryGetValue(res, out float amount))
            return amount;
        return 0;
    }

    public void SetChunkFeature(ChunkFeature feat)
    {
        ChunkFeature = feat;
    }

    public Color GetMapColor()
    {
        Color biomeCol = Biome.GetColor();
        if(ChunkFeature != null)
        {
            if(ChunkFeature is ChunkRiverNode)
            {
                ChunkRiverNode rn = ChunkFeature as ChunkRiverNode;
                if (rn.HasBridge)
                    return Color.grey;
                return new Color(0, 0, 0.4f);
            }else if(ChunkFeature is ChunkRoad)
            {
                ChunkRoad r = ChunkFeature as ChunkRoad;
                if (r.Type == ChunkRoad.RoadType.Dirt)
                    return new Color(165f / 255f, 42f / 255f, 42f / 255f);
                //Debug.Log("road at " + Pos);
                return Color.grey;
            }else if(ChunkFeature is ChunkLake)
            {
                return Color.blue;
            }
        }
        return biomeCol;
    }

}

public enum ChunkResource
{
    wood, ironOre, silverOre, goldOre, wheatFarm, vegetableFarm, silkFarm, cattleFarm, sheepFarm, 
}
public static class ChunkResourceHelper
{
    public static EconomicItem[] GetEconomicItem(this ChunkResource cr)
    {

        switch (cr)
        {
            case ChunkResource.ironOre:
                return new EconomicItem[] { Economy.IronOre };
            case ChunkResource.silverOre:
                return new EconomicItem[] { Economy.SilverOre };
            case ChunkResource.goldOre:
                return new EconomicItem[] { Economy.GoldOre };
            case ChunkResource.wood:
                return new EconomicItem[] { Economy.WoodLog };
            case ChunkResource.cattleFarm:
                return new EconomicItem[] { Economy.CowCarcus, Economy.CowHide };
            case ChunkResource.sheepFarm:
                return new EconomicItem[] { Economy.SheepCarcus, Economy.Wool };
            case ChunkResource.wheatFarm:
                return new EconomicItem[] { Economy.Wheat };
            case ChunkResource.vegetableFarm:
                return new EconomicItem[] { Economy.Vegetables };
            case ChunkResource.silkFarm:
                return new EconomicItem[] { Economy.Silk };

        }
        return null;


    }
}