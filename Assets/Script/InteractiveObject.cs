using UnityEngine;
using System.Collections;
[RequireComponent(typeof(MeshRenderer), typeof(MeshCollider))]
public class InteractiveObject : MonoBehaviour
{
    [Header("Information")]
    public string title;
    public string description;
    public string url;
    public Sprite qrCode;


    [Header("Camera Settings")]
    public Camera MainCamera;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float zoomDistance = 1.5f;
    [SerializeField] private float zoomHeightOffset = 0.3f;
    [SerializeField] private Vector3 objectViewOffset = Vector3.zero; // Offset to position object left/right/up/down relative to camera view

    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    
    private Material[] originalMaterials;
    private Material[] highlightMaterials;

    private bool isHovered = false;
    private bool isZooming = false;
    private bool isZoomedIn = false;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;

    public static System.Action onHitESCKey;
    public static bool IsNavigatingObject { get; private set; } = false;

    void Start()
    {
        // Validate required components
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        if (meshRenderer == null)
        {
            Debug.LogError($"InteractiveObject on {gameObject.name} requires a MeshRenderer component.", this);
            enabled = false;
            return;
        }

        if (meshCollider == null)
        {
            Debug.LogError($"InteractiveObject on {gameObject.name} requires a MeshCollider component.", this);
            enabled = false;
            return;
        }

        // Setup camera
        if (MainCamera == null)
            MainCamera = Camera.main;

        if (MainCamera == null)
        {
            Debug.LogError($"InteractiveObject on {gameObject.name} requires a Camera reference.", this);
            enabled = false;
            return;
        }

        // Create highlight materials with yellow emission for all materials
        originalMaterials = meshRenderer.materials;
        highlightMaterials = new Material[originalMaterials.Length];
        
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            highlightMaterials[i] = new Material(originalMaterials[i]);
            highlightMaterials[i].EnableKeyword("_EMISSION");
            highlightMaterials[i].SetColor("_EmissionColor", Color.yellow * 1.5f);
        }
    }

    void Update()
    {
        HandleHoverAndClick();
        HandleEscapeKey();
    }

    void HandleHoverAndClick()
    {
        if (MainCamera == null) return;


        Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform)
            {
                if (!isHovered)
                {
                    meshRenderer.materials = highlightMaterials;
                    isHovered = true;
                }

                if (Input.GetMouseButtonDown(0) && !isZooming && !IsNavigatingObject)
                {
                    originalCameraPos = MainCamera.transform.position;
                    originalCameraRot = MainCamera.transform.rotation;
                    ShowInformationPopup();
                    StartCoroutine(SmoothZoomToObject());
                }
            }
            else if (isHovered)
            {
                meshRenderer.materials = originalMaterials;
                isHovered = false;
            }
        }
        else if (isHovered)
        {
            meshRenderer.materials = originalMaterials;
            isHovered = false;
        }
    }

    void HandleEscapeKey()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isZoomedIn && !isZooming)
        {
            StartCoroutine(SmoothReturnToOriginal());
            onHitESCKey?.Invoke();
        }
    }

    void ShowInformationPopup()
    {
        InformationPopup.ShowDialog(title, description, url, qrCode);
    }
    IEnumerator SmoothZoomToObject()
    {
        isZooming = true;

        Vector3 startPos = MainCamera.transform.position;
        Quaternion startRot = MainCamera.transform.rotation;

        // Calculate object's center using renderer bounds (accounts for object size and position)
        Vector3 objectCenter = meshRenderer.bounds.center;
        
        // Calculate direction from object center to current camera position
        // This maintains the viewing angle relative to each object
        Vector3 dir = (MainCamera.transform.position - objectCenter).normalized;
        
        // Calculate target position: position camera at zoomDistance from object center
        // Camera moves closer to object along the same viewing direction
        targetPosition = objectCenter + dir * zoomDistance;
        
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
            cameraRight = MainCamera.transform.right;
        }
        
        Vector3 cameraUp = Vector3.Cross(lookDirection, cameraRight).normalized;
        
        // Apply view offset (relative to camera's right/up/forward)
        // X = right/left, Y = up/down, Z = forward/back (usually keep at 0)
        Vector3 offsetWorld = cameraRight * objectViewOffset.x + cameraUp * objectViewOffset.y + lookDirection * objectViewOffset.z;
        Vector3 targetLookAt = objectCenter + offsetWorld;
        
        // Make camera look at the offset position
        targetRotation = Quaternion.LookRotation(targetLookAt - targetPosition);

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            MainCamera.transform.position = Vector3.Lerp(startPos, targetPosition, elapsed);
            MainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRotation, elapsed);
            yield return null;
        }

        // Ensure final position is exactly as calculated
        MainCamera.transform.position = targetPosition;
        MainCamera.transform.rotation = targetRotation;

        isZooming = false;
        isZoomedIn = true;
        IsNavigatingObject = true;
    }

    IEnumerator SmoothReturnToOriginal()
    {
        isZooming = true;

        Vector3 startPos = MainCamera.transform.position;
        Quaternion startRot = MainCamera.transform.rotation;

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            MainCamera.transform.position = Vector3.Lerp(startPos, originalCameraPos, elapsed);
            MainCamera.transform.rotation = Quaternion.Slerp(startRot, originalCameraRot, elapsed);
            yield return null;
        }

        isZooming = false;
        isZoomedIn = false;
        IsNavigatingObject = false;
    }

    void OnDestroy()
    {
        // Clean up created materials to prevent memory leaks
        if (highlightMaterials != null)
        {
            for (int i = 0; i < highlightMaterials.Length; i++)
            {
                if (highlightMaterials[i] != null)
                {
                    Destroy(highlightMaterials[i]);
                }
            }
        }
    }
}
