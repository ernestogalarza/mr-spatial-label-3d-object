using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class SpatialAnchorManager : MonoBehaviour
{
    [Header("Prefabs")]
    public OVRSpatialAnchor anchorPrefab;
    public GameObject anchorMarkerPrefab;
    public AnchorObjectData[] anchorObjects;

    [Header("Controller")]
    public Transform rightController;
    
    [Header("Input")]
    public OVRInput.Button createButton = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Button deleteButton = OVRInput.Button.One;
    public OVRInput.Button toggleAnchorButton = OVRInput.Button.One;
    public OVRInput.Button toggleLabelButton = OVRInput.Button.Two;
    public OVRInput.Button changeSceneButton = OVRInput.Button.PrimaryIndexTrigger;
    
    

    private List<AnchorInstance> anchorInstances = new List<AnchorInstance>();
    private Dictionary<System.Guid, int> anchorUuidToId = new Dictionary<System.Guid, int>();

    private bool visualsVisible = true;
    
    private bool objectContentVisible = true;
    
    private string currentSceneName;
    private string sceneSaveKey;
    
    
    private void ChangeScene()
    {
        SceneManager.LoadScene("ARLabelScene");
    }
    

    // =====================================================
    // START
    // =====================================================
    void Start()
    {  currentSceneName = SceneManager.GetActiveScene().name;
        sceneSaveKey = "SavedAnchors_" + currentSceneName;

        Debug.Log("Anchor system for scene: " + currentSceneName);
        LoadAnchorUuidToIdMapping();
        LoadAnchorsAsync();
    }

    // =====================================================
    // UPDATE
    // =====================================================
    void Update()
    {
        if (OVRInput.GetDown(createButton, OVRInput.Controller.RTouch))
            CreateAnchorAsync();
        

        if (OVRInput.GetDown(deleteButton, OVRInput.Controller.RTouch))
            DeleteAllAnchorsAsync();
        

        if (OVRInput.GetDown(toggleAnchorButton, OVRInput.Controller.LTouch))
            ToggleVisuals();
        

        if (OVRInput.GetDown(toggleLabelButton, OVRInput.Controller.LTouch))
            ToggleLabelsVisuals();
        
        
        if (OVRInput.GetDown(changeSceneButton, OVRInput.Controller.LTouch))
            ChangeScene();
    }

    // =====================================================
    // CREATE ANCHOR
    // =====================================================
    public async void CreateAnchorAsync()
    {
        if (anchorInstances.Count >= anchorObjects.Length)
        {
            Debug.LogWarning("Maximum anchors reached");
            return;
        }

        Vector3 pos = rightController.position;
        Quaternion rot = rightController.rotation;

        var anchor = Instantiate(anchorPrefab, pos, rot);

        while (!anchor.Created)
            await Task.Yield();

        var result = await anchor.SaveAnchorAsync();

        if (!result.Success)
        {
            Debug.LogError("Anchor save failed");
            Destroy(anchor.gameObject);
            return;
        }

        int id = anchorObjects[anchorInstances.Count].id;

        anchorUuidToId[anchor.Uuid] = id;
        SaveAnchorUuidToIdMapping();

        GameObject marker = null;

        if (anchorMarkerPrefab != null)
        {
            marker = Instantiate(anchorMarkerPrefab, anchor.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localRotation = Quaternion.identity;
        }

        GameObject content = SpawnContentObject(anchor, id);

        AnchorInstance instance = new AnchorInstance
        {
            anchor = anchor,
            anchorMarker = marker,
            contentObject = content,
            id = id
        };

        anchorInstances.Add(instance);
    }

    // =====================================================
    // SPAWN CONTENT (Compatible ISDK)
    // =====================================================
    GameObject SpawnContentObject(OVRSpatialAnchor anchor, int id)
    {
        AnchorObjectData data = GetDataById(id);

        if (data == null || data.prefab == null)
        {
            Debug.LogWarning("Prefab not found for id: " + id);
            return null;
        }

        GameObject obj = Instantiate(data.prefab);

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

    // =====================================================
    // TOGGLE MARKERS
    // =====================================================
    public void ToggleVisuals()
    {
        visualsVisible = !visualsVisible;

        foreach (var instance in anchorInstances)
        {
            if (instance.anchorMarker != null)
                instance.anchorMarker.SetActive(visualsVisible);
        }
    }
    
    
    // =====================================================
    // TOGGLE MARKERS
    // =====================================================
    public void ToggleLabelsVisuals()
    {
        objectContentVisible = !objectContentVisible;

        foreach (var instance in anchorInstances)
        {
            if (instance.contentObject != null )
                instance.contentObject.SetActive(objectContentVisible);
        }
    }

    // =====================================================
    // DELETE ALL
    // =====================================================
    public async void DeleteAllAnchorsAsync()
    {
        foreach (var instance in anchorInstances)
        {
            if (instance.anchor != null)
            {
                await instance.anchor.EraseAnchorAsync();
                Destroy(instance.anchor.gameObject);
            }

            if (instance.anchorMarker != null)
                Destroy(instance.anchorMarker);

            if (instance.contentObject != null)
                Destroy(instance.contentObject);
        }

        anchorInstances.Clear();
        anchorUuidToId.Clear();
        PlayerPrefs.DeleteKey(sceneSaveKey);
    }

    // =====================================================
    // LOAD ANCHORS
    // =====================================================
    async void LoadAnchorsAsync()
    {
        if (anchorUuidToId.Count == 0)
            return;

        var loadOptions = new OVRSpatialAnchor.LoadOptions
        {
            Uuids = new List<System.Guid>(anchorUuidToId.Keys).ToArray()
        };

        var unboundAnchors =
            await OVRSpatialAnchor.LoadUnboundAnchorsAsync(loadOptions);

        if (unboundAnchors == null)
            return;

        foreach (var unbound in unboundAnchors)
        {
            GameObject go =
                new GameObject("LoadedAnchor_" + unbound.Uuid);

            var anchor = go.AddComponent<OVRSpatialAnchor>();
            unbound.BindTo(anchor);

            int id = GetIdForAnchor(unbound.Uuid);

            GameObject marker = null;

            if (anchorMarkerPrefab != null)
            {
                marker = Instantiate(anchorMarkerPrefab, anchor.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localRotation = Quaternion.identity;
            }

            GameObject content = SpawnContentObject(anchor, id);

            AnchorInstance instance = new AnchorInstance
            {
                anchor = anchor,
                anchorMarker = marker,
                contentObject = content,
                id = id
            };

            anchorInstances.Add(instance);
        }
    }

    // =====================================================
    // HELPERS
    // =====================================================
    AnchorObjectData GetDataById(int id)
    {
        foreach (var entry in anchorObjects)
        {
            if (entry.id == id)
                return entry;
        }
        return null;
    }

    int GetIdForAnchor(System.Guid uuid)
    {
        if (anchorUuidToId.TryGetValue(uuid, out int id))
            return id;

        return -1;
    }

    void SaveAnchorUuidToIdMapping()
    {
        PersistedAnchorInfoList list =
            new PersistedAnchorInfoList();

        foreach (var kvp in anchorUuidToId)
        {
            list.anchors.Add(new PersistedAnchorInfo
            {
                uuid = kvp.Key.ToString(),
                id = kvp.Value
            });
        }

        PlayerPrefs.SetString(
            sceneSaveKey,
            JsonUtility.ToJson(list));

        PlayerPrefs.Save();
    }

    void LoadAnchorUuidToIdMapping()
    {
        if (!PlayerPrefs.HasKey(sceneSaveKey))
        {
            Debug.Log("No saved anchors for scene: " + currentSceneName);
            return;
        }

        var json = PlayerPrefs.GetString(sceneSaveKey);
        var list =
            JsonUtility.FromJson<PersistedAnchorInfoList>(json);

        anchorUuidToId.Clear();

        foreach (var info in list.anchors)
        {
            anchorUuidToId[
                System.Guid.Parse(info.uuid)
            ] = info.id;
        }
    }
}