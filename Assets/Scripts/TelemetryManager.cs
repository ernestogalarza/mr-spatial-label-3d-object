using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class TelemetryManager : MonoBehaviour
{
    public static TelemetryManager Instance { get; private set; }

    // Cambiamos a private para protegerlo de cambios accidentales
    [SerializeField] private string _participantID = "NoAsignado";
    public string ParticipantID => _participantID;
    
    public string currentCondition = "Ninguna"; 
    private string filePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Al despertar, si ya teníamos un ID (por ser el original), reinicializamos la ruta
        if (_participantID != "NoAsignado")
        {
            InitializeCSV();
        }
    }

    public void SetParticipantID(string id)
    {
        _participantID = id;
        InitializeCSV();
    }

    private void InitializeCSV()
    {
        filePath = Path.Combine(Application.persistentDataPath, $"Participante_{_participantID}_Logs.csv");
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Timestamp,ParticipanteID,Condicion,Evento,ObjetoDestino\n");
        }
    }

    public void LogEvent(string eventType, string targetObject)
    {
        if (_participantID == "NoAsignado") return;

        string logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},{_participantID},{currentCondition},{eventType},{targetObject}\n";
        
        try {
            File.AppendAllText(filePath, logLine);
        } catch (Exception e) {
            Debug.LogError($"[Telemetry] Error: {e.Message}");
        }
    }
}