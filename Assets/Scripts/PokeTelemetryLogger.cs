using UnityEngine;
using Oculus.Interaction; // SDK v83 de Meta

public class PokeTelemetryLogger : MonoBehaviour
{
    [Header("Identificador (Ej: Silla_Etiqueta o Manzana_3D)")]
    public string objectName;

    private PokeInteractable pokeInteractable;

    private void Awake()
    {
        pokeInteractable = GetComponent<PokeInteractable>();
        
        if (pokeInteractable == null)
        {
            Debug.LogError($"[Telemetry] No se encontró un PokeInteractable en {gameObject.name}");
        }
    }

    private void OnEnable()
    {
        if (pokeInteractable != null)
        {
            // Nos suscribimos al evento general de cambio de estado del SDK v83
            pokeInteractable.WhenStateChanged += HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        if (pokeInteractable != null)
        {
            pokeInteractable.WhenStateChanged -= HandleStateChanged;
        }
    }

    // El SDK v83 pasa los argumentos del cambio de estado (viejo y nuevo)
    private void HandleStateChanged(InteractableStateChangeArgs args)
    {
        // Solo registramos el log cuando el estado pasa a "Select" (Poke completado)
        if (args.NewState == InteractableState.Select)
        {
            // 2. Mide la Frecuencia de Interacción
            TelemetryManager.Instance.LogEvent("Poke_Realizado", objectName);
        }
    }
}