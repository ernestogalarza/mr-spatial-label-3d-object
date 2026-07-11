using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class AnchorFollower : MonoBehaviour
{
    public OVRSpatialAnchor targetAnchor;
    public float heightOffset = 0.2f;
    public float forwardOffset = 0f;
    public float rotationOffset = 0f; // Este es el valor de pushRotationPositionObject
    
    public GameObject targetMarker;

    private HandGrabInteractable handGrabInteractable;

    /*
    void Awake()
    {
        handGrabInteractable = GetComponent<HandGrabInteractable>();
    }*/

    void LateUpdate()
    {
        if (targetAnchor == null || !targetAnchor.Created)
            return;

        /*
        // 🚨 Si está siendo agarrado, NO lo movemos
        if (handGrabInteractable != null &&
            handGrabInteractable.State == InteractableState.Select)
            return;
*/
        transform.position =
            targetAnchor.transform.position +
            targetAnchor.transform.up * heightOffset +
            targetAnchor.transform.forward * forwardOffset;
            ;

       // transform.rotation = targetAnchor.transform.rotation;
       transform.rotation = targetAnchor.transform.rotation * Quaternion.Euler(0, rotationOffset, 0);
    }
}