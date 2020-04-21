﻿using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
/// <summary>

/// </summary>
public class LoadedEntityAnimationManager : MonoBehaviour, IGamePauseEvent
{

    public float JumpAnimationTime = 0.3f;

    protected LoadedEntity LoadedEntity;
    protected Animator Animator;
    protected virtual void Start()
    {
        LoadedEntity = GetComponent<LoadedEntity>();
        Animator = GetComponent<Animator>();
        EventManager.Instance.AddListener(this);
    }


    protected virtual void Update()
    {
        float maxSpeed = LoadedEntity.Entity.MaxMoveSpeed;
        float curSpeed = LoadedEntity.RigidBody.velocity.magnitude;
        float speedPercent = Mathf.Clamp(curSpeed / maxSpeed * 1.4f,0,1);
        Animator.SetFloat("SpeedPercent", speedPercent, 0.1f, Time.deltaTime);

    }

    public virtual void Jump()
    {
        Animator.SetTrigger("Jump");
    }

    public virtual void LandJump()
    {
        Animator.SetTrigger("LandJump");
        //Set speed to 1 incase we have set to 0 for jumping animation
        Animator.speed = 1;
    }
    /// <summary>
    /// Sets the entity to the final pose of its jump animation
    /// while also freezing the animator. The result is that, while falling,
    /// we are always in a fall position
    /// </summary>
    public virtual void SetFalling()
    {
        
        Animator.speed = 0;
        Animator.Play("Armature|Jump", 0, 1);
    }


    /// <summary>
    /// Triggers the animator to call attack left.
    /// </summary>
    public virtual void AttackLeft()
    {

    }
    /// <summary>
    /// Triggers the animator to call attack right.
    /// </summary>
    public virtual void AttackRight()
    {

    }

    public void GamePauseEvent(bool pause)
    {

        Animator.enabled = !pause;
    }


    public LoadedHumanoidAnimatorManager HumanoidCast()
    {
        return this as LoadedHumanoidAnimatorManager;
    }

    void OnDisable()
    {
        EventManager.Instance.RemoveListener(this);
    }

}