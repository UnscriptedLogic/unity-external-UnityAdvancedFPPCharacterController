using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

/// <summary>
/// Sub-class of PlayerMovement, configure the default values here if you don't have a prefab yet.
/// </summary>
[CreateAssetMenu(fileName = "PlayerMoveData", menuName = "Advanced FPS Controller/PlayerMoveData", order = 0)]
public class PlayerMoveData : ScriptableObject 
{   
    public bool tagMovement = true;
    public float acceleration = 10f;
    public float tagSpeed = 3.25f;
    public float tagDelay = 1f;
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float slideSpeed = 7.5f;
    public float walkDamp = 4.5f;
    public float sprintDamp = 3.95f;
    public float crouchDamp = 20f;
    public float airSmoothen = 2.25f;

    [Header("Crouching and Sliding")]
    public bool canSlide;
    [Range(0, 20.0f)] public float heightLerp = 10f;
    public float slideDuration = 200f;
    public float slideCooldown = 200f;
    public float slideDeacceleration = 2f;
    public float standHeight = 2f;
    public float crouchHeight = 1f;

    [Header("Jumping")]
    public float jumpForce = 2.5f;

    [Header("Ground Detection")]
    public LayerMask groundMask;
    public float antiBump = 10;
    public float FallTimeMax = 0.25f;
    public float CoyoteTimeMax = 0.1f;
    public float groundDistance = 1f;
    public float slopeSlideSpeed = 1f;
}
