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
    // Camera Rotation
    public float mouseSensitivity = 2f;
    private float verticalRotation = 0f;
    private Transform cameraTransform;
    
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
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = currentMovementMode == MovementMode.Ground;
        cameraTransform = Camera.main.transform;

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

    void MoveGround()
    {
        Vector3 movement = (transform.right * moveHorizontal + transform.forward * moveForward).normalized;
        Vector3 targetVelocity = movement * MoveSpeed;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = targetVelocity.x;
        velocity.z = targetVelocity.z;
        rb.linearVelocity = velocity;

        // If we aren't moving and are on the ground, stop velocity so we don't slide
        if (isGrounded && moveHorizontal == 0 && moveForward == 0)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void MoveFlying()
    {
        float ascend = Input.GetButton("Jump") ? flyVerticalSpeed : 0f;
        float descend = Input.GetKey(KeyCode.LeftControl) ? -flyVerticalSpeed : 0f;

        Vector3 horizontalVelocity = (transform.right * moveHorizontal + transform.forward * moveForward).normalized * flySpeed;
        Vector3 verticalVelocity = Vector3.up * (ascend + descend);

        rb.linearVelocity = horizontalVelocity + verticalVelocity;
    }

    void RotateCamera()
    {
        float horizontalRotation = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0, horizontalRotation, 0);

        verticalRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    void Jump()
    {
        isGrounded = false;
        groundCheckTimer = groundCheckDelay;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z); // Initial burst for the jump
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