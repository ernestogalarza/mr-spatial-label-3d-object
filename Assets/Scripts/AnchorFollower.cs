using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class AnchorFollower : MonoBehaviour
{
    public OVRSpatialAnchor targetAnchor;
    public float heightOffset = 0.2f;
    public float forwardOffset = 0f;

    private HandGrabInteractable handGrabInteractable;

    void Awake()
    {
        handGrabInteractable = GetComponent<HandGrabInteractable>();
    }

    void LateUpdate()
    {
        if (targetAnchor == null || !targetAnchor.Created)
            return;

        // 🚨 Si está siendo agarrado, NO lo movemos
        if (handGrabInteractable != null &&
            handGrabInteractable.State == InteractableState.Select)
            return;

        transform.position =
            targetAnchor.transform.position +
            targetAnchor.transform.up * heightOffset +
            targetAnchor.transform.forward * forwardOffset;
            ;

        transform.rotation = targetAnchor.transform.rotation;
    }
}