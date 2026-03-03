using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BtnTitleIcon : MonoBehaviour
{
    

    public TextMeshProUGUI txtTitleLabel;
    public TextMeshProUGUI txtSubtitleLabel;
    public Image iconImage;
    public string labelSpanish;
    public string labelEnglish;

    public Sprite playSprit;

    private string subtextLock = "(Press to revel in Spanish)";
    private string subtextUnlock = "(Press to play Audio)";
    
    //icon_play-circle_24_Filled
    
    
    

    public void unlockButton()
    {
        txtTitleLabel.text = labelSpanish;
        txtSubtitleLabel.text = subtextUnlock;
        iconImage.sprite = playSprit;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        txtTitleLabel.text = labelEnglish;
        txtSubtitleLabel.text = subtextLock;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
