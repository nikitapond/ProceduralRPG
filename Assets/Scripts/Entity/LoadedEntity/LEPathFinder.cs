using UnityEngine;
using UnityEditor;
using Pathfinding;
using System.Collections;
using System;
/// <summary>
/// A component added to all entities except the player.
/// Controlls path finding details
/// </summary>
public class LEPathFinder : MonoBehaviour
{
    private AIDestinationSetter DestinationSetter;
    private AIPath AIPathFinder;
    private GameObject TargetObject;

    private LoadedEntity LoadedEntity;
    private Rigidbody RB;

    public Vector3 Target { get; private set; }

    public Vector3 Direction { get { return AIPathFinder.steeringTarget; } }

    /// <summary>
    /// Function form to call when an entity is close to target (within distance of 1)
    /// </summary>
    /// <param name="target"></param>
    //public delegate void OnCloseToTarget(params object[] args);

    private Action<object[]> TargetCallback_;
    //private OnCloseToTarget TargetCallback;
    private object[] CallbackArgs;

    private void Awake()
    {
        
        AIPathFinder = gameObject.AddComponent<AIPath>();
        DestinationSetter = gameObject.AddComponent<AIDestinationSetter>();
        TargetObject = new GameObject();
        TargetObject.transform.parent = PathFinderTargetHolder.Holder;
        TargetObject.transform.localPosition = new Vector3(float.MaxValue, float.MaxValue);
        DestinationSetter.target = TargetObject.transform;
        AIPathFinder.radius = 0.4f;
        LoadedEntity = GetComponent<LoadedEntity>();
        //StartCoroutine(WaitThenConstrain(1));
        //AIPathFinder.constrainInsideGraph = true;
        AIPathFinder.updateRotation = true;
        RB = LoadedEntity.gameObject.GetComponent<Rigidbody>();


    }


    private IEnumerator WaitThenConstrain(float t)
    {
        yield return new WaitForSeconds(t);
        AIPathFinder.constrainInsideGraph = true;
    }

    public float CurrentSpeed()
    {
        return AIPathFinder.velocity.magnitude;
    }

    /// <summary>
    /// Tells the path finder to travel towards the specified entity.
    /// This results in the path target to being just infront of the other entity, 
    /// from the viewpoint of this entity.
    /// </summary>
    /// <param name="entity"></param>
    public void SetEntityTarget(Entity entity, float range=1)
    {
        Vector2 targetPos = entity.Position2;
        Vector2 ourPos = LoadedEntity.Entity.Position2;

        Vector2 dif = (targetPos - ourPos).normalized;
        Vector2 finalTarget = targetPos - dif * range;
        SetTarget(finalTarget, 0.1f);

        LoadedEntity.LookTowardsEntity(entity);



    }
    public void SetTarget(Vector2 target2, float repath=20)
    {
        //  Debug.Log("Setting target to " + target2);
        //DestinationSetter.target = PlayerManager.Instance.Player.GetLoadedEntity().transform;
        Vector3 targetGlobal = new Vector3(target2.x, transform.position.y, target2.y);
        Vector3 targetLocal = targetGlobal - transform.position;
        TargetObject.transform.position = targetGlobal;
        LoadedEntity.SetLookBasedOnMovement(true);
        AIPathFinder.repathRate = repath;
        Target = targetGlobal;

        LoadedEntity.SetLookBasedOnMovement(true);

        //AIPathFinder.SearchPath();

    }
    public void SetTarget(Vector3 target, Action<object[]> callback, object[] callbackArgs)
    {
        TargetObject.transform.position = target;
        Target = target;
        Debug.Log("UMH:" + callback + ":" + callbackArgs);
        LoadedEntity.SetLookBasedOnMovement(true);
        this.TargetCallback_ = callback;
        this.CallbackArgs = callbackArgs;
        LoadedEntity.SetIdle(false);

    }
    public void SetTarget(Vector3 target)
    {
        LoadedEntity.SetLookBasedOnMovement(true);
        TargetObject.transform.position = target;
        Target = target;


    }
    /// <summary>
    /// Called periodically to check if the path finder has reached its target.
    /// <br/> If the target is reached, we check for 
    /// </summary>
    public void Tick()
    {
        RB.velocity = AIPathFinder.steeringTarget;



        if (CallbackArgs != null)
        {
            float reqDist = 1.5f;
            if (CallbackArgs.Length >= 2)
            {
                Debug.Log(CallbackArgs[1]);
                if (CallbackArgs[1] is float || CallbackArgs[1] is int)
                    reqDist = (float)CallbackArgs[1];               
            }
            Vector2 disp = TargetObject.transform.position.XZ() - transform.position.XZ();
            float dist = disp.sqrMagnitude;

            if (dist <= reqDist*reqDist)
            {
                TargetCallback_(CallbackArgs);
            }

        }
        return;
    }

    /// <summary>
    /// Used to set the distance between this entity and the player.
    /// The distance should be the square of the distance, to prevent need of sqr root function
    /// This distance is used to set resolutions for the path finder:
    /// When close to the player, the re-path rate is high and the 'pickNestWaypoint' islow.
    /// 
    /// </summary>
    /// <param name="sqrDistance"></param>
    public void SetDistanceToPlayer(int sqrDistance)
    {
        AIPathFinder.canSearch = true;
        //Within 1 chunk of player
        if (sqrDistance < World.ChunkSize * World.ChunkSize)
        {
            AIPathFinder.repathRate = 0.5f;
            AIPathFinder.pickNextWaypointDist = 0.2f;
        }else if(sqrDistance < World.ChunkSize*World.ChunkSize * 4)
        {//Within 2 chunks of player
            AIPathFinder.repathRate = 0.75f;
            AIPathFinder.pickNextWaypointDist = 0.5f;
        }else if(sqrDistance < World.ChunkSize*World.ChunkSize * 9)
        {
            //Within 3 chunks of player
            AIPathFinder.repathRate = 1f;
            AIPathFinder.pickNextWaypointDist = 0.8f;
        }
        else
        {
            AIPathFinder.canSearch = false;

        }
    }
    /// <summary>
    /// Sets the speed the entity can move at
    /// </summary>
    /// <param name="maxSpeed"></param>
    public void SetSpeed(float maxSpeed)
    {
        AIPathFinder.maxSpeed = maxSpeed;
    }

    public void SetPause(bool pause)
    {
        AIPathFinder.canMove = !pause;
    }


    private void OnDestroy()
    {
        Destroy(TargetObject);
    }
}