using UnityEngine;

public class AnchorMarkerButton : MonoBehaviour
{
    private GameObject linkedObject;

    [Header("Visual Feedback")]
    public Renderer buttonRenderer;
    public Color visibleColor = Color.green;
    public Color hiddenColor = Color.red;

    public void Initialize(GameObject obj)
    {
        linkedObject = obj;
        UpdateVisual(false);
    }

    // Este método lo conectas al Poke Interactable (On Select)
    public void OnButtonPressed()
    {
        if (linkedObject == null)
        {
            Debug.LogError("❌ linkedObject es NULL");
            return;
        }

        bool newState = !linkedObject.activeSelf;
        linkedObject.SetActive(newState);

        UpdateVisual(newState);

        Debug.Log($"🔁 {linkedObject.name} ahora está {(newState ? "VISIBLE" : "OCULTO")}");
    }

    private void UpdateVisual(bool isVisible)
    {
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color =
                isVisible ? visibleColor : hiddenColor;
        }
    }
}