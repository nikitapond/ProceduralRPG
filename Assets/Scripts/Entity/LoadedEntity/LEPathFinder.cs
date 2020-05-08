using UnityEngine;
using UnityEditor;
using Pathfinding;
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

    public Vector3 Target { get; private set; }

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
    }
    public void SetTarget(Vector2 target2, float repath=20)
    {
        //  Debug.Log("Setting target to " + target2);
        //DestinationSetter.target = PlayerManager.Instance.Player.GetLoadedEntity().transform;
        Vector3 targetGlobal = new Vector3(target2.x, transform.position.y, target2.y);
        Vector3 targetLocal = targetGlobal - transform.position;
        TargetObject.transform.position = targetGlobal;

        AIPathFinder.repathRate = repath;
        Target = targetGlobal;
        //AIPathFinder.SearchPath();

    }
    public void SetTarget(Vector3 target)
    {
        Debug.Log("Setting target to " + target);
        TargetObject.transform.position = target;
        Target = target;

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
}