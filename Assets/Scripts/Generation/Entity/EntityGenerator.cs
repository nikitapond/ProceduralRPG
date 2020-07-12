using UnityEngine;
using UnityEditor;
using System.Xml.Linq;
using System.Collections.Generic;
public class EntityGenerator
{
    public readonly World World;
    public readonly EntityManager EntityManager;
    public readonly GameGenerator2 GameGen;
    public EntityGenerator(GameGenerator2 gameGen, EntityManager entityManager)
    {
        GameGen = gameGen;
        World = gameGen!=null?gameGen.World:World.Instance;
        EntityManager = entityManager;
    }

    public void GenerateAllKingdomEntities()
    {
        foreach(KeyValuePair<int, Kingdom> kpv in World.WorldKingdoms)
        {
            KingdomNPCGenerator kingEntGen = new KingdomNPCGenerator(GameGen, kpv.Value, EntityManager);
            kingEntGen.GenerateKingdomNPC();

        }

       
    }

}