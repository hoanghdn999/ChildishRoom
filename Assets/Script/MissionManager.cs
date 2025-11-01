using UnityEngine;
using TMPro;

public class MissionManager : MonoBehaviour
{
    [Header("UI References (Children)")]
    public Transform header;
    public Transform progress;

    private TextMeshProUGUI headerText;
    private TextMeshProUGUI progressText;

    [Header("Mission Settings")]
    public int totalObjects = 11;
    private int foundCount = 0;

    private void Start()
    {
        // Automatically get the TMP components from the children
        if (header != null)
            headerText = header.GetComponentInChildren<TextMeshProUGUI>();

        if (progress != null)
            progressText = progress.GetComponentInChildren<TextMeshProUGUI>();

        UpdateMissionUI();
    }

    public void RegisterFound()
    {
        if (foundCount >= totalObjects) return;
        foundCount++;
        UpdateMissionUI();
    }

    private void UpdateMissionUI()
    {
        if (headerText != null)
            headerText.text = "Hãy cùng mình khám phá những món đồ tuổi thơ\r\nđang ẩn giấu trong căn phòng này nha!";
        if (progressText != null)
            progressText.text = $"{foundCount:00}/{totalObjects:11}";
    }
}
