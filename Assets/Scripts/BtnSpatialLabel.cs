using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BtnSpatialLabel : MonoBehaviour
{
    public TextMeshProUGUI txtTitleLabel;
    public TextMeshProUGUI txtSubtitleLabel;
    public Image iconImage;
    public Sprite playIcon;
    public Sprite lockIcon;

    private string labelSpanish;
    private string labelEnglish;

    private AudioClip audioClip;
    public AudioSource audioSource;

    private string subtextLock = "(Press to reveal in Spanish)";
    private string subtextUnlock = "(Press to play Audio)";

    private bool lockButton = true;

    public void Configure(string spanish, string english,  AudioClip clip)
    {
        labelSpanish = spanish;
        labelEnglish = english;
        audioClip = clip;

        txtTitleLabel.text = labelEnglish;
        txtSubtitleLabel.text = subtextLock;
    }

    public void OnButtonPressed()
    {
        if (lockButton)
        {
            UnlockButtonClick();
            
        }
        
        if (audioClip != null)
            audioSource.PlayOneShot(audioClip);
    }


    private void UnlockButtonClick()
    {
        txtTitleLabel.text = labelSpanish;
        txtSubtitleLabel.gameObject.SetActive(false);
        txtSubtitleLabel.text = subtextUnlock;
        iconImage.sprite = playIcon;
        lockButton = false;
    }
    
    public void LockButtonClick()
    {
        txtTitleLabel.text = labelEnglish;
        txtSubtitleLabel.gameObject.SetActive(true);
        txtSubtitleLabel.text = subtextLock;
        iconImage.sprite = lockIcon;
        lockButton = true;
    }
}
