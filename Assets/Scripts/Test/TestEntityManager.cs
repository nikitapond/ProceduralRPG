using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEntityManager : MonoBehaviour
{

    public GameObject EntityPrefab;
    // Start is called before the first frame update
    void Start()
    {
        return;
        int maxSize = 32 * World.ChunkSize;
        GenerationRandom genRan = new GenerationRandom(0);
        for(int i=0; i<20; i++)
        {
            Vec2i pos = genRan.RandomVec2i(maxSize/2, maxSize);
            GameObject ent = Instantiate(EntityPrefab);
            ent.transform.parent = transform;
            ent.transform.position = new Vector3(pos.x, 0, pos.z);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
