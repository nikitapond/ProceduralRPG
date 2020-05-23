using UnityEngine;
using UnityEditor;

public interface IGamePauseEvent : IEventListener { void GamePauseEvent(bool pause); }

public class GamePause : IEvent
{
 
    public bool Pause { get; private set; }
    public GamePause(bool pause)
    {
        Pause = pause;
        Debug.Log("pause: " + pause);
    }
}


/*
public interface IPlayerPickupItemEvent : IEventListener { void PlayerPickupItemEvent(PlayerPickupItem ev); }
public class PlayerPickupItem : IEvent
{
    public Item Item { get; private set; }
    public PlayerPickupItem(Item item)
    {
        Item = item;
    }
}

public interface IPlayerDropItemEvent : IEventListener { void PlayerDropItemEvent(PlayerDropItem ev); }
public class PlayerDropItem : IEvent
{
    public Item Item { get; private set; }
    public PlayerDropItem(Item item)
    {
        Item = item;
    }
}*/