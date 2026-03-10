using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class SpatialLabelManager : BaseAnchorManager
{
    
    public Transform rightController;
    public AnchorObjectData[] anchorObjects;
     
    
    [Header("Input")]
    public OVRInput.Button createButton = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Button deleteButton = OVRInput.Button.One;
    public OVRInput.Button lockButtons = OVRInput.Button.Two;
    public OVRInput.Button toggleAnchorButton = OVRInput.Button.One;
    public OVRInput.Button saveNewAnchors = OVRInput.Button.Two;
    public OVRInput.Button changeSceneButton = OVRInput.Button.PrimaryIndexTrigger;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(deleteButton, OVRInput.Controller.RTouch))
            DeleteAllAnchorsAsync();
        
        if (OVRInput.GetDown(createButton, OVRInput.Controller.RTouch))
            CreateAnchorAsync();
        
        if (OVRInput.GetDown(saveNewAnchors, OVRInput.Controller.RTouch))
            RecreateAllAnchorsFromMarkers();
        
        if (OVRInput.GetDown(toggleAnchorButton, OVRInput.Controller.LTouch))
            ToggleVisuals();
        

        if (OVRInput.GetDown(lockButtons, OVRInput.Controller.LTouch))
            LockAllButtons();
        
        
        if (OVRInput.GetDown(changeSceneButton, OVRInput.Controller.LTouch))
            ChangeScene();

    }
    
    public async void RecreateAllAnchorsFromMarkers()
{
    if (anchorInstances.Count == 0)
    {
        Debug.LogWarning("No anchors to recreate.");
        return;
    }

    var instancesCopy = new List<AnchorInstance>(anchorInstances);

    foreach (var instance in instancesCopy)
    {
        if (instance == null || instance.anchorMarker == null)
            continue;

        Vector3 newPos = instance.anchorMarker.transform.position;
        Quaternion newRot = instance.anchorMarker.transform.rotation;

        // 🔹 Guardar referencia del anchor viejo
        OVRSpatialAnchor oldAnchor = instance.anchor;

        // 🔹 Guardar posición actual del contenido
        Vector3 contentWorldPos = instance.contentObject.transform.position;
        Quaternion contentWorldRot = instance.contentObject.transform.rotation;

        // 1️⃣ Crear nuevo anchor PRIMERO
        var newAnchor = Instantiate(anchorPrefab, newPos, newRot);

        while (!newAnchor.Created)
            await Task.Yield();

        var result = await newAnchor.SaveAnchorAsync();

        if (!result.Success)
        {
            Debug.LogError("Failed to save new anchor for id: " + instance.id);
            Destroy(newAnchor.gameObject);
            continue;
        }

        // 2️⃣ Actualizar mapping
        anchorUuidToId[newAnchor.Uuid] = instance.id;

        // 3️⃣ Actualizar referencia en instance
        instance.anchor = newAnchor;

        // 4️⃣ Actualizar follower
        AnchorFollower follower = instance.contentObject?.GetComponent<AnchorFollower>();
        if (follower != null)
        {
            follower.targetAnchor = newAnchor;
        }

        // 5️⃣ Restaurar posición exacta del contenido
        instance.contentObject.transform.position = contentWorldPos;
        instance.contentObject.transform.rotation = contentWorldRot;
        
        
        
        
        if (instance.anchorMarker != null)
        {
            // 1️⃣ Desacoplar del anchor viejo
            instance.anchorMarker.transform.SetParent(newAnchor.transform);

            // 2️⃣ Reposicionar al nuevo anchor
            instance.anchorMarker.transform.position = newPos;
           instance.anchorMarker.transform.rotation = newRot;

            // 3️⃣ Escala y visibilidad
            instance.anchorMarker.SetActive(true);
        }

        // 6️⃣ Ahora sí borrar anchor viejo
        if (oldAnchor != null)
        {
            await oldAnchor.EraseAnchorAsync();
            anchorUuidToId.Remove(oldAnchor.Uuid);
            Destroy(oldAnchor.gameObject);
        }
        
    }

    SaveAnchorUuidToIdMapping();

    Debug.Log("All anchors recreated and aligned correctly.");
}
    
    public void LockAllButtons()
    {
        foreach (var instance in anchorInstances)
        {
            if (instance.contentObject != null)
            {
                BtnSpatialLabel btn = instance.contentObject.GetComponent<BtnSpatialLabel>();

                if (btn != null)
                {
                    btn.LockButtonClick();
                }
            }
        }

        Debug.Log("All buttons locked again.");
    }
    
    private void ChangeScene()
    {
        SceneManager.LoadScene("ARLabelScene");
    }
    
    public async void CreateAnchorAsync()
    {
        if (anchorInstances.Count >= anchorObjects.Length)
        {
            Debug.LogWarning("Maximum anchors reached");
            return;
        }

        Vector3 pos = rightController.position;
        Quaternion rot = rightController.rotation;

        // 🔥 Usamos el método del padre
        AnchorInstance instance = await CreateAnchorBase(pos, rot);

        if (instance == null)
            return;

        int id = anchorObjects[anchorInstances.Count].id;

        instance.id = id;
        instance.sceneName = SceneManager.GetActiveScene().name;

        // 🔹 Registrar UUID mapping (esto es responsabilidad de la hija)
        anchorUuidToId[instance.anchor.Uuid] = id;
        SaveAnchorUuidToIdMapping();

        // 🔹 Crear contenido específico de labels
        GameObject content = SpawnContentObject(
            instance.anchor,
            instance.anchorMarker,
            id
        );

        instance.contentObject = content;

        anchorInstances.Add(instance);
    }
    
    // =====================================================
    // SPAWN CONTENT (Compatible ISDK)
    // =====================================================
  protected override GameObject   SpawnContentObject(OVRSpatialAnchor anchor,GameObject anchorMarker, int id)
    {
        AnchorObjectData data = GetDataById(id);

        if (data == null || data.prefab == null)
        {
            Debug.LogWarning("Prefab not found for id: " + id);
            return null;
        }

        GameObject obj = Instantiate(data.prefab);
        
        BtnSpatialLabel btnScript = obj.GetComponent<BtnSpatialLabel>();

        if (btnScript != null)
        {
            btnScript.Configure(
                data.labelSpanish,
                data.labelEnglish,
                data.audioClip
            );
        }

        float heightOffset = 0.04f;

        obj.transform.position =
            anchor.transform.position +
            anchor.transform.up * heightOffset;

        obj.transform.rotation = anchor.transform.rotation;

        // 🚨 NO parent
        // 🚨 NO mover Rigidbody manualmente
        // 🚨 NO tocar interactable

        
        AnchorFollower follower = obj.GetComponent<AnchorFollower>();
        if (follower == null)
            follower = obj.AddComponent<AnchorFollower>();

        follower.targetAnchor = anchor;
        follower.heightOffset = heightOffset;

        return obj;
    }

    protected override GameObject SpawnContentObject(OVRSpatialAnchor anchor, GameObject marker, GameObject objectAugmentation, int id)
    {
        throw new System.NotImplementedException();
    }


    AnchorObjectData GetDataById(int id)
    {
        foreach (var entry in anchorObjects)
        {
            if (entry.id == id)
                return entry;
        }
        return null;
    }

}
