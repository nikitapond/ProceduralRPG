using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using System.Collections.Generic;
using System.Collections;
public class LoadedEntity : MonoBehaviour, IGamePauseEvent
{

    public bool Selected;

    public float slopeRayHeight = 0.5f;
    public float steepSlopeAngle = 55;
    public float slopeThreshold = 0.01f;

    public static readonly float LOOK_ROTATION_SPEED = 5;
    private int GROUND_LAYER_MASK;
    public Entity Entity { get; private set; }
    public LoadedEntityAnimationManager AnimationManager { get; private set; }
    public Rigidbody RigidBody { get; private set; }

    private float VerticalVelocity;
    private float Height = -1;
    private EntityHealthBar EntityHealthBar;
    private CapsuleCollider Collider;
    //private float targetRotation;
    private float DistToGround;

    private Vector3 LookTowards;

    private Vector2 MoveDirection;
    private Vector2 TargetPosition;

    //public WeaponController WeaponController { get; private set; }

    //Used to check for entities, if Idle 
    public bool IsIdle { get; private set; }

    //private Vector3 moveDirection = Vector3.zero;
    private CharacterController controller;

    public List<Entity> NearEntities;


    void OnDrawGizmos()
    {

        if (!Selected)
            return;
        Color prev = Gizmos.color;
        Color col = OnGround() ? Color.green : Color.red;
        Gizmos.color = col;
        float DisstanceToTheGround = GetComponent<Collider>().bounds.extents.y;

        Gizmos.DrawLine(transform.position, transform.position - Vector3.up * (DisstanceToTheGround + 0.1f));
        Gizmos.color = prev;
        if (Entity.EntityAI != null)
        {
            
            if(Entity.EntityAI.EntityPath != null)
            {
                foreach(Vec2i v in Entity.EntityAI.EntityPath.Path)
                {
                    Vector3 pos = new Vector3(v.x + 0.5f, GetWorldHeight(v.x+0.5f, v.z+0.5f), v.z+0.5f);
                    Gizmos.DrawCube(pos, Vector3.one * 0.2f);
                }
            }
        }
        if(NearEntities != null)
        {
           // GameManager.DebugGUI.SetData("Entity_" + Entity.ID + "_near:", NearEntities.Count);
            foreach(Entity e in NearEntities)
            {
                if (e.Equals(Entity))
                    continue;
                Color c = Color.red;
                if (Entity.EntityAI.CombatAI.LineOfSight(e))
                {
                    c = Color.green;
                }
                Color oldC = Gizmos.color;
                Gizmos.color = c;
                Gizmos.DrawLine(Entity.Position, e.Position);
                Gizmos.color = oldC;               
  

            }
        }

        if(Entity is Player)
        {
            Vector3 floorPos = new Vector3(transform.position.x, 0, transform.position.z);
            floorPos.y = GetWorldHeight();
            Gizmos.DrawSphere(floorPos, 0.2f);
        }
    }

    public void SetEntity(Entity entity)
    {
    
        EventManager.Instance.AddListener(this);
        
        Entity = entity;
        AnimationManager = GetComponent<LoadedEntityAnimationManager>();

        DistToGround = 0;
        transform.position = new Vector3(entity.Position.x, transform.position.y, entity.Position.z);
        RigidBody = GetComponent<Rigidbody>();
        Collider = GetComponent<CapsuleCollider>();


        GameObject entityhealthBar = Instantiate(ResourceManager.GetEntityGameObject("healthbar"));
        entityhealthBar.transform.parent = transform;
        entityhealthBar.transform.localPosition = new Vector3(0, 2, 0);

        EntityHealthBar = entityhealthBar.GetComponentInChildren<EntityHealthBar>();
        controller = GetComponent<CharacterController>();
        GROUND_LAYER_MASK = LayerMask.GetMask("Ground");
        // AnimControll = GetComponentInChildren<AnimatorController>();

    }

    public void SetIdle(bool idle)
    {
        IsIdle = idle;
    }


    public void ResetPhysics()
    {
        VerticalVelocity = 0;

    }


