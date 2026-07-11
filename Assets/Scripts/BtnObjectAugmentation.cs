using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BtnObjectAugmentation : MonoBehaviour
{
    public TextMeshProUGUI txtTitleLabel;
    public TextMeshProUGUI txtSubtitleLabel;

    public Image iconImage;
    public Sprite revealIcon;
    public Sprite lockIcon;

    private string labelSpanish;
    private string labelEnglish;
    private AudioClip audioClip;
    public AudioSource audioSource;

    private GameObject augmentationPrefab;
    private GameObject spawnedObject;

    private string subtextLock = "(Press to reveal object)";
    private string subtextUnlock = "(Object revealed)";

    private bool lockButton = true;
    private bool objectSpawned = false;
    private AnchorInstance anchorInstance;
    
    private OVRSpatialAnchor targetAnchor;
    
    private float pushUpPositionObject;
    private float pushRotationPositionObject;

    public void Configure(string spanish, string english, AudioClip clip,GameObject objectAugmentation,
        OVRSpatialAnchor anchor,
        AnchorInstance instance,
        float pushUpPosition,
        float pushRotation)
    {
        labelSpanish = spanish;
        labelEnglish = english;
        augmentationPrefab = objectAugmentation;
        targetAnchor = anchor;
        anchorInstance = instance;

        pushRotationPositionObject = pushRotation;
        pushUpPositionObject = pushUpPosition;

        txtTitleLabel.text = labelEnglish;
        txtSubtitleLabel.text = subtextLock;

        audioClip = clip;
        iconImage.sprite = lockIcon;
    }

    public void OnButtonPressed()
    {
        if (lockButton)
        {
            UnlockButtonClick();
        }

        // Instanciar solo una vez
        if (!objectSpawned && augmentationPrefab != null)
        {
            SpawnAugmentation();
        }
        
        if (audioClip != null)
            audioSource.PlayOneShot(audioClip);
        
        Debug.Log("Botón presionado");
        
        if (TelemetryManager.Instance != null)
        {
            TelemetryManager.Instance.LogEvent("Click_Boton", this.gameObject.name);
        }
    }

    private void UnlockButtonClick()
    {
        txtTitleLabel.text = labelSpanish;

        txtSubtitleLabel.gameObject.SetActive(false);
        txtSubtitleLabel.text = subtextUnlock;

        iconImage.sprite = revealIcon;

        lockButton = false;
    }

    private void SpawnAugmentation()
    {
        // 1. Calcular la posición desplazada
        // Usamos transform.up para moverlo hacia "arriba" del botón y 
        // transform.forward para moverlo hacia "adelante" (o ajustar según tu necesidad)
        Vector3 spawnPosition = transform.position + (transform.up * pushUpPositionObject);

        // 2. Calcular la rotación
        // Aplicamos la rotación base del transform más el offset de rotación deseado
        Quaternion spawnRotation = transform.rotation * Quaternion.Euler(0, pushRotationPositionObject, 0);
        
        spawnedObject = Instantiate(
            augmentationPrefab,
            spawnPosition,
            spawnRotation
        );

        // 🔹 Hacer que siga el mismo anchor
        AnchorFollower follower = spawnedObject.GetComponent<AnchorFollower>();

        if (follower == null)
            follower = spawnedObject.AddComponent<AnchorFollower>();

        
        follower.targetAnchor = targetAnchor;
        follower.heightOffset = pushUpPositionObject > 0 ? pushUpPositionObject : 0.08f;      // Tu variable de altura
        follower.rotationOffset = pushRotationPositionObject;
        
        // Offset opcional para que no colisione con el botón
        follower.forwardOffset = 0.06f;
        
        // 🔹 GUARDAR referencia en AnchorInstance
        if (anchorInstance != null)
            anchorInstance.objectAugmentation = spawnedObject;


        objectSpawned = true;
    }

    public void LockButtonClick()
    {
        txtTitleLabel.text = labelEnglish;

        txtSubtitleLabel.gameObject.SetActive(true);
        txtSubtitleLabel.text = subtextLock;

        iconImage.sprite = lockIcon;

       // augmentationPrefab.SetActive(false);

       if (objectSpawned)
       {
           Destroy(spawnedObject);
           objectSpawned = false;
       }

       
        
        lockButton = true;
    }
}