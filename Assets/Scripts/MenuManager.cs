using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    
    //[SerializeField] private TMP_InputField _txtAngle;
    [SerializeField] public TMP_InputField txtCurrentParticipant;
    [SerializeField] public TextMeshProUGUI lblCurrentScene;
    [SerializeField] public TMP_Dropdown dropdown;
    private int currentParticipant = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OnDropdownSceneChanged(int index)
    {
        Debug.Log("====>OPTION: " + index);

        switch (index)
        {
            case 0:
                //Spatial label
                Debug.Log("==TEST== Spatial label");
              //  SceneManager.LoadScene("SpatialLabelScene");
                break;

            case 1:
                //Object Augmented
                Debug.Log("==TEST== Object Augmented");
                SceneManager.LoadScene("ObjectAugmentationScene");
                break;

            case 2:
                Debug.Log("==PILOT== Spatial label");
                SceneManager.LoadScene("SpatialLabelScene");
                break;

            case 3:
                Debug.Log("==PILOT== Object Augmented");
                break;
        }
    }

    public void SettingParticipantId(String type)
    {
        Debug.LogWarning("Changing Participant Id");
        switch (type)
        {
            case "1":
                //adding 
                currentParticipant++;
                break;

            case "0":
                currentParticipant--;
                break;

            default:
                break;
        }
        
        
        txtCurrentParticipant.text = currentParticipant.ToString();
    }
}