    public bool OnGround()
    {
        return IsGrounded;
        return Physics.Raycast(transform.position+Vector3.up, -Vector3.up, 0.1f+1);
    }
    private bool IsJumping;
    private bool IsWaitingForJump;
    private bool IsFalling;
    private bool IsRunning;
    private bool IsGrounded;

    private Vector2 DesiredVelocity;
    private bool CheckMoveableTerrain(Vector3 pos, Vector3 desiredDirection, float distance)
    {

        /*float slopeRayHeight = 1f;
        float steepSlopeAngle = 89;
        float slopeThreshold = 0.01f;
        */
        Ray ray = new Ray(pos, desiredDirection);

        RaycastHit hit;
        //Raycast only the ground
        if(Physics.Raycast(ray, out hit, distance, layerMask:GROUND_LAYER_MASK))
        {
            float slopeAngle = Mathf.Deg2Rad * Vector3.Angle(Vector3.up, hit.normal);
            float radius = Mathf.Abs(slopeRayHeight / Mathf.Sin(slopeAngle)); // slopeRayHeight is the Y offset from the ground you wish to cast your ray from.

            if (slopeAngle >= steepSlopeAngle * Mathf.Deg2Rad) //You can set "steepSlopeAngle" to any angle you wish.
            {
                if (hit.distance - Collider.radius > Mathf.Abs(Mathf.Cos(slopeAngle) * radius) + slopeThreshold) // Magical Cosine. This is how we find out how near we are to the slope / if we are standing on the slope. as we are casting from the center of the collider we have to remove the collider radius.
                                                                                                                 // The slopeThreshold helps kills some bugs. ( e.g. cosine being 0 at 90° walls) 0.01 was a good number for me here
                {
                    return true; // return true if we are still far away from the slope
                }

                return false; // return false if we are very near / on the slope && the slope is steep
            }

            return true; // return true if the slope is not steep
        }
        return true;
    }

