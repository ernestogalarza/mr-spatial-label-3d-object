using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AnchorManager : MonoBehaviour
{
    [Header("References")]
    public Transform rightController;
    public OVRSpatialAnchor anchorPrefab;

    [Header("Anchor Object Database")]
    public AnchorObjectData[] anchorObjects;

    private List<OVRSpatialAnchor> activeAnchors = new();
    private PersistedAnchorInfoList persistedList = new();

    private const string PlayerPrefsKey = "PERSISTED_SPATIAL_ANCHORS";

    private int nextId = 1;
    private List<GameObject> spawnedVisuals = new List<GameObject>();
    
    private List<AnchorMarkInstance> anchorInstances = new List<AnchorMarkInstance>();


    private bool visualsVisible = true;

    async void Start()
    {
        await LoadAnchorsAsync();
    }

    void Update()
    {
        // A (Right) → Crear Anchor
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            CreateAnchorAsync();
        }

        // B (Right) → Toggle Visuales
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            ToggleVisuals();
        }

        // X (Left) → Eliminar Todos
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            DeleteAllAnchorsAsync();
        }
    }

    // ===============================
    // CREATE ANCHOR
    // ===============================
    async void CreateAnchorAsync()
    {
        if (anchorInstances.Count >= anchorObjects.Length)
        {
            Debug.LogWarning("Maximum number of anchors reached.");
            return;
        }

        Vector3 pos = rightController.position;
        Quaternion rot = rightController.rotation;

        var anchor = Instantiate(anchorPrefab, pos, rot);

        while (!anchor.Created)
            await Task.Yield();

        while (!anchor.Localized)
            await Task.Yield();

        var saveResult = await anchor.SaveAnchorAsync();

        if (!saveResult.Success)
        {
            Debug.LogError("Failed to save anchor.");
            Destroy(anchor.gameObject);
            return;
        }

        int id = anchorObjects[anchorInstances.Count].id;

        GameObject visual = SpawnVisual(anchor, id);

        AnchorMarkInstance instance = new AnchorMarkInstance
        {
            anchor = anchor,
            visual = visual,
            id = id
        };

        anchorInstances.Add(instance);

        Debug.Log($"Anchor created with ID {id}");
    }

    async Task LoadAnchorsAsync()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
            return;

        string json = PlayerPrefs.GetString(PlayerPrefsKey);
        persistedList = JsonUtility.FromJson<PersistedAnchorInfoList>(json);

        if (persistedList == null || persistedList.anchors.Count == 0)
            return;

        foreach (var info in persistedList.anchors)
        {
            Guid guid = new Guid(info.uuid);

            var loadOptions = new OVRSpatialAnchor.LoadOptions
            {
                Uuids = new Guid[] { guid }
            };


            var unboundAnchors =
                await OVRSpatialAnchor.LoadUnboundAnchorsAsync(loadOptions);

            if (unboundAnchors == null || unboundAnchors.Length == 0)
            {
                Debug.LogWarning("Failed loading anchor: " + info.uuid);
                continue;
            }

            var unbound = unboundAnchors[0];

            var anchor = Instantiate(anchorPrefab);

            unbound.BindTo(anchor);

            while (!anchor.Localized)
                await Task.Yield();

            activeAnchors.Add(anchor);

            SpawnVisual(anchor, info.id);

            if (info.id >= nextId)
                nextId = info.id + 1;

            Debug.Log("Loaded anchor ID: " + info.id);
        }
    }

    
    // ===============================
    // SPAWN VISUAL (NO HIJO)
    // ===============================
    GameObject SpawnVisual(OVRSpatialAnchor anchor, int id)
    {
        GameObject prefab = GetPrefabById(id);

        if (prefab == null)
        {
            Debug.LogWarning("No prefab assigned for ID " + id);
            return null;
        }

        GameObject visual = Instantiate(prefab);

        // Posicionar encima del anchor
        float heightOffset = 0.2f;

        visual.transform.position =
            anchor.transform.position +
            anchor.transform.up * heightOffset;
        visual.transform.rotation = anchor.transform.rotation;

     //   visual.transform.localScale = Vector3.one;
     // Si quieres que "flote" 20 cm por encima
    // visual.transform.localPosition += new Vector3(0, 0.2f, 0);
    

        AnchorFollower follower = visual.AddComponent<AnchorFollower>();
        follower.targetAnchor = anchor;

        return visual;
    }
    

    // ===============================
    // HELPER
    // ===============================
    GameObject GetPrefabById(int id)
    {
        foreach (var entry in anchorObjects)
        {
            if (entry.id == id)
                return entry.prefab;
        }

        return null;
    }

    // ===============================
    // TOGGLE ANCHOR COMPONENT ONLY
    // ===============================
    void ToggleVisuals()
    {
        visualsVisible = !visualsVisible;

        foreach (var instance in anchorInstances)
        {
            if (instance.visual != null)
                instance.visual.SetActive(visualsVisible);
        }

        Debug.Log("Associated visuals visible: " + visualsVisible);
    }

    // ===============================
    // DELETE ALL
    // ===============================
    async void DeleteAllAnchorsAsync()
    {
        foreach (var instance in anchorInstances)
        {
            if (instance.anchor != null)
            {
                await instance.anchor.EraseAnchorAsync();
                Destroy(instance.anchor.gameObject);
            }

            if (instance.visual != null)
            {
                Destroy(instance.visual);
            }
        }

        anchorInstances.Clear();

        Debug.Log("All anchors and visuals deleted.");
    }

    void SavePersistedData()
    {
        string json = JsonUtility.ToJson(persistedList);
        PlayerPrefs.SetString(PlayerPrefsKey, json);
        PlayerPrefs.Save();
    }
}