using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// Used to list various world places
/// </summary>
public class ConsoleCommandList : ConsoleCommand
{
    public override string RunCommand(string[] args)
    {
        if(args == null || args.Length==0)
        {
            return "Define argument to list: sets, cstruct, dung";
        }

        switch (args[0])
        {
            case "sets":
                return ListSettlements();
            case "cstruct":
                return ListChunkStructures();
            case "dung":
                return ListDungeons();
        }

        return "Argument " + args[0] + " not recognised";
    }

    private string ListSettlements()
    {
        string result = "";
        foreach(KeyValuePair<int, WorldLocation> sets in GameManager.WorldManager.World.WorldLocations)
        {
            if(sets.Value is Settlement)
            {
                Settlement set = sets.Value as Settlement;
                result += set.Name + "_" + set.SettlementID + ":(" + set.Centre + "), ";
            }
                

        }
        return result;
        
    }

    private string ListChunkStructures()
    {
        string result = "";
        foreach (KeyValuePair<int, ChunkStructure> structs in GameManager.WorldManager.World.WorldChunkStructures)
        {
            result += structs.Value.ToString() + ":(" + structs.Value.ChunkPos + "), ";

        }
        return result;
    }

    private string ListDungeons()
    {
        string result = "";
        foreach (KeyValuePair<int, Subworld> subs in GameManager.WorldManager.World.WorldSubWorlds)
        {
            result += subs.Value.ToString() + "_"+subs.Value.SubworldID+ ":(" + World.GetChunkPosition(subs.Value.InternalEntrancePos) + "), ";

        }
        return result;
    }
}