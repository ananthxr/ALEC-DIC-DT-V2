using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Setup")]
    [SerializeField] private Transform defaultCameraPosition;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Camera controlledCamera;

    [Header("Navigation")]
    [SerializeField] private UnityEngine.UI.Button backNavigationButton;

    [Header("Camera Mode")]
    [SerializeField] private bool useFreeExplorationMode = true; // True = temporary orbit + panning, False = fixed orbit + no panning

    [Header("Controls - Smooth & Responsive")]
    [SerializeField] private float panSpeed = 2f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float zoomSpeed = 5f;

    [Header("Smoothing")]
    [SerializeField] private float moveSmooth = 8f;
    [SerializeField] private float rotateSmooth = 8f;

    [Header("Limits")]
    [SerializeField] private float minZoomDistance = 6f;
    [SerializeField] private float maxZoomDistance = 20f;

    [Header("Pan Limits")]
    [SerializeField] private float panLimitMinX = -9f;     // Left boundary
    [SerializeField] private float panLimitMaxX = 9f;      // Right boundary
    [SerializeField] private float panLimitMinZ = -20.25f; // Back boundary
    [SerializeField] private float panLimitMaxZ = 5f;      // Front boundary

    // Smooth movement state
    private bool isPanning = false;
    private bool isRotating = false;
    private Vector3 lastMousePosition;

    // Target values for smooth interpolation
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    // Track last camera position
    private Vector3 lastCameraPosition;
    private Quaternion lastCameraRotation;

    void Start()
    {
        InitializeCamera();
    }

    void Update()
    {
        HandleInput();

        // Smooth interpolation to target values
        Vector3 currentPos = controlledCamera.transform.position;
        Quaternion currentRot = controlledCamera.transform.rotation;

        float posDistance = Vector3.Distance(currentPos, targetPosition);
        float rotDistance = Quaternion.Angle(currentRot, targetRotation);

        // Only interpolate if there's a meaningful difference
        if (posDistance > 0.001f || rotDistance > 0.01f)
        {
            controlledCamera.transform.position = Vector3.Lerp(
                currentPos, targetPosition, Time.deltaTime * moveSmooth);

            controlledCamera.transform.rotation = Quaternion.Slerp(
                currentRot, targetRotation, Time.deltaTime * rotateSmooth);
        }

        // Update tracking values
        lastCameraPosition = controlledCamera.transform.position;
        lastCameraRotation = controlledCamera.transform.rotation;
    }

    private void InitializeCamera()
    {
        if (controlledCamera == null)
            controlledCamera = Camera.main;

        if (defaultCameraPosition != null)
        {
            controlledCamera.transform.position = defaultCameraPosition.position;
            controlledCamera.transform.rotation = defaultCameraPosition.rotation;

            // Initialize target values to current position
            targetPosition = controlledCamera.transform.position;
            targetRotation = controlledCamera.transform.rotation;

            // Initialize tracking values
            lastCameraPosition = controlledCamera.transform.position;
            lastCameraRotation = controlledCamera.transform.rotation;

            Debug.Log($"[CameraController] Pan limits set: X({panLimitMinX} to {panLimitMaxX}), Z({panLimitMinZ} to {panLimitMaxZ})");
        }

        SetupBackNavigationButton();
        Debug.Log("[CameraController] Camera controller initialized");
    }

    private void SetupBackNavigationButton()
    {
        if (backNavigationButton != null)
        {
            backNavigationButton.onClick.AddListener(OnBackButtonClicked);
            backNavigationButton.gameObject.SetActive(true);
            Debug.Log("[CameraController] Back navigation button setup complete");
        }
    }

    private void OnBackButtonClicked()
    {
        Debug.Log("[CameraController] Back button clicked - resetting camera to default");
        ForceResetCameraStates();
        ResetToDefault();
    }

    private void ForceResetCameraStates()
    {
        isPanning = false;
        isRotating = false;
        Debug.Log("[CameraController] Camera states force reset");
    }

    private void HandleInput()
    {
        // Check if mouse is over UI - if so, don't do camera controls
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            // Force reset camera states when over UI
            isPanning = false;
            isRotating = false;
            return;
        }

        // Force reset if no mouse buttons are actually held down
        if (!Input.GetMouseButton(0))
        {
            isPanning = false;
        }
        if (!Input.GetMouseButton(1))
        {
            isRotating = false;
        }

        // Left mouse - Panning
        if (Input.GetMouseButtonDown(0))
        {
            isPanning = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isPanning = false;
        }

        // Right mouse - Rotation
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }

        // Handle panning (only in free exploration mode)
        if (isPanning && useFreeExplorationMode)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            if (mouseDelta.magnitude > 0.1f) // Tiny threshold to prevent micro-jitter
            {
                // Transform mouse movement to world space
                Vector3 worldDelta = controlledCamera.transform.TransformDirection(-mouseDelta.x, 0, -mouseDelta.y);
                worldDelta.y = 0; // Keep on ground plane
                worldDelta *= panSpeed * 0.01f;

                Vector3 newTargetPosition = targetPosition + worldDelta;

                // Apply pan limits to target position
                targetPosition = ApplyPanLimits(newTargetPosition);
            }

            lastMousePosition = Input.mousePosition;
        }
        else if (isPanning && !useFreeExplorationMode)
        {
            // Panning disabled in constrained mode
            Debug.Log("[CameraController] Panning disabled in constrained mode");
            lastMousePosition = Input.mousePosition;
        }

        // Handle rotation - mode-dependent behavior
        if (isRotating)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            if (mouseDelta.magnitude > 0.1f) // Tiny threshold
            {
                if (useFreeExplorationMode)
                {
                    // FREE EXPLORATION MODE: Orbit around temporary point in front of camera
                    float temporaryOrbitDistance = 10f;
                    Vector3 temporaryOrbitPoint = controlledCamera.transform.position + controlledCamera.transform.forward * temporaryOrbitDistance;

                    float horizontalRotation = mouseDelta.x * rotationSpeed * 0.01f;
                    float verticalRotation = mouseDelta.y * rotationSpeed * 0.01f;

                    // Calculate direction from temporary orbit point TO camera
                    Vector3 directionFromOrbitToCamera = (controlledCamera.transform.position - temporaryOrbitPoint).normalized;
                    float currentDistanceToOrbit = temporaryOrbitDistance;

                    // Apply horizontal rotation (around Y-axis)
                    Quaternion horizontalRot = Quaternion.AngleAxis(horizontalRotation, Vector3.up);
                    directionFromOrbitToCamera = horizontalRot * directionFromOrbitToCamera;

                    // Apply vertical rotation (around right axis)
                    Vector3 rightAxis = Vector3.Cross(Vector3.up, directionFromOrbitToCamera).normalized;
                    Quaternion verticalRot = Quaternion.AngleAxis(verticalRotation, rightAxis);
                    directionFromOrbitToCamera = verticalRot * directionFromOrbitToCamera;

                    // Calculate new camera position
                    Vector3 newCameraPosition = temporaryOrbitPoint + directionFromOrbitToCamera * currentDistanceToOrbit;

                    // Update targets
                    targetPosition = newCameraPosition;
                    targetRotation = Quaternion.LookRotation(temporaryOrbitPoint - newCameraPosition);

                    Debug.Log($"[CameraController] Free mode - Orbiting around temporary point at: {temporaryOrbitPoint}");
                }
                else
                {
                    // CONSTRAINED MODE: Orbit around fixed camera target
                    if (cameraTarget != null)
                    {
                        float horizontalRotation = mouseDelta.x * rotationSpeed * 0.01f;
                        float verticalRotation = mouseDelta.y * rotationSpeed * 0.01f;

                        Vector3 targetPoint = cameraTarget.position;

                        // Calculate direction from target to camera
                        Vector3 dirToCamera = (targetPosition - targetPoint).normalized;
                        float currentDistance = Vector3.Distance(targetPosition, targetPoint);

                        // Apply horizontal rotation (around Y-axis)
                        Quaternion horizontalRot = Quaternion.AngleAxis(horizontalRotation, Vector3.up);
                        dirToCamera = horizontalRot * dirToCamera;

                        // Apply vertical rotation (around right axis)
                        Vector3 rightAxis = Vector3.Cross(Vector3.up, dirToCamera).normalized;
                        Quaternion verticalRot = Quaternion.AngleAxis(verticalRotation, rightAxis);
                        dirToCamera = verticalRot * dirToCamera;

                        // Update target position and rotation
                        targetPosition = targetPoint + dirToCamera * currentDistance;
                        targetRotation = Quaternion.LookRotation(targetPoint - targetPosition);

                        Debug.Log($"[CameraController] Constrained mode - Orbiting around fixed target at: {targetPoint}");
                    }
                    else
                    {
                        Debug.LogWarning("[CameraController] Constrained mode requires camera target to be assigned!");
                    }
                }
            }

            lastMousePosition = Input.mousePosition;
        }

        // Handle zoom - move along camera's view direction with distance limits from orbit target
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Debug.Log($"[CameraController] Scroll detected: {scroll}");

            // Always zoom along camera's forward direction (where it's looking)
            Vector3 currentPos = controlledCamera.transform.position;
            Vector3 forwardDirection = controlledCamera.transform.forward;

            // Calculate zoom movement distance
            float zoomDistance = scroll * zoomSpeed;
            Vector3 zoomMovement = forwardDirection * zoomDistance;

            // Calculate new camera position
            Vector3 newCameraPosition = currentPos + zoomMovement;

            // Check distance limits from orbit target (if target exists)
            bool canZoom = true;
            if (cameraTarget != null)
            {
                Vector3 targetPoint = cameraTarget.position;
                float currentDistance = Vector3.Distance(currentPos, targetPoint);
                float newDistance = Vector3.Distance(newCameraPosition, targetPoint);

                Debug.Log($"[CameraController] Current distance: {currentDistance:F2}, New distance: {newDistance:F2}, Limits: {minZoomDistance}-{maxZoomDistance}");

                // Enforce distance limits from orbit target
                if (newDistance < minZoomDistance || newDistance > maxZoomDistance)
                {
                    canZoom = false;
                    Debug.Log($"[CameraController] Zoom blocked: Distance {newDistance:F2} outside limits");
                }
            }
            else
            {
                // Without target, only constrain Y axis (prevent going underground or too high)
                newCameraPosition.y = Mathf.Clamp(newCameraPosition.y, 2f, 50f);
            }

            if (canZoom)
            {
                // Apply new position
                controlledCamera.transform.position = newCameraPosition;

                // Sync targets to prevent interpolation conflicts (rotation stays unchanged)
                targetPosition = controlledCamera.transform.position;
                targetRotation = controlledCamera.transform.rotation;
                lastCameraPosition = controlledCamera.transform.position;
                lastCameraRotation = controlledCamera.transform.rotation;

                if (cameraTarget != null)
                {
                    float finalDistance = Vector3.Distance(controlledCamera.transform.position, cameraTarget.position);
                    Debug.Log($"[CameraController] Zoom applied: Distance to target: {finalDistance:F2}");
                }
                else
                {
                    Debug.Log($"[CameraController] Free zoom applied");
                }
            }
        }
    }

    private Vector3 ApplyPanLimits(Vector3 newPosition)
    {
        // Apply X-axis limits
        newPosition.x = Mathf.Clamp(newPosition.x, panLimitMinX, panLimitMaxX);

        // Apply Z-axis limits
        newPosition.z = Mathf.Clamp(newPosition.z, panLimitMinZ, panLimitMaxZ);

        // Y axis is not constrained for panning

        return newPosition;
    }

    // Public method to reset camera to default position
    public void ResetToDefault()
    {
        if (defaultCameraPosition == null)
            return;

        controlledCamera.transform.position = defaultCameraPosition.position;
        controlledCamera.transform.rotation = defaultCameraPosition.rotation;

        targetPosition = defaultCameraPosition.position;
        targetRotation = defaultCameraPosition.rotation;

        lastCameraPosition = defaultCameraPosition.position;
        lastCameraRotation = defaultCameraPosition.rotation;

        Debug.Log("[CameraController] Camera reset to default position");
    }

    private void OnDestroy()
    {
        if (backNavigationButton != null)
        {
            backNavigationButton.onClick.RemoveListener(OnBackButtonClicked);
        }
    }

    // Public method to switch camera modes at runtime
    public void SetCameraMode(bool freeExploration)
    {
        useFreeExplorationMode = freeExploration;

        if (freeExploration)
        {
            Debug.Log("[CameraController] Switched to FREE EXPLORATION mode (temporary orbit + panning enabled)");
        }
        else
        {
            Debug.Log("[CameraController] Switched to CONSTRAINED mode (fixed orbit + no panning)");

            // Warn if no camera target is assigned
            if (cameraTarget == null)
            {
                Debug.LogWarning("[CameraController] Camera target not assigned - constrained mode may not work properly!");
            }
        }
    }

    // Public getter for current mode
    public bool IsFreeExplorationMode()
    {
        return useFreeExplorationMode;
    }

    private void OnDrawGizmos()
    {
        // Draw pan limits as a box in the scene view
        Gizmos.color = Color.yellow;

        // Calculate the center and size of the pan limit box
        Vector3 center = new Vector3(
            (panLimitMinX + panLimitMaxX) / 2f,
            0f,
            (panLimitMinZ + panLimitMaxZ) / 2f
        );

        Vector3 size = new Vector3(
            panLimitMaxX - panLimitMinX,
            0.1f, // Small height just for visualization
            panLimitMaxZ - panLimitMinZ
        );

        // Draw wireframe box
        Gizmos.DrawWireCube(center, size);

        // Draw corner posts for better visibility
        float postHeight = 10f;
        Vector3[] corners = new Vector3[]
        {
            new Vector3(panLimitMinX, 0, panLimitMinZ),
            new Vector3(panLimitMaxX, 0, panLimitMinZ),
            new Vector3(panLimitMaxX, 0, panLimitMaxZ),
            new Vector3(panLimitMinX, 0, panLimitMaxZ)
        };

        Gizmos.color = Color.red;
        foreach (Vector3 corner in corners)
        {
            Gizmos.DrawLine(corner, corner + Vector3.up * postHeight);
        }

        // Draw labels on the edges
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(panLimitMinX, 0.1f, panLimitMinZ), new Vector3(panLimitMaxX, 0.1f, panLimitMinZ));
        Gizmos.DrawLine(new Vector3(panLimitMaxX, 0.1f, panLimitMinZ), new Vector3(panLimitMaxX, 0.1f, panLimitMaxZ));
        Gizmos.DrawLine(new Vector3(panLimitMaxX, 0.1f, panLimitMaxZ), new Vector3(panLimitMinX, 0.1f, panLimitMaxZ));
        Gizmos.DrawLine(new Vector3(panLimitMinX, 0.1f, panLimitMaxZ), new Vector3(panLimitMinX, 0.1f, panLimitMinZ));
    }
}
