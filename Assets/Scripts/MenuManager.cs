using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lblParticipant;
    
    [Header("Input")]
    public OVRInput.Button addButton = OVRInput.Button.One;
    public OVRInput.Button minusButtons = OVRInput.Button.Two;
    
    private int currentParticipant = 1;
    
    void Start()
    {
        // Reflejamos el valor inicial en la UI
        lblParticipant.text = currentParticipant.ToString();
    }

    public void LoadSpatialLabelScene()
    {
        Debug.Log("==Spatial label");
        if (TelemetryManager.Instance != null)
        {
            TelemetryManager.Instance.currentCondition = "Condition_A_SpatialLabel";
            TelemetryManager.Instance.LogEvent("Inicio_Prueba", "Escena_Cargada");
        }
        SceneManager.LoadScene("SpatialLabelScene"); 
    }

    public void LoadObjectAugmentedScene()
    {
        Debug.Log("==Object Augmented");
        // 2. Opcional pero recomendado: Registramos qué condición va a empezar
        if (TelemetryManager.Instance != null)
        {
            TelemetryManager.Instance.currentCondition = "Condition_B_3DObject";
            TelemetryManager.Instance.LogEvent("Inicio_Prueba", "Escena_Cargada");
        }
        SceneManager.LoadScene("ObjectAugmentationScene"); 
    }

    public void SettingParticipantId(String type)
    {
        Debug.LogWarning("Changing Participant Id");
        switch (type)
        {
            case "1":
                currentParticipant++;
                break;

            case "0":
                if(currentParticipant > 0) // Pequeña protección para evitar IDs negativos
                    currentParticipant--;
                break;
        }
        
        lblParticipant.text = currentParticipant.ToString();
        
        // 2. ¡CRÍTICO! Actualizamos el TelemetryManager
        if (TelemetryManager.Instance != null)
        {
            TelemetryManager.Instance.SetParticipantID(currentParticipant.ToString());
            Debug.Log($"[MenuManager] ID de participante actualizado a: {currentParticipant}");
        }
    }
    
    // =====================================================
    // INPUT
    // =====================================================

    void Update()
    {
        if (OVRInput.GetDown(addButton, OVRInput.Controller.LTouch))
            SettingParticipantId("1");

        if (OVRInput.GetDown(minusButtons, OVRInput.Controller.LTouch))
            SettingParticipantId("0");
    }
}