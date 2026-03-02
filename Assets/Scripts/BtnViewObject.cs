using UnityEngine;

public class BtnViewObject : MonoBehaviour
{
    public ObjectAnchorManager objectAnchorManager;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objectAnchorManager = GameObject.Find("ObjectAnchorManager").GetComponent<ObjectAnchorManager>();
    }
    
    public void toggleVirtualObject()
    {
        Debug.Log("======>PRESS MISMO: "+gameObject.name);
        
        objectAnchorManager.toggleVirtualObject(gameObject);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
