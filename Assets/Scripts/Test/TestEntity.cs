﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Pathfinding;
public class TestEntity : MonoBehaviour
{
    public GameObject Player;
    private AIPath aip;
    // Start is called before the first frame update
    void Start()
    {
        aip = GetComponent<AIPath>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
