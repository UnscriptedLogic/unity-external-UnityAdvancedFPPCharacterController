using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This script handles walking, sprinting, jumping, and crouching. It also has coyote time and good slope movement.
/// Slope Movement has been implemented and modified from - https://www.youtube.com/watch?v=GI5LAbP5slE&ab_channel=Hero3D
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    #region Components
    [Header("References")]
    [SerializeField] public PlayerMoveData moveData;
    [SerializeField] private PlayerManager playerManager;
    #endregion

    public bool allowMove = true;
    
    [Header("Speed")]
    public bool changeSpeed;
    public float speed = 0f;
    public float _desiredSpeed = 0f;
    public Vector3 velocity = Vector3.zero;
    public Vector3 slopeMoveDirection = Vector3.zero;
    public Vector3 moveSmoothen = Vector3.zero;
    public float x = 0f, y = 0f;
    public bool isMoving = false;
    public bool isWalking = false;
    public bool isSprinting = false;
    // A method where we send booleans to define how fast the controller should move
    public float desiredSpeed(bool condition)
    {
        if (condition = isCrouching) return _desiredSpeed = moveData.crouchSpeed;
        else if (condition = isSprinting) return _desiredSpeed = moveData.sprintSpeed;
        else return _desiredSpeed = moveData.walkSpeed;
    }
    public float _groundSmoothen;
    // A method where PlayerMovement send booleans to define how smooth or slipperly the floor is
    public float groundSmoothen(bool condition)
    {
        if (condition = isCrouching) return _groundSmoothen = moveData.crouchDamp;
        else if (condition = isSprinting) return _groundSmoothen = moveData.sprintDamp;
        else return _groundSmoothen = moveData.walkDamp;
    }

    [Header("Crouching and Sliding")]
    public bool attemptingCrouch = false;
    public bool alreadySliding = false;
    public bool isSliding = true;
    public bool isCrouching = false;
    public bool toggleCrouch;
    public bool stopSlide = true;

    [Header("Jumping")]
    public bool useGravity = true;
    public bool jumped = false;
    public bool attemptingJump = false;
    
    [Header("Ground Detection")]
    public bool onSlope = false;
    public bool Grounded = false;
    public bool ccGrounded = false;
    public float groundForce = 10f;
    public float CoyoteTime;
    /**
    Only true if CoyoteTime has not exceeded the limit. Make sure CoyoteTimeMax only has a small value
    because this isn't for double-jumping.
    */
    public bool coyoteGrounded()
    {
        return CoyoteTime < moveData.CoyoteTimeMax;
    }
    public float fallSpeed = 0f;

    #region Initialization and Deactivation Logic
    bool playerInputInit = false;
    // Edge-triggered inputs, these are the stuff you definitely don't want to run every frame.
    private IEnumerator Start() // Initialize edge-triggered inputs only after PlayerInput's Instance is defined.
    {
        yield return new WaitForEndOfFrame();
        ToggleInputs(true);
        playerInputInit = true;
    }
    
    private void OnEnable() // Enable edge-triggered inputs.
    {
        if (!playerInputInit) return;
        ToggleInputs(true);
    }

    private void OnDisable() // Disable edge-triggered inputs.
    {
        ToggleInputs(false);
    }

    private void ToggleInputs(bool enable)
    {
        switch(enable)
        {
            case true:
                PlayerInput.Instance.jumpAction.performed += JumpInput;
                PlayerInput.Instance.sprintAction.performed += SprintInput;
                PlayerInput.Instance.sprintAction.canceled += WalkInput;
                PlayerInput.Instance.crouchAction.performed += CrouchInput;
                PlayerInput.Instance.crouchAction.canceled += StandInput;
            break;
            case false:
                PlayerInput.Instance.jumpAction.performed -= JumpInput;
                PlayerInput.Instance.sprintAction.performed -= SprintInput;
                PlayerInput.Instance.sprintAction.canceled -= WalkInput;
                PlayerInput.Instance.crouchAction.performed -= CrouchInput;
                PlayerInput.Instance.crouchAction.canceled -= StandInput;
            break;
        }
    }
    #endregion

    #region Update Methods
    private void Update() // Self explanatory, come on man
    {
        Inputs();
        ApplyGravity();
        GroundChecker();
        ChangeSpeed();
        Move();
    }
    #endregion

    #region Inputs
    Vector2 moveInput;
    private void Inputs() // Keyboard / WASD inputs
    {
        attemptingJump = PlayerInput.Instance.jump;
        x = PlayerInput.Instance.move.x;
        y = PlayerInput.Instance.move.y;
        moveInput = new Vector2(x, y);
        if (moveInput.sqrMagnitude > 1)
        {
            moveInput.Normalize();
        }
    }
    #endregion

    #region Gravity and Detection Logic
    /**
    Built-in CharacterController component is not a Rigidbody! Define gravity by yourself using this method.
    */
    private void ApplyGravity()
    {
        if (useGravity) velocity.y -= Physics.gravity.y * -2f * Time.deltaTime;

        playerManager.controller.Move(new Vector3(0f, velocity.y, 0f) * Time.deltaTime);

        /**
        fallSpeed defines how much  * o o m p h *  the camera headbobbing should have upon landing. It should only be
        tracked when mid-air for bug-fixing. (It doesn't work well when it tracks even when grounded)
        */

		if (!playerManager.controller.isGrounded) fallSpeed = playerManager.controller.velocity.y;
    }
    private bool fell;
    /**
    Checks if the following conditions are met while SprintInput() is being called.
    (Check the GroundChecker() method.)
    */
    Coroutine tagCoroutine;
    public bool Fell
    {
        get { return fell; }
        set
        {
            if (fell == value) return;

            fell = value;
            if (fell)
            {
                jumped = false;
                if (CoyoteTime > moveData.FallTimeMax)
                {
                    playerManager.playerAudio.doLandSfx = true;
                    playerManager.playerAudio.DoSFX(playerManager.playerAudio.doLandSfx);
                    if(moveData.tagMovement) StartTag();
                }
                if (CoyoteTime > moveData.CoyoteTimeMax && !playerManager.cameraManager.reduceMotion)
                {
                    playerManager.cameraManager.LandBob(new Vector3(0f, fallSpeed, 0f));
                }
                CoyoteTime = 0;
            }
        }
    }

    public void StartTag() // Slow down movement speed on land, can also be used for when player is shot
    {
        if (tagCoroutine != null) StopCoroutine(TagMovement());
        tagCoroutine = StartCoroutine(TagMovement());
    }

    private IEnumerator TagMovement()
    {
        changeSpeed = false;
        speed = moveData.tagSpeed;
        yield return new WaitForSeconds(moveData.tagDelay);
        if(coyoteGrounded())
        {
            changeSpeed = true;
        }
    }

    private void GroundChecker()
    {
        /**
        These two are for debugging only, because controller.isGrounded and coyoteGrounded() does not appear
        in inspector.
        */
        Fell = playerManager.controller.isGrounded;
        ccGrounded = playerManager.controller.isGrounded;
        Grounded = coyoteGrounded();

        // Extra sprinting logic, bug-fixing and etc
        QueueSprint = coyoteGrounded() && isCrouching && isMoving;

        // Extra crouching logic, bug-fixing and etc
        QueueCrouch = playerManager.controller.isGrounded;
        if (isCrouching && !coyoteGrounded())
        {
            StopCrouch();
        }

        // Manage coyote time. Coyote time is the extra time that the controller has to be able to jump in mid-air.
        if (!playerManager.controller.isGrounded)
        {
            CoyoteTime += Time.deltaTime;
        }
    }
    #endregion
    
    #region Movement Logic
    private void ChangeSpeed()
    {
        var speedCondition = isCrouching && isSprinting;

        // Check PlayerMoveData for these two.
        if(changeSpeed)
        {
            speed = Mathf.Lerp(speed, _desiredSpeed, moveData.acceleration * Time.deltaTime);
            groundSmoothen(speedCondition);
            desiredSpeed(speedCondition);
        }

        isWalking = !isSprinting && !isCrouching;
    }

    Vector3 moveDir;

    #region Slope Movement
    private RaycastHit slopeHit;
    private Vector3 normalVector;
    private bool IsFloor(Vector3 floor) {
        float angle = Vector3.Angle(Vector3.up, floor);
        return angle < Mathf.Abs(playerManager.controller.slopeLimit);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!coyoteGrounded()) return;
        if (IsFloor(hit.normal))
        {
            normalVector = normalVector != hit.normal ? hit.normal : normalVector;
		    onSlope = (Vector3.Angle(Vector3.up, hit.normal) > 1f);
        }
    }
    #endregion
    private void Move()
    {
        // Input Vector3 multiplied with the character's transform directions.
        moveDir = (playerManager.orientation.forward * moveInput.y + playerManager.orientation.right * moveInput.x).normalized;

        // Checks if controller is moving, this makes sure that sprinting is bug-free
        isMoving = (new Vector3(velocity.x, 0f, velocity.z) != Vector3.zero);

        // antiBump makes sure that slope movement is not bumpy and that the controller stays in place on the y-axis when grounded.
        if (velocity.y < 0 && playerManager.controller.isGrounded) velocity.y = -groundForce;

        // Probably makes it so the controller can't do some quasi-vaulting, might remove later because I barely notice any difference
        playerManager.controller.stepOffset = coyoteGrounded() ? 0.3f : 0f;
        // moveSmoothen is responsible for creating some sort of inertia and overall makes movement smoother.
        moveSmoothen = Vector3.MoveTowards(moveSmoothen, moveDir, (playerManager.controller.isGrounded ? _groundSmoothen : moveData.airSmoothen) * Time.deltaTime);

        var dirVector = isSliding ? slideDir : moveSmoothen;
        slopeMoveDirection = Vector3.ProjectOnPlane((Vector3)dirVector, normalVector);

        playerManager.controller.Move(new Vector3(dirVector.x, !jumped ? slopeMoveDirection.y : dirVector.y, dirVector.z) * (isSliding ? moveData.slideSpeed : speed) * Time.deltaTime);

        // Will be useful for PlayerAudio, remove if you do not need PlayerAudio.
        velocity.x = playerManager.controller.velocity.x;
        velocity.z = playerManager.controller.velocity.z;
    }
    #endregion

    /**
        QUEUEING - A feature I implemented for sprinting and crouching because as far as I know event-based input (22-51)
        does not actually track whether you're pressing a key or not.
    */

    #region Sprint Logic
    #region Sprint Input Logic
    // These two handles the input for sprinting, they're separated from the actual sprinting code to make queing possible.
    private void SprintInput(InputAction.CallbackContext ctx)
    {
        queueSprint = true;
        StartSprint();
    }
    private void WalkInput(InputAction.CallbackContext ctx)
    {
        queueSprint = false;
        StopSprint();
    }
    private bool queueSprint;
    /**
    Checks if the following conditions are met while SprintInput() is being called.
    (Check the GroundChecker() method.)
    */
    public bool QueueSprint
    {
        get { return coyoteGrounded() && isCrouching && isMoving; }
        set
        {
            var sprintCondition = (coyoteGrounded() && !isCrouching && isMoving);
            if (isSprinting) return;
            value = sprintCondition;
            if (value == sprintCondition && queueSprint)
            {
                StartSprint();
            }
        }
    }
    #endregion
    
    // These two are responsible for the actual sprinting feature
    private void StartSprint()
    {
        if (coyoteGrounded() && !isCrouching && isMoving) isSprinting = true;
    }
    private void StopSprint()
    {
        isSprinting = false;
    }
    #endregion

    #region Jump Logic
    // Mmm yes... jump
    private void JumpInput(InputAction.CallbackContext ctx)
    {
        if(!jumped) Jump();
    }
    private void Jump()
    {
        if (!coyoteGrounded()) return;
        jumped = true;
        if(isSliding)
        {
            StopSlide();
        }
        /**
        These three lines of code calls for the playerAudio script to play jump sound effects.
        Remove it if you do not need it.
        */
        playerManager.playerAudio.source.pitch = 1;
        playerManager.playerAudio.source.clip = playerManager.playerAudio.jumpSound;
        playerManager.playerAudio.source.PlayOneShot(playerManager.playerAudio.source.clip);

        velocity.y = Mathf.Sqrt(moveData.jumpForce * -3.0f * Physics.gravity.y);
    }
    #endregion

    #region  Crouch Logic
    #region Crouch Input Logic
    // These two handles the input for crouching, they're separated from the actual crouching code to make queing possible.
    private void CrouchInput(InputAction.CallbackContext ctx)
    {
        switch(toggleCrouch)
        {
            case true:
                queueCrouch = !queueCrouch;
            break;
            case false:
                queueCrouch = true;
            break;
        }
        StartCrouch();
    }
    private void StandInput(InputAction.CallbackContext ctx)
    {
        if(toggleCrouch) return;
        queueCrouch = false;
        StopCrouch();
    }
    private bool queueCrouch;
    /**
    Checks if the following conditions are met while CrouchInput() is being called.
    (Check the GroundChecker() method.)
    */
    public bool QueueCrouch
    {
        get { return coyoteGrounded(); }
        set
        {
            if (isCrouching) return;
            value = coyoteGrounded();
            if (value == true && queueCrouch)
            {
                StartCrouch();
            }
        }
    }
    #endregion

    Vector3 slideDir;
    float test;
    // These two are responsible for the actual crouching feature
    Coroutine crouchRoutine, slideCoroutine;
    private void StartCrouch()
    {
        if (!coyoteGrounded()) return; // Only crouch when grounded
        switch(toggleCrouch)
        {
            case true:
                isCrouching = !isCrouching;
            break;
            case false:
                isCrouching = true;
            break;
        }

        if (isSprinting && isCrouching)
        {
            StopSprint();
            
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            if (!alreadySliding && moveData.canSlide) slideCoroutine = StartCoroutine(Slide());
        }

        if(!isCrouching && isSliding)
        {
            StopSlide();
        }

        if (crouchRoutine != null) StopCoroutine(crouchRoutine); // Bug-fix for the forever looping AdjustHeight() Coroutine bug.
        
        // Picks between crouchHeight and standHeight because StopCrouch() only works when toggleCrouch is disabled.
        crouchRoutine = StartCoroutine(AdjustHeight(isCrouching ? moveData.crouchHeight : moveData.standHeight));
    }
    private void StopCrouch()
    {
        isCrouching = false;
        if(isSliding)
        {
            StopSlide();
        }
        if (crouchRoutine != null) StopCoroutine(crouchRoutine); // Bug-fix for the forever looping AdjustHeight() Coroutine bug.
        crouchRoutine = StartCoroutine(AdjustHeight(moveData.standHeight));
    }
    private IEnumerator Slide()
    {
        alreadySliding = true;
        stopSlide = false;
        isSliding = true;
        
        playerManager.playerAudio.source.pitch = 1;
        playerManager.playerAudio.source.clip = playerManager.playerAudio.slideSound;
        playerManager.playerAudio.source.PlayOneShot(playerManager.playerAudio.source.clip);

        CancelInvoke(nameof(StopSlide));
        Invoke(nameof(StopSlide), moveData.slideDuration * Time.deltaTime);
        slideDir = moveDir;
        while(!stopSlide)
        {
            slideDir = Vector3.Lerp(slideDir, Vector3.zero, moveData.slideDeacceleration * Time.deltaTime);
            yield return null;
        }
    }
    private void StopSlide()
    {
        stopSlide = true;
        isSliding = false;
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        CancelInvoke(nameof(ResetSlide));
        Invoke(nameof(ResetSlide), moveData.slideCooldown * Time.deltaTime);
    }
    private void ResetSlide()
    {
        alreadySliding = false;
    }
    private IEnumerator AdjustHeight(float height) // Shortens and elongates the collider depending on the provided float value.
    {
        while(playerManager.controller.height != height)
        {
            playerManager.controller.height = Mathf.Lerp(playerManager.controller.height, height, moveData.heightLerp * Time.deltaTime);
            playerManager.controller.center = Vector3.Lerp(playerManager.controller.center, new Vector3(0, height * 0.5f, 0), moveData.heightLerp * Time.deltaTime);
            playerManager.orientation.transform.localPosition = playerManager.controller.center;
            yield return null;
        }
    }
    #endregion
}