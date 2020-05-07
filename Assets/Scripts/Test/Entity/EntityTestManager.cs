using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityTestManager : MonoBehaviour
{
    EntityManager EntityManager;
    EventManager EventManager;
    PlayerManager PlayerManager;
    private void Awake()
    {
        ResourceManager.LoadAllResources();
        EntityManager = GetComponent<EntityManager>();
        EventManager = new EventManager();
        PlayerManager = GetComponentInChildren<PlayerManager>();
        Player player = new Player();
        PlayerManager.SetPlayer(player);
    }

    void Start()
    {
        Bandit testNPC = new Bandit();
        EntityManager.AddFixedEntity(testNPC);
        EntityManager.LoadChunk(new Vec2i(0, 0));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
