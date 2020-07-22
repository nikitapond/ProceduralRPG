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

    private Vector2 GridSize;


    public Vector3 Target { get; private set; }
    private bool IsExactTarget;
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
        LoadedEntity = GetComponent<LoadedEntity>();
        AIPathFinder = gameObject.AddComponent<AIPath>();
        DestinationSetter = gameObject.AddComponent<AIDestinationSetter>();
        TargetObject = new GameObject();
        TargetObject.transform.parent = PathFinderTargetHolder.Holder;
        TargetObject.transform.localPosition = new Vector3(float.MaxValue, float.MaxValue);
        DestinationSetter.target = TargetObject.transform;

        Target = LoadedEntity.transform.position;

        AIPathFinder.radius = 0.4f;
        
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
        Target = entity.Position;
        IsExactTarget = true;
    }
    public void SetTarget(Vector2 target2, float repath=20)
    {
        //  Debug.Log("Setting target to " + target2);
        //DestinationSetter.target = PlayerManager.Instance.Player.GetLoadedEntity().transform;
        Vector3 targetGlobal = new Vector3(target2.x, transform.position.y, target2.y);
        TargetObject.transform.position = targetGlobal;
        AIPathFinder.repathRate = repath;

        InternalSetTargetPosition(targetGlobal);
        //AIPathFinder.SearchPath();

    }
    public void SetTarget(Vector3 target, Action<object[]> callback, object[] callbackArgs)
    {
        
        this.TargetCallback_ = callback;
        this.CallbackArgs = callbackArgs;
        InternalSetTargetPosition(target);

    }
    public void SetTarget(Vector3 target)
    {
        InternalSetTargetPosition(target);
    }
    /// <summary>
    /// Called by all position target setters. Ensures that the position set
    /// is within the current path finding bounds. <br/>If the point is outside the map,
    /// we find the nearest point and set as the target, whilst saving the final target position.
    /// We then pass this final target position 
    /// </summary>
    /// <param name="target"></param>
    private void InternalSetTargetPosition(Vector3 target)
    {
        if(target == Vector3.zero)
        {
            Debug.Log("set target to 0");
        }
        Debug.Log(LoadedEntity.Entity + " again");
        //Ensure target is set        
        Target = target;
        //Find the neasest node
        NNInfo nninfo = AstarPath.active.GetNearest(target);
        
        //If the node is far from the target position, we ensure that the path finder knows
        //that this is not the final position.
        if ((target - nninfo.position).XZ().sqrMagnitude > 4)
        {
            IsExactTarget = false;
            TargetObject.transform.position = nninfo.position;
            //Debug.Log(LoadedEntity.Entity + " has non exact PF: " + nninfo.position + "->" + target);

        }
        else
        {
            IsExactTarget = true;
            TargetObject.transform.position = target;
            //Debug.Log(LoadedEntity.Entity + " has exact PF: " + target);
        }
        LoadedEntity.SetLookBasedOnMovement(true);

        LoadedEntity.SetIdle(false);


     
    }


    /// <summary>
    /// Called periodically to check if the path finder has reached its target.
    /// <br/> If the target is reached, we check for 
    /// </summary>
    public void Tick()
    {
        //If the target is non exact, we must try to target the exact position.
        if (!IsExactTarget)
        {
            
            //Get nearest node to the target
            NNInfo nninfo = AstarPath.active.GetNearest(Target);

            //If the target is far from the nearest node, we ensure we know its not the final position
            if ((Target - nninfo.position).XZ().sqrMagnitude > 4)
            {
                //Debug.Log(LoadedEntity?.Entity + " has target outside of map");
                //If the target is far from its nearest node, but the entity is close to the nearest node, 
                //then we know the entity is at the edge of the map
                //We check if the player can see the entity, if not, we teleport to our target position.
                if ((nninfo.position - LoadedEntity.transform.position).XZ().sqrMagnitude < 9)
                {
                    //LoadedEntity.SpeechBubble.PushMessage("Close to edge?: Target: " + Target + "->" + nninfo.position + "->" + LoadedEntity.transform.position);
                    if (!EntityManager.PlayerCanSeeEntity(LoadedEntity.Entity))
                    {
                        LoadedEntity.SpeechBubble.PushMessage("Tp to target");
                        LoadedEntity.Entity.MoveEntity(Target.XZ());
                        IsExactTarget = true;
                        return;
                    }
                }
                else
                {
                    //LoadedEntity.SpeechBubble.PushMessage("dist to node?: Target: " + Target + "->" + nninfo.position + "->" + LoadedEntity.transform.position);
                }
                IsExactTarget = false;
                TargetObject.transform.position = nninfo.position;
               // Debug.Log(LoadedEntity?.Entity + " has non exact PF: " + nninfo.position + "->" + Target);
                //If 
            }
            else
            {
                IsExactTarget = true;
                TargetObject.transform.position = Target;
                //Debug.Log(LoadedEntity?.Entity + " has exact PF: " + Target);

            }

            /*
            Vector3 playerPos = PlayerManager.Instance.Player.Position;
            float dx = playerPos.x - Target.x;
            float dz = playerPos.z - Target.z;
            Debug.Log(LoadedEntity.Entity + " target at " + Target + " Player pos: " + playerPos + " dispalcement (xz): [" + dx + "," + dz + "]" + "test: " + GridSize);
            float abs_dx = Mathf.Abs(dx);
            float abs_dz = Mathf.Abs(dz);
            Vector3 nTarget = Target;
            IsExactTarget = true;
            if (abs_dx > GridSize.x)
            {
                IsExactTarget = false;
                float xSign = Mathf.Sign(dx);

                nTarget.x = playerPos.x + 0.95f * xSign * GridSize.x;
            }
            if (abs_dz > GridSize.y)
            {
                IsExactTarget = false;
                float zSign = Mathf.Sign(dz);

                nTarget.z = playerPos.z + 0.95f * zSign * GridSize.y;

            }
            TargetObject.transform.position = nTarget;
            */

        }


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