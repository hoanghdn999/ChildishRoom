using UnityEngine;

public class CountingObjects : MonoBehaviour
{
    private bool clicked = false;

    private void OnMouseDown()
    {
        if (clicked) return;
        clicked = true;

        MissionManager mission = FindObjectOfType<MissionManager>();
        if (mission != null)
            mission.RegisterFound();
    }
}
