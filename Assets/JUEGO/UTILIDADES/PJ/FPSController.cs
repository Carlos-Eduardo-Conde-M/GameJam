using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6.0f;
    [SerializeField] private float crouchSpeed = 2.0f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 15f;

    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float controllerRadius = 0.3f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float standingCameraHeight = 0.9f;
    [SerializeField] private float crouchCameraHeight = 0.4f;

    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float lookXLimit = 85f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float cameraSmoothing = 5f;

    [Header("Head Bob Settings")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.08f;
    [SerializeField] private float crouchBobSpeed = 10f;
    [SerializeField] private float crouchBobAmount = 0.03f;

    [Header("FOV Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float normalFOV = 75f;
    [SerializeField] private float sprintFOV = 85f;
    [SerializeField] private float fovTransitionSpeed = 8f;

    [Header("Footstep Settings")]
    [SerializeField] private bool enableFootsteps = true;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] walkFootsteps;
    [SerializeField] private AudioClip[] sprintFootsteps;
    [SerializeField] private AudioClip[] crouchFootsteps;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float sprintStepInterval = 0.3f;
    [SerializeField] private float crouchStepInterval = 0.7f;

    // Private variables
    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 currentVelocity = Vector3.zero;
    private float rotationX = 0f;
    private float targetRotationX = 0f;
    private bool isCrouching = false;
    private float currentHeight;
    private float targetCameraHeight;
    private float currentCameraHeight;
    private Vector3 initialCameraPosition;
    private float headBobTimer = 0f;
    private float footstepTimer = 0f;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize heights - usar los valores actuales del CharacterController si no están en 0
        if (standingHeight == 2f && controller.height != 2f)
        {
            standingHeight = controller.height;
        }

        currentHeight = standingHeight;
        controller.height = currentHeight;
        controller.center = new Vector3(0, currentHeight / 2, 0);
        controller.radius = controllerRadius; // Establecer el radio desde el script

        // Setup camera
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (playerCamera == null && cameraTransform != null)
        {
            playerCamera = cameraTransform.GetComponent<Camera>();
        }

        if (cameraTransform != null)
        {
            initialCameraPosition = cameraTransform.localPosition;
            currentCameraHeight = standingCameraHeight;
            targetCameraHeight = standingCameraHeight;

            // Posicionar cámara correctamente desde el inicio
            cameraTransform.localPosition = initialCameraPosition + new Vector3(0, currentCameraHeight, 0);
        }

        // Setup audio source
        if (audioSource == null && enableFootsteps)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.playOnAwake = false;
        }

        if (playerCamera != null)
        {
            normalFOV = playerCamera.fieldOfView;
        }
    }

    void Update()
    {
        // Check if grounded
        isGrounded = controller.isGrounded;

        // Handle crouch
        HandleCrouch();

        // Handle movement
        HandleMovement();

        // Handle camera rotation
        HandleCameraRotation();

        // Handle head bob
        if (enableHeadBob)
        {
            HandleHeadBob();
        }

        // Handle FOV
        HandleFOV();

        // Handle footsteps
        if (enableFootsteps)
        {
            HandleFootsteps();
        }

        // Unlock cursor with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Lock cursor again on click
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void HandleCrouch()
    {
        // Toggle crouch
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
        }

        // Smooth height transition
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        controller.height = currentHeight;

        // Adjust center
        controller.center = new Vector3(0, currentHeight / 2, 0);

        // Smooth camera height transition
        targetCameraHeight = isCrouching ? crouchCameraHeight : standingCameraHeight;
        currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraHeight, Time.deltaTime * crouchTransitionSpeed);
    }

    void HandleMovement()
    {
        // Get input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 inputDirection = new Vector3(moveX, 0, moveZ).normalized;

        // Determine speed
        float currentSpeed = walkSpeed;
        bool isSprinting = false;

        if (Input.GetKey(KeyCode.LeftShift) && !isCrouching && moveZ > 0)
        {
            currentSpeed = sprintSpeed;
            isSprinting = true;
        }
        else if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }

        // Calculate movement direction relative to camera
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        Vector3 targetVelocity = (forward * moveZ + right * moveX) * currentSpeed;

        // Smooth acceleration/deceleration
        float accelRate = inputDirection.magnitude > 0 ? acceleration : deceleration;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, accelRate * Time.deltaTime);

        // Apply movement
        moveDirection.x = currentVelocity.x;
        moveDirection.z = currentVelocity.z;

        // Handle jumping
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            moveDirection.y = jumpForce;
        }

        // Apply gravity
        if (!isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
        else if (moveDirection.y < 0)
        {
            moveDirection.y = -2f; // Small downward force to keep grounded
        }

        // Move the controller
        controller.Move(moveDirection * Time.deltaTime);
    }

    void HandleCameraRotation()
    {
        if (cameraTransform == null) return;

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (invertY) mouseY = -mouseY;

        // Rotate player body (Y axis)
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera (X axis) with smoothing
        targetRotationX -= mouseY;
        targetRotationX = Mathf.Clamp(targetRotationX, -lookXLimit, lookXLimit);
        rotationX = Mathf.Lerp(rotationX, targetRotationX, Time.deltaTime * cameraSmoothing);

        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }

    void HandleHeadBob()
    {
        if (cameraTransform == null) return;

        // Only bob when moving and grounded
        bool isMoving = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;

        if (isMoving && isGrounded)
        {
            float bobSpeed = walkBobSpeed;
            float bobAmount = walkBobAmount;

            if (Input.GetKey(KeyCode.LeftShift) && !isCrouching && Input.GetAxis("Vertical") > 0)
            {
                bobSpeed = sprintBobSpeed;
                bobAmount = sprintBobAmount;
            }
            else if (isCrouching)
            {
                bobSpeed = crouchBobSpeed;
                bobAmount = crouchBobAmount;
            }

            headBobTimer += Time.deltaTime * bobSpeed;

            float bobOffsetY = Mathf.Sin(headBobTimer) * bobAmount;
            float bobOffsetX = Mathf.Cos(headBobTimer * 0.5f) * bobAmount * 0.5f;

            Vector3 targetPosition = initialCameraPosition + new Vector3(bobOffsetX, currentCameraHeight + bobOffsetY, 0);
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetPosition, Time.deltaTime * 10f);
        }
        else
        {
            headBobTimer = 0f;
            Vector3 targetPosition = initialCameraPosition + new Vector3(0, currentCameraHeight, 0);
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetPosition, Time.deltaTime * 10f);
        }
    }

    void HandleFOV()
    {
        if (playerCamera == null) return;

        float targetFOV = normalFOV;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching && Input.GetAxis("Vertical") > 0 && isGrounded;

        if (isSprinting)
        {
            targetFOV = sprintFOV;
        }

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);
    }

    void HandleFootsteps()
    {
        if (audioSource == null) return;

        bool isMoving = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;

        if (isMoving && isGrounded)
        {
            footstepTimer += Time.deltaTime;

            float stepInterval = walkStepInterval;
            AudioClip[] footstepArray = walkFootsteps;

            if (Input.GetKey(KeyCode.LeftShift) && !isCrouching && Input.GetAxis("Vertical") > 0)
            {
                stepInterval = sprintStepInterval;
                footstepArray = sprintFootsteps.Length > 0 ? sprintFootsteps : walkFootsteps;
            }
            else if (isCrouching)
            {
                stepInterval = crouchStepInterval;
                footstepArray = crouchFootsteps.Length > 0 ? crouchFootsteps : walkFootsteps;
            }

            if (footstepTimer >= stepInterval && footstepArray.Length > 0)
            {
                AudioClip clip = footstepArray[Random.Range(0, footstepArray.Length)];
                audioSource.PlayOneShot(clip);
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    // Public method to set mouse sensitivity (useful for settings menu)
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }

    // Public method to toggle head bob
    public void ToggleHeadBob(bool enabled)
    {
        enableHeadBob = enabled;
    }
}