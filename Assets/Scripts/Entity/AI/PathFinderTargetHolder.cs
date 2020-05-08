using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Script used to hold onto the transforms of path finding targets
/// </summary>
public class PathFinderTargetHolder : MonoBehaviour
{
    public static Transform Holder;
    // Start is called before the first frame update
    private void Awake()
    {
        Holder = transform;
    }
}
