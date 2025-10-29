using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NaiNhunInteraction : MonoBehaviour
{
    public Camera MainCamera;
    public Canvas nainhun_canvas;
    public TextMeshProUGUI infoText;

    private Material originalMaterial;
    private Material highlightMaterial;

    private bool isHovered = false;
    private bool isZooming = false;
    private bool isZoomedIn = false;

    private float zoomSpeed = 2f;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;

    void Start()
    {
        if (MainCamera == null)
            MainCamera = Camera.main;

        // Create highlight emission
        Renderer rend = GetComponent<Renderer>();
        originalMaterial = rend.material;
        highlightMaterial = new Material(originalMaterial);
        highlightMaterial.EnableKeyword("_EMISSION");
        // highlightMaterial.SetColor("_EmissionColor", Color.yellow * 1.5f);

        if (nainhun_canvas != null)
            nainhun_canvas.enabled = false;
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
                    GetComponent<Renderer>().material = highlightMaterial;
                    isHovered = true;
                }

                if (Input.GetMouseButtonDown(0) && !isZooming && !isZoomedIn)
                {
                    originalCameraPos = MainCamera.transform.position;
                    originalCameraRot = MainCamera.transform.rotation;

                    StartCoroutine(SmoothZoomToObject());
                    if (nainhun_canvas != null)
                    {
                        nainhun_canvas.enabled = true;
                        infoText.text = "This is a toy.";
                    }
                }
            }
            else if (isHovered)
            {
                GetComponent<Renderer>().material = originalMaterial;
                isHovered = false;
            }
        }
        else if (isHovered)
        {
            GetComponent<Renderer>().material = originalMaterial;
            isHovered = false;
        }
    }

    void HandleEscapeKey()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isZoomedIn && !isZooming)
        {
            StartCoroutine(SmoothReturnToOriginal());
        }
    }

    System.Collections.IEnumerator SmoothZoomToObject()
    {
        isZooming = true;

        Vector3 startPos = MainCamera.transform.position;
        Quaternion startRot = MainCamera.transform.rotation;

        // Move camera to a point in front of the object
        Vector3 dir = (MainCamera.transform.position - transform.position).normalized;
        targetPosition = transform.position + dir * 1.5f + Vector3.up * 0.3f;
        targetRotation = Quaternion.LookRotation(transform.position - targetPosition);

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * zoomSpeed;
            MainCamera.transform.position = Vector3.Lerp(startPos, targetPosition, elapsed);
            MainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRotation, elapsed);
            yield return null;
        }

        isZooming = false;
        isZoomedIn = true;
    }

    System.Collections.IEnumerator SmoothReturnToOriginal()
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

        if (nainhun_canvas != null)
            nainhun_canvas.enabled = false;

        isZooming = false;
        isZoomedIn = false;
    }
}
