using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public abstract class BaseAnchorManager : MonoBehaviour
{
    [Header("Base Prefabs")]
    public OVRSpatialAnchor anchorPrefab;
    public GameObject anchorMarkerPrefab;

    protected List<AnchorInstance> anchorInstances = new();
    protected Dictionary<System.Guid, int> anchorUuidToId = new();

    protected string currentSceneName;
    protected string sceneSaveKey;

    protected bool visualsVisible = true;

    // =====================================================
    // INITIALIZATION
    // =====================================================
    protected virtual void Awake()
    {
        InitializePersistence();
        LoadAnchorUuidToIdMapping();
    }

    protected virtual void Start()
    {
        LoadAnchorsAsync();
    }

    protected virtual void InitializePersistence()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        sceneSaveKey = "SavedAnchors_" + currentSceneName;
    }

    // =====================================================
    // CREATE BASE ANCHOR (Anchor + Marker)
    // =====================================================
    protected async Task<AnchorInstance> CreateAnchorBase(Vector3 pos, Quaternion rot)
    {
        var anchor = Instantiate(anchorPrefab, pos, rot);

        while (!anchor.Created)
            await Task.Yield();

        var result = await anchor.SaveAnchorAsync();

        if (!result.Success)
        {
            Destroy(anchor.gameObject);
            return null;
        }

        GameObject marker = null;

        if (anchorMarkerPrefab != null)
        {
            marker = Instantiate(anchorMarkerPrefab, anchor.transform);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localRotation = Quaternion.identity;
            marker.SetActive(true);
        }

        AnchorInstance instance = new AnchorInstance
        {
            anchor = anchor,
            anchorMarker = marker,
            sceneName = currentSceneName
        };

        return instance;
    }

    // =====================================================
    // LOAD ANCHORS (BASE BEHAVIOR)
    // =====================================================
    protected async void LoadAnchorsAsync()
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
            GameObject go = new GameObject("LoadedAnchor_" + unbound.Uuid);
            var anchor = go.AddComponent<OVRSpatialAnchor>();
            unbound.BindTo(anchor);

            int id = GetIdForAnchor(unbound.Uuid);

            GameObject marker = null;

            if (anchorMarkerPrefab != null)
            {
                marker = Instantiate(anchorMarkerPrefab, anchor.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localRotation = Quaternion.identity;
                marker.SetActive(true);
            }

            GameObject content = SpawnContentObject(anchor, marker, id);

            AnchorInstance instance = new AnchorInstance
            {
                anchor = anchor,
                anchorMarker = marker,
                contentObject = content,
                id = id,
                sceneName = currentSceneName
            };

            anchorInstances.Add(instance);
        }
    }

    // =====================================================
    // DELETE ALL
    // =====================================================
    public async Task DeleteAllAnchorsAsync()
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
            
            if (instance.objectAugmentation != null)
                Destroy(instance.objectAugmentation);
        }

        anchorInstances.Clear();
        anchorUuidToId.Clear();

        PlayerPrefs.DeleteKey(sceneSaveKey);
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
    // PERSISTENCE
    // =====================================================
    protected void SaveAnchorUuidToIdMapping()
    {
        PersistedAnchorInfoList list = new PersistedAnchorInfoList();

        foreach (var kvp in anchorUuidToId)
        {
            list.anchors.Add(new PersistedAnchorInfo
            {
                uuid = kvp.Key.ToString(),
                id = kvp.Value
            });
        }

        PlayerPrefs.SetString(sceneSaveKey, JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    protected void LoadAnchorUuidToIdMapping()
    {
        if (!PlayerPrefs.HasKey(sceneSaveKey))
            return;

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

    protected int GetIdForAnchor(System.Guid uuid)
    {
        if (anchorUuidToId.TryGetValue(uuid, out int id))
            return id;

        return -1;
    }

    // =====================================================
    // ABSTRACT CONTENT SPAWN (MUST BE IMPLEMENTED)
    // =====================================================
    protected abstract GameObject SpawnContentObject(
        OVRSpatialAnchor anchor,
        GameObject marker,
        int id
    );
    
    protected abstract GameObject SpawnContentObject(
        OVRSpatialAnchor anchor,
        GameObject marker,
        GameObject objectAugmentation,
        int id
    );
    
    

}