    /// <summary>
    /// Causes the entity to jump. 
    /// Checks if the entity is grounded, and if
    /// the jump is possible we play the jump animation 
    /// </summary>
    public void Jump()
    {
        IsIdle = false;
        if (!OnGround() || IsJumping || IsWaitingForJump)
            return;
        IsWaitingForJump = true;
        StartCoroutine(WaitForJumpAnimation());
    }
    void OnCollisionEnter(Collision col)
    {
        
        if (col.gameObject.CompareTag("Ground"))
        {
            IsGrounded = true;
        }
            
    }
    void OnCollisionExit(Collision col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            IsGrounded = false;
        }
            
    }
    void OnCollisionStay(Collision col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            IsGrounded = true;
        }            
    }


    /// <summary>
    /// Internal method used for jumping.
    /// Waits for the Jump animation to play before setting variables
    /// such that jumping occurs
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitForJumpAnimation()
    {
        AnimationManager.Jump();
        yield return new WaitForSeconds(AnimationManager.JumpAnimationTime);
        VerticalVelocity = 10;
        IsJumping = true;
        IsWaitingForJump = false;
        yield return new WaitForSeconds(1.5f);
        IsJumping = false;
    }

    /// <summary>
    /// Sets if the entity should currently be running or not
    /// </summary>
    /// <param name="running"></param>
    public void SetRunning(bool running)
    {
        IsIdle = false;

        IsRunning = running;
    }



    private System.Diagnostics.Stopwatch Stopwatch;
    


    /// <summary>
    /// Causes entity to look towards the specified point
    /// </summary>
    /// <param name="v">Point of interest to face towards</param>
    public void LookTowardsPoint(Vector3 v)
    {

        IsIdle = false;
        LookTowards = v;
    }
    public void LookTowardsPoint(Vector2 v)
    {

        IsIdle = false;
        LookTowards = new Vector3(v.x,0, v.y);
    }
    public void SetLookBasedOnMovement(bool onMovement)
    {
        IsIdle = false;
    }

    private float GetWorldHeight(float x, float z)
    {
        RaycastHit hit;
        if (Physics.Raycast(new Ray(new Vector3(x,64,z), Vector3.down), out hit, 64, layerMask: GROUND_LAYER_MASK))
        {
            return hit.point.y;
        }
        ChunkData chunk = GameManager.WorldManager.CRManager.GetChunk(World.GetChunkPosition(x,z));
        if (chunk.Heights != null)
            return  chunk.Heights[(int)x % World.ChunkSize, (int)z % World.ChunkSize];
        else
            return  chunk.BaseHeight;

    }

    private float GetWorldHeight()
    {
        float height = 4.5f;
        ChunkData chunk = GameManager.WorldManager.CRManager.GetChunk(World.GetChunkPosition(Entity.TilePos));
        if (chunk.Heights != null)
            height = chunk.Heights[Entity.TilePos.x % World.ChunkSize, Entity.TilePos.z % World.ChunkSize];
        else
            height = chunk.BaseHeight;

        Vector3 basePos = new Vector3(transform.position.x, 64, transform.position.z);

        
        RaycastHit hit;
        if (Physics.Raycast(new Ray(basePos, Vector3.down), out hit, 64, layerMask: GROUND_LAYER_MASK))
        {
            return hit.point.y;
        }
        /*
        if (transform.position.y < height)
        {
            RaycastHit hit;
            if(Physics.Raycast(new Ray(transform.position, Vector3.up), out hit, height+1,  layerMask: GROUND_LAYER_MASK))
            {
                return hit.point.y;
            }
        }else if(transform.position.y > height)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(transform.position, Vector3.down), out hit, height, layerMask: GROUND_LAYER_MASK))
            {
                return hit.point.y;
            }
        }
        */
        return height;
    }
    private void UpdateVerticalVelocity()
    {
        VerticalVelocity -= 9.81f * Time.fixedDeltaTime;
    }

    public void MoveTowards(Vector3 position, bool chosenPoint=false)
    {
        

        Vector3 delta = position - transform.position;
        delta.Normalize();
        MoveDirection.x = delta.x;
        MoveDirection.y = delta.z;
        if (chosenPoint)
            TargetPosition = new Vector2(position.x, position.z);
        else
            TargetPosition = new Vector2(delta.x, delta.z) * 20f;
        IsIdle = false;

        DesiredVelocity = new Vector2(position.x - transform.position.x, position.z - transform.position.z);

    }
    
    public void MoveTowards(Vector2 position)
    {
        MoveTowards(new Vector3(position.x, transform.position.y, position.y), true);
        DesiredVelocity = new Vector2(position.x - transform.position.x, position.y - transform.position.z);
    }

    public void MoveInDirection(Vector2 direction)
    {
        MoveTowards(transform.position + new Vector3(direction.x, 0, direction.y).normalized);
        DesiredVelocity = new Vector2(direction.x, direction.y);
    }
    /// <summary>
    /// Physics update for the entity
    /// TODO - fix jump gravity...
    /// </summary>
    private void FixedUpdate() {

        //RigidBody.angularVelocity = Vector3.zero;
        if (GameManager.Paused)
            return;
        if (IsIdle)
        {
          //  RigidBody.velocity = Vector3.zero;
            return;
        }



        Debug.BeginDeepProfile("le_fixed_update");


        EntityHealthBar.SetHealthPct(Entity.CombatManager.CurrentHealth / Entity.CombatManager.MaxHealth);


        Vec2i cPos = World.GetChunkPosition(transform.position);

        //Check if the current chunk is loaded
        if (!GameManager.WorldManager.CRManager.IsCurrentChunkPositionLoaded(cPos))
        {
            //If not, keep position dixed with +ve y value, to prevent falling through world.
            transform.position = new Vector3(transform.position.x, 6, transform.position.z);
            return;
        }


        float ground = GetWorldHeight();

        float velY = RigidBody.velocity.y;

        //If we are below the ground, reset position to be at ground level
        if(transform.position.y < ground)
        {
            transform.position = new Vector3(transform.position.x, ground, transform.position.z);
        }
        //If our DesiredVelocity is non 0
        if(DesiredVelocity != Vector2.zero)
        {
            //We check if the terrain we are moving into is valid to move
            if(CheckMoveableTerrain(transform.position, new Vector3(DesiredVelocity.x, 0, DesiredVelocity.y), 10f))
            {
                //Calculate and set velocity
                float moveSpeed = IsRunning?Entity.MovementData.RunSpeed:Entity.MovementData.WalkSpeed;
                RigidBody.velocity = new Vector3(DesiredVelocity.x* moveSpeed, velY, DesiredVelocity.y* moveSpeed);
                //inform animation manager of velocity
                AnimationManager.SetSpeedPercentage(moveSpeed / Entity.MovementData.RunSpeed);
            }
            DesiredVelocity = Vector2.zero;
            AnimationManager.SetSpeedPercentage(0);
        }
        else
        {
            AnimationManager.SetSpeedPercentage(0);
        }

        //If we have a specified look direction
        if (LookTowards != Vector3.zero)
        {
            float angle = Vector3.SignedAngle(Vector3.forward, LookTowards - transform.position, Vector3.up);
            Quaternion quat = Quaternion.Euler(new Vector3(0, angle, 0));
            transform.rotation = Quaternion.Slerp(transform.rotation, quat, Time.fixedDeltaTime * LOOK_ROTATION_SPEED);
            Entity.SetLookAngle(transform.rotation.eulerAngles.y);
        }

        //The final y coord we will end up at
        float finalY = transform.position.y;
        if (finalY < ground)
            finalY = ground;
        bool IsGrounded = transform.position.y - ground <= 0.1f;


        //Debug info for player Jumping
        if (Entity is Player)
        {
            DebugGUI.Instance.SetData("jump_vel", VerticalVelocity);
            DebugGUI.Instance.SetData("onGround", IsGrounded);
            DebugGUI.Instance.SetData("isJumping", IsJumping);
            DebugGUI.Instance.SetData("isFalling", IsFalling);

        }

        /*
        //Falling & jumping code:

        //We change our vertical velocity as required
        VerticalVelocity -= 9.81f * Time.fixedDeltaTime;
        if(IsGrounded && VerticalVelocity < 0)
        {
            //if we are grounded, and our velocity is negative:
            //If we were falling or jumping, we now land
            if (IsJumping || IsFalling)
            {
                AnimationManager.LandJump();
            }

            VerticalVelocity = 0;
            IsJumping = false;
            IsFalling = false;
        }
        
        //If the entity is currently grounded and moving down
        if (IsGrounded && VerticalVelocity < 0)
        {
            //If we were falling or jumping, we now land
            if (IsJumping || IsFalling)
            {
                AnimationManager.LandJump();
            }

            velY = 0;
            IsJumping = false;
            IsFalling = false;

            finalY = ground;

            //maybe we remove this?
           // transform.position = new Vector3(transform.position.x, ground, transform.position.z);

        }
        else if (IsGrounded && VerticalVelocity > 0)
        {
            //Update velocity
            velY -= 9.81f * Time.fixedDeltaTime;

            //Change height due to velocity + acc
            //finalY = transform.position.y + VerticalVelocity * Time.fixedDeltaTime - 9.81f * Time.fixedDeltaTime * Time.fixedDeltaTime;
            //if (finalY < ground)
            //    finalY = ground;
            //transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }//We are on the ground, and our vertical velocity is small enough to cancle
        else if (IsGrounded && Mathf.Abs(VerticalVelocity) < 0.1f)
        {
            VerticalVelocity = 0;
            IsFalling = false;
            IsJumping = false;
        }
        else
        {
            //We are not grounded
            //We are not jumping or falling (yet
            if(!(IsJumping || IsFalling))
            {
                //If the down is only a little bit down, 
                //We move to the down position
                if (transform.position.y - ground < 0.3f)
                {
                    finalY = ground;
                   // transform.position = new Vector3(transform.position.x, ground, transform.position.z);
                }
                else
                {
                    //if we falling further, we set falling to true
                    IsFalling = true;
                    AnimationManager.SetFalling();
                }
            }
            else
            {
                //If we are not grounded, and we are either jumping or falling, we must update our velocity.
                //Update velocity
                VerticalVelocity -= 9.81f * Time.fixedDeltaTime;
                //Change height due to velocity + acc
                finalY = transform.position.y + VerticalVelocity * Time.fixedDeltaTime - 9.81f * Time.fixedDeltaTime * Time.fixedDeltaTime;
                if (finalY < ground)
                    finalY = ground;
               // transform.position = new Vector3(transform.position.x, y, transform.position.z);
            }

            
            /*
            //If we are not grounded, but we aren't currently falling or jumping, 
            //then we must be falling
            if (!(IsJumping || IsFalling))
            {
                IsFalling = true;
                AnimationManager.SetFalling();
            }

            //Update velocity
            VerticalVelocity -= 9.81f * Time.fixedDeltaTime;
            //Change height due to velocity + acc
            float y = transform.position.y + VerticalVelocity * Time.fixedDeltaTime - 9.81f * Time.fixedDeltaTime * Time.fixedDeltaTime;
            if (y < ground)
                y = ground;
            transform.position = 
        }*/
        transform.position = new Vector3(transform.position.x, finalY, transform.position.z);








        Entity.SetPosition(transform.position);

        Debug.EndDeepProfile("le_fixed_update");

        return;

        //If we have a specified move direction
        if (!(MoveDirection == Vector2.zero))
        {
            float tileSpeed = 1; //TODO - get tile speed
            //Finds correct speed associated with running/walking for this entity
            float entitySpeed = IsRunning?Entity.MovementData.RunSpeed:Entity.MovementData.WalkSpeed;

            AnimationManager.SetSpeedPercentage(entitySpeed / Entity.MovementData.RunSpeed);

            Vector2 v2Pos = new Vector2(transform.position.x, transform.position.z);
            Vector2 targetDisp = TargetPosition - v2Pos;
            float targetMag = targetDisp.magnitude;
            Vector3 vel = new Vector3(MoveDirection.x, 0, MoveDirection.y) * tileSpeed * entitySpeed;

            //controller.SimpleMove(vel);
            



            float oldVy = RigidBody.velocity.y;

            //Vector3 vel = new Vector3(MoveDirection.x, 0, MoveDirection.y) * tileSpeed * entitySpeed;
            RigidBody.velocity = new Vector3(MoveDirection.x, 0, MoveDirection.y) * tileSpeed * entitySpeed;
            //If the distance the body will move in a frame (|velocity|*dt) is more than the desired amount (target mag)
            //Then we must scale the velocity down
            if(RigidBody.velocity.magnitude*Time.fixedDeltaTime > targetMag)
            {

               RigidBody.velocity = RigidBody.velocity * targetMag/(Time.fixedDeltaTime* RigidBody.velocity.magnitude);
            }

            RigidBody.velocity += Vector3.up * oldVy;
            
            Vector3 moveTo = transform.position + vel * Time.fixedDeltaTime*5;
            float moveToHeight = GetWorldHeight(moveTo.x, moveTo.z);

            if (Entity is Player)
            {
                Debug.Log("ground: " + ground + " move to" + moveToHeight);
            }
            if(moveToHeight > ground)
            {
                if ((moveToHeight - ground) * Time.fixedDeltaTime < 0.5f)
                    transform.position = new Vector3(transform.position.x, moveToHeight, transform.position.z);
            }
            else
            {
                transform.position = new Vector3(transform.position.x, moveToHeight, transform.position.z);
            }

            if (Entity is Player)
            {
                DebugGUI.Instance.SetData("Vel", RigidBody.velocity + " | " + vel);
            }

        }
        else
        {
            AnimationManager.SetSpeedPercentage(0);
        }
        //If we have a specified look direction
        if (LookTowards != Vector3.zero)
        {
            float angle = Vector3.SignedAngle(Vector3.forward, LookTowards - transform.position, Vector3.up);
            Quaternion quat = Quaternion.Euler(new Vector3(0, angle, 0));
            transform.rotation = Quaternion.Slerp(transform.rotation, quat, Time.fixedDeltaTime * LOOK_ROTATION_SPEED);
            Entity.SetLookAngle(transform.rotation.eulerAngles.y);
        }


        //If the entity is currently grounded and moving down
        if (IsGrounded && VerticalVelocity <0)
        {

            //If we were falling or jumping, we now land
            if(IsJumping || IsFalling)
            {
                AnimationManager.LandJump();
            }

            VerticalVelocity = 0;
            IsJumping = false;
            IsFalling = false;
            //maybe we remove this?
            transform.position = new Vector3(transform.position.x, ground, transform.position.z);

        }else if(IsGrounded && VerticalVelocity > 0)
        {
            //Update velocity
            VerticalVelocity -= 9.81f * Time.fixedDeltaTime;
            //Change height due to velocity + acc
            float y = transform.position.y + VerticalVelocity * Time.fixedDeltaTime - 9.81f * Time.fixedDeltaTime * Time.fixedDeltaTime;
            if (y < ground)
                y = ground;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }
        else if(IsGrounded && Mathf.Abs(VerticalVelocity) < 0.1f)
        {
            VerticalVelocity = 0;
            IsFalling = false;
            IsJumping = false;
        }else
        {
            //We are not grounded

            //If we are not grounded, but we aren't currently falling or jumping, 
            //then we must be falling
            if(!(IsJumping || IsFalling))
            {
                IsFalling = true;
                AnimationManager.SetFalling();
            }

            //Update velocity
            VerticalVelocity -= 9.81f * Time.fixedDeltaTime;
            //Change height due to velocity + acc
            float y = transform.position.y + VerticalVelocity * Time.fixedDeltaTime - 9.81f * Time.fixedDeltaTime * Time.fixedDeltaTime;
            if (y < ground)
                y = ground;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }

        //Check if we are on the ground
        bool IsOnGround = OnGround();

        //Debug info for player Jumping
        if(Entity is Player)
        {
            DebugGUI.Instance.SetData("jump_vel", VerticalVelocity);
            DebugGUI.Instance.SetData("onGround", IsOnGround);
            DebugGUI.Instance.SetData("isJumping", IsJumping);
            DebugGUI.Instance.SetData("isFalling", IsFalling);

        }
        /*
        if(VerticalVelocity > 0)
        {
            VerticalVelocity += Physics.gravity.y * Time.fixedDeltaTime;
            RigidBody.velocity = RigidBody.velocity + Vector3.up * VerticalVelocity;
            if (VerticalVelocity < 0)
                VerticalVelocity = 0;
        }
        */

        /*
        if (IsOnGround && VerticalVelocity < 0)
        {
            VerticalVelocity = 0;
            IsJumping = false;
            IsFalling = false;
        }*/
        /*
        //Check if we are off the ground
        if (!IsOnGround)
        {
            //If so, update vertical position and velocity accordingly
            VerticalVelocity -= 9.81f * Time.fixedDeltaTime;
            Height += VerticalVelocity * Time.fixedDeltaTime - 9.81f * Time.fixedDeltaTime * Time.fixedDeltaTime;
            //transform.position += Vector3.up * (VerticalVelocity * Time.fixedDeltaTime+9.81f*Time.fixedDeltaTime*Time.fixedDeltaTime);
            //Next, if we are off the ground but not jumping, it must mean we're falling
            if (!IsJumping)
            {
                //If not currently falling, set it to true
                if (!IsFalling)
                {
                    IsFalling = true;
                    AnimationManager.SetFalling();
                }
            }
        }
        else if (VerticalVelocity > 0) {
            //If we are on the ground, but our velocity is positive, we must be jumping
            VerticalVelocity -= 9.81f * Time.fixedDeltaTime;
            Height += VerticalVelocity * Time.fixedDeltaTime;

        }
        else if ((IsJumping || IsFalling) && !IsWaitingForJump)
        {
            //If we are on the ground, but still jumping or falling, set both to false. reset velocity
            IsJumping = false;
            VerticalVelocity = 0;
            IsFalling = false;
            //Then play the land animation
            AnimationManager.LandJump();
        }
        float worldHeight = GetWorldHeight();
        if (transform.position.y < worldHeight)
        {
            transform.position = new Vector3(transform.position.x, worldHeight, transform.position.z);
            VerticalVelocity = 0;
        }

        //transform.position = new Vector3(transform.position.x, Height, transform.position.z);
        */
       /* float worldHeight = GetWorldHeight();
        if (transform.position.y < worldHeight)
        {
            transform.position = new Vector3(transform.position.x, worldHeight, transform.position.z);
            VerticalVelocity = 0;
        }*/
        //Reset variables 
        LookTowards = Vector3.zero;
        MoveDirection = Vector2.zero;


        Entity.SetPosition(transform.position);

        Debug.EndDeepProfile("le_fixed_update");

        return;


     
    }


    public void GamePauseEvent(bool pause)
    {
      
    }
}