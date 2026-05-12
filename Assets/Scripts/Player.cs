using UnityEngine;

/*
    This script provides jumping and movement in Unity 3D - Gatsby
*/

public enum MovementMode
{
    Ground,
    Flying
}

public class Player : MonoBehaviour
{

public Animator anim;

    // Camera Rotation
    public float mouseSensitivity = 4f;
    private float verticalRotation = 20f;   // start angle (degrees down toward player)
    private float horizontalRotation = 0f;
    private Transform cameraTransform;

    public float cameraDistance = 5f;       // how far behind the player
    public float cameraHeight = 2f;         // target height offset on the player
    public Vector2 verticalClamp = new Vector2(-10f, 60f); // min/max look angle
    
    // General Movement
    private Rigidbody rb;
    public float MoveSpeed = 5f;
    private float moveHorizontal;
    private float moveForward;
    public MovementMode currentMovementMode = MovementMode.Ground;
    public bool canFly = true;
    public float flySpeed = 8f;
    public float flyVerticalSpeed = 5f;

    // Jumping
    public float jumpForce = 10f;
    public float fallMultiplier = 2.5f; // Multiplies gravity when falling down
    public float ascendMultiplier = 2f; // Multiplies gravity for ascending to peak of jump
    private bool isGrounded = true;
    public LayerMask groundLayer;
    private float groundCheckTimer = 0f;
    private float groundCheckDelay = 0.3f;
    private float playerHeight;
    private float raycastDistance;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = currentMovementMode == MovementMode.Ground;
        cameraTransform = GameObject.FindWithTag("MainCamera").transform;
        // Set the raycast to be slightly beneath the player's feet
        playerHeight = GetComponent<CapsuleCollider>().height * transform.localScale.y;
        raycastDistance = (playerHeight / 2) + 0.2f;

        // Hides the mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        moveHorizontal = Input.GetAxisRaw("Horizontal");
        moveForward = Input.GetAxisRaw("Vertical");

        RotateCamera();
        UpdateAnimations(); // <-- Drive all animation state here

        if (canFly && Input.GetKeyDown(KeyCode.F))
        {
            currentMovementMode = currentMovementMode == MovementMode.Flying
                ? MovementMode.Ground
                : MovementMode.Flying;

            rb.useGravity = currentMovementMode == MovementMode.Ground;
            if (currentMovementMode == MovementMode.Flying)
            {
                isGrounded = false;
                groundCheckTimer = 0f;
            }
        }

        if (Input.GetButtonDown("Jump") && currentMovementMode == MovementMode.Ground && isGrounded)
        {
            Jump();
        }

        if (currentMovementMode == MovementMode.Ground)
        {
            // Checking when we're on the ground and keeping track of our ground check delay
            if (!isGrounded && groundCheckTimer <= 0f)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
                isGrounded = Physics.Raycast(rayOrigin, Vector3.down, raycastDistance, groundLayer);
            }
            else
            {
                groundCheckTimer -= Time.deltaTime;
            }
        }
    }

    void FixedUpdate()
    {
        switch (currentMovementMode)
        {
            case MovementMode.Ground:
                MoveGround();
                ApplyJumpPhysics();
                break;
            case MovementMode.Flying:
                MoveFlying();
                break;
        }
    }

    // ─────────────────────────────────────────────
    //  ANIMATIONS
    // ─────────────────────────────────────────────
    // Animator parameters expected:
    //   bool  "isWalking"
    //   bool  "isFlying"
    //   trigger "Jump"      (set once when jump starts)
    //
    // Recommended Animator setup:
    //   Idle  ──(isWalking)──►  Walk
    //   Idle  ──(isFlying) ──►  Fly
    //   Walk  ──(!isWalking)──► Idle
    //   Any   ──[Jump trigger]► Jump  (transitions back to Idle/Walk on exit)

    void UpdateAnimations()
    {
        bool isMoving = (moveHorizontal != 0 || moveForward != 0);
        bool isFlying = currentMovementMode == MovementMode.Flying;

        anim.SetBool("isWalking", isMoving && !isFlying);
        anim.SetBool("isFlying",  isFlying);
    }

    // ─────────────────────────────────────────────

    void MoveGround()
    {
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 movement = (camRight * moveHorizontal + camForward * moveForward).normalized;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = movement.x * MoveSpeed;
        velocity.z = movement.z * MoveSpeed;
        rb.linearVelocity = velocity;

        // Rotate player to face movement direction
        if (movement.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }

        if (isGrounded && moveHorizontal == 0 && moveForward == 0)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void MoveFlying()
    {
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 horizontalVelocity = (camRight * moveHorizontal + camForward * moveForward).normalized * flySpeed;

        float ascend = Input.GetButton("Jump") ? flyVerticalSpeed : 0f;
        float descend = Input.GetKey(KeyCode.LeftControl) ? -flyVerticalSpeed : 0f;

        rb.linearVelocity = horizontalVelocity + Vector3.up * (ascend + descend);

        if (horizontalVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }

        if (isGrounded && moveHorizontal == 0 && moveForward == 0)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        horizontalRotation += mouseX;
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, verticalClamp.x, verticalClamp.y);

        // Position camera behind and above player
        Quaternion camRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
        Vector3 targetPos = transform.position + Vector3.up * cameraHeight;
        cameraTransform.position = targetPos + camRotation * new Vector3(0f, 0f, -cameraDistance);
        cameraTransform.LookAt(targetPos);
    }

    void Jump()
    {
        isGrounded = false;
        groundCheckTimer = groundCheckDelay;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);

        anim.SetTrigger("Jump"); // <-- Fire jump animation exactly once
    }

    void ApplyJumpPhysics()
    {
        if (rb.linearVelocity.y < 0) 
        {
            // Falling: Apply fall multiplier to make descent faster
            rb.linearVelocity += Vector3.up * Physics.gravity.y * fallMultiplier * Time.fixedDeltaTime;
        } // Rising
        else if (rb.linearVelocity.y > 0)
        {
            // Rising: Change multiplier to make player reach peak of jump faster
            rb.linearVelocity += Vector3.up * Physics.gravity.y * ascendMultiplier  * Time.fixedDeltaTime;
        }
    }
}