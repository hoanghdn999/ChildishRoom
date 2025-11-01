using UnityEngine;

public class ClickToPlayPop : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPop();
    }
}
