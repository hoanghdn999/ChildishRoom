using UnityEngine;

[RequireComponent(typeof(Camera))]
public class InteractiveCamera : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private Transform orbitTarget;
    [SerializeField] private float orbitDistance = 5f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minFieldOfView = 15f;

    private Camera targetCamera;
    private float defaultFieldOfView;
    private float currentHorizontalAngle;
    private float currentVerticalAngle;
    private Vector3 orbitCenter;
    private bool isDragging;
    private Vector3 lastMousePosition;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        
        if (targetCamera == null)
        {
            Debug.LogError($"InteractiveCamera on {gameObject.name} requires a Camera component.", this);
            enabled = false;
            return;
        }

        if (targetCamera.orthographic)
        {
            Debug.LogWarning($"InteractiveCamera on {gameObject.name} requires a perspective camera. Current camera is orthographic.", this);
        }

        defaultFieldOfView = targetCamera.fieldOfView;
        // Ensure minFieldOfView doesn't exceed default (can't zoom in more than allowed)
        minFieldOfView = Mathf.Min(minFieldOfView, defaultFieldOfView);
        targetCamera.fieldOfView = Mathf.Clamp(targetCamera.fieldOfView, minFieldOfView, defaultFieldOfView);
    }

    private void Start()
    {
        CalculateOrbitCenter();
        InitializeOrbitAngles();
    }

    private void Update()
    {
        HandleMouseDrag();
        HandleZoom();
    }

    private void CalculateOrbitCenter()
    {
        if (orbitTarget != null)
        {
            orbitCenter = orbitTarget.position;
        }
        else
        {
            // Calculate orbit center based on camera position and forward direction
            orbitCenter = transform.position + transform.forward * orbitDistance;
        }
    }

    private void InitializeOrbitAngles()
    {
        Vector3 direction = (transform.position - orbitCenter).normalized;
        currentHorizontalAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        currentVerticalAngle = Mathf.Asin(direction.y) * Mathf.Rad2Deg;
    }

    private void HandleMouseDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            
            currentHorizontalAngle += mouseDelta.x * rotationSpeed;
            currentVerticalAngle -= mouseDelta.y * rotationSpeed;
            currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);

            UpdateCameraPosition();
            lastMousePosition = Input.mousePosition;
        }
    }

    private void HandleZoom()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        
        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            float newFieldOfView = targetCamera.fieldOfView - scrollDelta * zoomSpeed;
            targetCamera.fieldOfView = Mathf.Clamp(newFieldOfView, minFieldOfView, defaultFieldOfView);
        }
    }

    private void UpdateCameraPosition()
    {
        // Update orbit center if target is set (in case target moves)
        if (orbitTarget != null)
        {
            orbitCenter = orbitTarget.position;
        }
        
        float horizontalRad = currentHorizontalAngle * Mathf.Deg2Rad;
        float verticalRad = currentVerticalAngle * Mathf.Deg2Rad;

        float cosVertical = Mathf.Cos(verticalRad);
        Vector3 direction = new Vector3(
            Mathf.Sin(horizontalRad) * cosVertical,
            Mathf.Sin(verticalRad),
            Mathf.Cos(horizontalRad) * cosVertical
        );

        transform.position = orbitCenter + direction * orbitDistance;
        transform.LookAt(orbitCenter);
    }

    private void OnValidate()
    {
        if (Application.isPlaying && targetCamera != null)
        {
            float maxFoV = defaultFieldOfView > 0 ? defaultFieldOfView : targetCamera.fieldOfView;
            minFieldOfView = Mathf.Min(minFieldOfView, maxFoV);
            targetCamera.fieldOfView = Mathf.Clamp(targetCamera.fieldOfView, minFieldOfView, maxFoV);
        }
    }
}
