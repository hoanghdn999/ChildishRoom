using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class InformationPopup : MonoBehaviour
{
    public const float TIME_SHOW = 0.2f;
    [SerializeField] protected Transform panel;
    [SerializeField] protected TextMeshProUGUI txtTitle;
    [SerializeField] protected TextMeshProUGUI txtDescription;
    [SerializeField] protected Button btnCTA;
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected Image imgQR;

    private string url;

    public static System.Action onStartShowDialog;
    public System.Action onHitESCKey;

    private void ParseData(string title, string description, string url, Sprite qrCode){
        txtTitle.text = title;
        txtDescription.text = description;
        this.url = url;
        imgQR.sprite = qrCode;

        InteractiveCamera.onHitESCKey += CloseDialog;
        btnCTA.onClick.RemoveAllListeners();
        btnCTA.onClick.AddListener(OnClickCTA);
    }

    void OnClickCTA()
    {
        Application.OpenURL(url);
    }

    public static void ShowDialog(string title, string description, string url, Sprite qrCode, System.Action onHitESCKey){
        var d = FindFirstObjectByType<InformationPopup>(FindObjectsInactive.Include);
        if (d != null && !d.isActiveAndEnabled)
        {
            d.gameObject.SetActive(true);
            d.ParseData(title, description, url, qrCode);
            d.AnimationShow();
        }
    }

    #region Base Show/Hide

        protected virtual void AnimationShow()
        {
            onStartShowDialog?.Invoke();
            this.panel.localScale = Vector3.zero;
            if (this.canvasGroup != null)
            {
                this.canvasGroup.alpha = 1;

            }
            Sequence seq = DOTween.Sequence();
            seq.Join(this.panel.DOScale(1f, TIME_SHOW).SetEase(Ease.OutBack));
            if (this.canvasGroup != null)
            {
               seq.Join(this.canvasGroup.DOFade(1, TIME_SHOW));
            }
            seq.OnComplete(this.OnCompleteShow).SetDelay(0.001f);
            
            
        }
        protected virtual void OnCompleteShow()
        {
        }
        
        protected virtual void AnimationHide()
        {
            Sequence seq = DOTween.Sequence();
            seq.Join(this.panel.DOScale(0.0f, TIME_SHOW).SetEase(Ease.Linear));
            if (this.canvasGroup != null)
            {
                seq.Join(this.canvasGroup.DOFade(0, TIME_SHOW));
            }
            seq.OnComplete(this.OnCompleteHide);
        }

        protected virtual void OnCompleteHide()
        {
            this.gameObject.SetActive(false);
            InteractiveCamera.Instance.SmoothReturnToOriginal(2);
        }

        public void CloseDialog()
        {
        Debug.Log("CloseDialog");
            AnimationHide();
        }

        private void Clear()
        {
        }
        #endregion
}
