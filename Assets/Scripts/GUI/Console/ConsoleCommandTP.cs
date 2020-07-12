using UnityEngine;
using UnityEditor;

public class ConsoleCommandTP : ConsoleCommand
{
    public override string RunCommand(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return "Define argument to teleport. sets, cstruct, dungs, chunk, wpos";
        }

        switch (args[0])
        {
            case "sets":
                return TeleportToSettlements(args);
            case "cstruct":
                return TeleportToChunkStructure(args);
            case "dung":
                return TeleportToDungeon(args);
            case "chunk":
                return TeleportToChunk(args);
            case "wpos":
                return TeleportToWorldPosition(args);

        }
        return "Argument " + args[0] + "not recognised. Argument should be: sets, cstruct, dung, chunk, wpos";
    }


    private string TeleportToSettlements(string[] args)
    {
        if (args.Length < 2)
            return "Specify which settlement";
        if(int.TryParse(args[1], out int setID)){

            Settlement set = GameManager.WorldManager.World.GetSettlement(setID);
            if (set == null)
                return "Settlement with ID " + setID + " not found";
            GameManager.PlayerManager.Player.MoveEntity(set.Centre * World.ChunkSize);
            return "Teleporting to settlement " + setID; 
        }
        else
        {
            return "Argument must be ID of settlement, " +args[1] + " not recognised";
        }        
    }

    private string TeleportToChunkStructure(string[] args)
    {
        if (int.TryParse(args[1], out int structID))
        {

            ChunkStructure cstruct = GameManager.WorldManager.World.GetChunkStructure(structID);
            if (cstruct == null)
                return "Chunk structure with ID " + structID + " not found";
            GameManager.PlayerManager.Player.MoveEntity(cstruct.ChunkPos * World.ChunkSize);
            return "Teleporting to chunk structure " + structID;
        }
        else
        {
            return "Argument must be ID of chunk structure, " + args[1] + " not recognised";
        }
    }
    private string TeleportToDungeon(string[] args)
    {
        if (int.TryParse(args[1], out int dunID))
        {

            Subworld subw = GameManager.WorldManager.World.GetSubworld(dunID);
            if (subw == null)
                return "Dungeon with ID " + dunID + " not found";
            GameManager.PlayerManager.Player.MoveEntity(subw.InternalEntrancePos);
            return "Teleporting to dungeon " + dunID;
        }
        else
        {
            return "Argument must be ID of chunk structure, " + args[1] + " not recognised";
        }
    }
    private string TeleportToChunk(string[] args)
    {
        if (int.TryParse(args[1], out int x) && int.TryParse(args[2], out int z))
        {

            if (x < 0 || x >= World.WorldSize || z < 0 || z >= World.WorldSize)
                return "Specified coordinate is outside of world";
            GameManager.PlayerManager.Player.MoveEntity(new Vec2i(x, z) * World.ChunkSize);
            return "Player teleporting to chunk " + x + "," + z;

        }
        else
        {
            return "Chunk coordinate " + args[1] + "," + args[2] + " not recognised";
        }
    }
    private string TeleportToWorldPosition(string[] args)
    {
        if (int.TryParse(args[1], out int x) && int.TryParse(args[2], out int z))
        {

            if (x < 0 || x >= World.WorldSize*World.ChunkSize || z < 0 || z >= World.WorldSize * World.ChunkSize)
                return "Specified coordinate is outside of world";
            GameManager.PlayerManager.Player.MoveEntity(new Vec2i(x, z));
            return "Player teleporting to position " + x + "," + z;

        }
        else
        {
            return "World coordinate " + args[1] + "," + args[2] + " not recognised";
        }
    }
}