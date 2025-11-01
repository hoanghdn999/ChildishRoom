using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class InteractiveCamera : MonoBehaviour
{
    public static InteractiveCamera Instance;
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

    // Zoom to object state
    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private bool isZooming = false;
    private bool isZoomedIn = false;
    private System.Action onReturnCallback;

    public static bool IsNavigatingObject { get; private set; } = false;
    public static System.Action onHitESCKey;

    private void Awake()
    {
        Instance = this;
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
    public static bool IsPointerOverUI()
    {
        return IsNavigatingObject;
    }

    private void Update()
    {
        HandleMouseDrag();
        HandleZoom();
        HandleEscapeKey();
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
        //Debug.Log("HandleMouseDrag");
        if (IsPointerOverUI())
        {
            return;
        }
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

            currentHorizontalAngle += (mouseDelta.x * rotationSpeed);
            currentVerticalAngle -= mouseDelta.y * rotationSpeed;
            currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);

            UpdateCameraPosition();
            lastMousePosition = Input.mousePosition;
        }
    }

    private void HandleZoom()
    {
        //Debug.Log("HandleZoom");
        float scrollDelta = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            float newFieldOfView = targetCamera.fieldOfView - scrollDelta * zoomSpeed;
            targetCamera.fieldOfView = Mathf.Clamp(newFieldOfView, minFieldOfView, defaultFieldOfView);
        }
    }

    private void UpdateCameraPosition()
    {
        if (IsPointerOverUI())
        {
            return;
        }
        Debug.Log("UpdateCameraPosition");
        // Update orbit center if target is set (in case target moves)
        if (orbitTarget != null)
        {
            orbitCenter = orbitTarget.position;
        }

        float horizontalRad = (currentHorizontalAngle * Mathf.Deg2Rad);
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

    public bool IsZooming => isZooming;
    public bool IsZoomedIn => isZoomedIn;

    private void HandleEscapeKey()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isZoomedIn && !isZooming)
        {
            if (onReturnCallback != null)
            {
                onReturnCallback();
            }
            onHitESCKey?.Invoke();
        }
    }

    public Coroutine SmoothZoomToObject(Bounds objectBounds, float zoomSpeed, float zoomDistance, float zoomHeightOffset, Vector3 objectViewOffset, System.Action onComplete = null, System.Action onReturn = null)
    {
        if (isZooming)
        {
            return null;
        }

        originalCameraPos = targetCamera.transform.position;
        originalCameraRot = targetCamera.transform.rotation;
        onReturnCallback = onReturn;

        return StartCoroutine(SmoothZoomToObjectCoroutine(objectBounds, zoomSpeed, zoomDistance, zoomHeightOffset, objectViewOffset, onComplete));
    }

    public Coroutine SmoothReturnToOriginal(float zoomSpeed, System.Action onComplete = null)
    {
        if (isZooming)
        {
            return null;
        }

        return StartCoroutine(SmoothReturnToOriginalCoroutine(zoomSpeed, onComplete));
    }

    private IEnumerator SmoothZoomToObjectCoroutine(Bounds objectBounds, float zoomSpeed, float zoomDistance, float zoomHeightOffset, Vector3 objectViewOffset, System.Action onComplete)
    {
        Debug.Log("SmoothZoom");
        isZooming = true;

        Vector3 startPos = targetCamera.transform.position;
        Quaternion startRot = targetCamera.transform.rotation;

        // Calculate object's center using renderer bounds (accounts for object size and position)
        Vector3 objectCenter = objectBounds.center;

        // Calculate direction from object center to current camera position
        // This maintains the viewing angle relative to each object
        Vector3 dir = (targetCamera.transform.position - objectCenter).normalized;

        // Calculate target position: position camera at zoomDistance from object center
        // Camera moves closer to object along the same viewing direction
        Vector3 targetPosition = objectCenter + dir * zoomDistance;

        // Add height offset
        targetPosition += Vector3.up * zoomHeightOffset;

        // Calculate initial look direction from camera to object center
        Vector3 lookDirection = (objectCenter - targetPosition).normalized;

        // Calculate camera's local axes relative to the look direction
        // Use world up as reference for right vector calculation
        Vector3 worldUp = Vector3.up;
        Vector3 cameraRight = Vector3.Cross(worldUp, lookDirection).normalized;

        // If camera is looking straight up/down, use camera's current right vector as fallback
        if (cameraRight.magnitude < 0.1f)
        {
            cameraRight = targetCamera.transform.right;
        }

        Vector3 cameraUp = Vector3.Cross(lookDirection, cameraRight).normalized;

        // Apply view offset (relative to camera's right/up/forward)
        // X = right/left, Y = up/down, Z = forward/back (usually keep at 0)
        Vector3 offsetWorld = cameraRight * objectViewOffset.x + cameraUp * objectViewOffset.y + lookDirection * objectViewOffset.z;
        Vector3 targetLookAt = objectCenter + offsetWorld;

        // Make camera look at the offset position
        Quaternion targetRotation = Quaternion.LookRotation(targetLookAt - targetPosition);

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            targetCamera.transform.position = Vector3.Lerp(startPos, targetPosition, elapsed);
            targetCamera.transform.rotation = Quaternion.Slerp(startRot, targetRotation, elapsed);
            yield return null;
        }

        // Ensure final position is exactly as calculated
        targetCamera.transform.position = targetPosition;
        targetCamera.transform.rotation = targetRotation;

        isZooming = false;
        isZoomedIn = true;
        IsNavigatingObject = true;
        onComplete?.Invoke();
    }

    private IEnumerator SmoothReturnToOriginalCoroutine(float zoomSpeed, System.Action onComplete)
    {
        Debug.Log("SmoothReturn");
        isZooming = true;

        Vector3 startPos = targetCamera.transform.position;
        Quaternion startRot = targetCamera.transform.rotation;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            targetCamera.transform.position = Vector3.Lerp(startPos, originalCameraPos, elapsed);
            targetCamera.transform.rotation = Quaternion.Slerp(startRot, originalCameraRot, elapsed);
            yield return null;
        }

        // Ensure final position is exactly as calculated
        targetCamera.transform.position = originalCameraPos;
        targetCamera.transform.rotation = originalCameraRot;

        isZooming = false;
        isZoomedIn = false;
        IsNavigatingObject = false;
        onReturnCallback = null;
        onComplete?.Invoke();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && targetCamera != null)
        {
            float maxFoV = defaultFieldOfView > 0 ? defaultFieldOfView : targetCamera.fieldOfView;
            minFieldOfView = Mathf.Min(minFieldOfView, maxFoV);
            targetCamera.fieldOfView = Mathf.Clamp(targetCamera.fieldOfView, minFieldOfView, maxFoV);
        }
    }
#endif
}
