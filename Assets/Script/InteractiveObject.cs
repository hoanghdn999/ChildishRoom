using UnityEngine;
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
    [SerializeField] private InteractiveCamera interactiveCamera;

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

        // Setup InteractiveCamera component
        if (interactiveCamera == null)
            interactiveCamera = MainCamera.GetComponent<InteractiveCamera>();

        if (interactiveCamera == null)
        {
            Debug.LogWarning($"InteractiveObject on {gameObject.name} recommends an InteractiveCamera component on the camera for zoom functionality.", this);
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

                if (Input.GetMouseButtonDown(0) && !InteractiveCamera.IsNavigatingObject && (interactiveCamera == null || !interactiveCamera.IsZooming))
                {
                    ShowInformationPopup();
                    SmoothZoomToObject();
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

    void HandleSmoothReturnToOriginal()
    {
        SmoothReturnToOriginal();
    }

    void ShowInformationPopup()
    {
        InformationPopup.ShowDialog(title, description, url, qrCode, HandleSmoothReturnToOriginal);
    }

    void SmoothZoomToObject()
    {
        if (interactiveCamera == null)
        {
            Debug.LogWarning($"InteractiveObject on {gameObject.name} cannot zoom without InteractiveCamera component.", this);
            return;
        }

        interactiveCamera.SmoothZoomToObject(meshRenderer.bounds, zoomSpeed, zoomDistance, zoomHeightOffset, objectViewOffset, null, HandleSmoothReturnToOriginal);
    }

    void SmoothReturnToOriginal()
    {
        if (interactiveCamera == null)
        {
            Debug.LogWarning($"InteractiveObject on {gameObject.name} cannot return to original position without InteractiveCamera component.", this);
            return;
        }

        interactiveCamera.SmoothReturnToOriginal(zoomSpeed);
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
