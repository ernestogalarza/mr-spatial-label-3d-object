using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class TelemetryManager : MonoBehaviour
{
    // Singleton estándar, seguro y optimizado para Unity
    public static TelemetryManager Instance { get; private set; }

    private string filePath;
    private Queue<string> logQueue = new Queue<string>();
    private bool isWriting = false;

    [Header("Datos de la Sesión")]
    public string participantID = "NoAsignado";
    public string currentCondition = "Ninguna"; 

    private void Awake()
    {
        // Validación estricta para garantizar que solo exista una instancia
        if (Instance != null && Instance != this)
        {
            Debug.Log("[Telemetry] Destruyendo instancia duplicada.");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); // Sobrevive al cambio de escenas
    }
    
    private void OnEnable()
    {
        // Si ya tenemos un ID pero el archivo no se ha inicializado o el filePath se perdió
        if (participantID != "NoAsignado" && string.IsNullOrEmpty(filePath))
        {
            InitializeCSV();
        }
    }

    public void SetParticipantID(string id)
    {
        participantID = id;
        InitializeCSV();
    }

    private void InitializeCSV()
    {
        // Usamos la ruta protegida interna de la app, que siempre tiene permisos
        string folderPath = Application.persistentDataPath;
        filePath = Path.Combine(folderPath, $"Participante_{participantID}_Logs.csv");

        // ESTO ES LO QUE NECESITAS: 
        // Ver exactamente qué ruta está usando Unity
        Debug.Log($"[Telemetry] PATH COMPLETO DE GUARDADO: {filePath}");

        if (!File.Exists(filePath))
        {
            string header = "Timestamp,ParticipanteID,Condicion,Evento,ObjetoDestino\n";
            File.WriteAllText(filePath, header);
        }
    }

    public void LogEvent(string eventType, string targetObject)
    {
        if (participantID == "NoAsignado") 
        {
            Debug.LogWarning("[Telemetry] El archivo no se crea porque el ID sigue siendo NoAsignado");
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string logLine = $"{timestamp},{participantID},{currentCondition},{eventType},{targetObject}";
    
        // Escribir directamente sin usar la cola por un momento para diagnosticar
        try {
            string path = Path.Combine(Application.persistentDataPath, $"Participante_{participantID}_Logs.csv");
            File.AppendAllText(path, logLine + "\n");
            Debug.Log($"[Telemetry] ESCRITURA FORZADA EN: {path}");
        } catch (Exception e) {
            Debug.LogError($"[Telemetry] Error: {e.Message}");
        }
    }

    // Uso de "async void" para que Unity maneje el evento de forma segura en segundo plano
    private async void FlushQueueToFileAsync()
    {
        isWriting = true;
        
        while (logQueue.Count > 0)
        {
            string line = logQueue.Dequeue();
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
                {
                    await writer.WriteLineAsync(line);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Telemetry] Error crítico de escritura: {e.Message}");
            }
        }
        
        isWriting = false;
    }
}