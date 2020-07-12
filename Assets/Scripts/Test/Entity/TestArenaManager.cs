using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestArenaManager : MonoBehaviour
{

    private void Awake()
    {
        GameManager.RNG = new GenerationRandom(0);
        ResourceManager.LoadAllResources();

        EventManager em = new EventManager();


        Player player = new Player();
        PlayerManager.Instance.SetPlayer(player);
    }
    // Start is called before the first frame update
    void Start()
    {
        EntityFaction bandits = new EntityFaction("Bandits!");
        for(int i =0; i<5; i++)
        {
            Vector2 pos = GameManager.RNG.RandomVector2(15, 25);
            Bandit b = new Bandit();
            b.SetEntityFaction(bandits);
            b.MoveEntity(Vec2i.FromVector2(pos));
            EntityManager.Instance.LoadEntity(b);

        }


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
