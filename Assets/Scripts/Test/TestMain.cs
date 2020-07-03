using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
/// <summary>
/// Contains methods required for initiating a game, such as setting up the player etc
/// </summary>
public class TestMain
{
    public static bool TEST = false;
    public static void SetupTest()
    {
        TEST = true;
        EventManager em = new EventManager();
        ResourceManager.LoadAllResources();
        World world = new World();
        WorldManager.Instance.SetWorld(world);
    }
    public static Player CreatePlayer(Vector3 position)
    {
        Player player = new Player();
        player.SetPosition(position);
        
        PlayerManager.Instance.SetPlayer(player);
        
        return player;
    }

}