using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class ObjectAugmentationManager : BaseAnchorManager
{
    public Transform rightController;
    public AnchorObjectAugmentationData[] anchorObjects;

    [Header("Input")]
    public OVRInput.Button createButton = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Button deleteButton = OVRInput.Button.One;
    public OVRInput.Button lockButtons = OVRInput.Button.Two;
    public OVRInput.Button toggleAnchorButton = OVRInput.Button.One;
    public OVRInput.Button saveNewAnchors = OVRInput.Button.Two;
    public OVRInput.Button changeSceneButton = OVRInput.Button.PrimaryIndexTrigger;

    protected override void Start()
    {
        base.Start();
    }

    // =====================================================
    // REQUIRED BY BASE CLASS
    // =====================================================

    protected override GameObject SpawnContentObject(
        OVRSpatialAnchor anchor,
        GameObject marker,
        int id)
    {
        AnchorObjectAugmentationData data = GetDataById(id);

        if (data == null)
            return null;

        return SpawnContentInternal(anchor, marker, id, data, null);
    }

    protected override GameObject SpawnContentObject(
        OVRSpatialAnchor anchor,
        GameObject marker,
        GameObject objectAugmentation,
        int id)
    {
        AnchorObjectAugmentationData data = GetDataById(id);

        if (data == null)
            return null;

        return SpawnContentInternal(anchor, marker, id, data, null);
    }

    // =====================================================
    // INTERNAL SPAWN (USED BY CREATE)
    // =====================================================

    GameObject SpawnContentInternal(
        OVRSpatialAnchor anchor,
        GameObject marker,
        int id,
        AnchorObjectAugmentationData data,
        AnchorInstance instance)
    {
        if (data.prefab == null)
        {
            Debug.LogWarning("Prefab not found for id: " + id);
            return null;
        }

        GameObject obj = Instantiate(data.prefab);

        BtnObjectAugmentation btnScript = obj.GetComponent<BtnObjectAugmentation>();

        if (btnScript != null)
        {
            btnScript.Configure(
                data.labelSpanish,
                data.labelEnglish,
                data.audioClip,
                data.objectAugmentation,
                anchor,
                instance
            );
        }

        float heightOffset = 0.04f;

        obj.transform.position =
            anchor.transform.position +
            anchor.transform.up * heightOffset;

        obj.transform.rotation = anchor.transform.rotation;

        AnchorFollower follower = obj.GetComponent<AnchorFollower>();
        if (follower == null)
            follower = obj.AddComponent<AnchorFollower>();

        follower.targetAnchor = anchor;
        follower.heightOffset = heightOffset;

        return obj;
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

        AnchorInstance instance = await CreateAnchorBase(pos, rot);

        if (instance == null)
            return;

        int id = anchorObjects[anchorInstances.Count].id;

        instance.id = id;
        instance.sceneName = SceneManager.GetActiveScene().name;

        anchorUuidToId[instance.anchor.Uuid] = id;
        SaveAnchorUuidToIdMapping();

        AnchorObjectAugmentationData data = GetDataById(id);

        GameObject content = SpawnContentInternal(
            instance.anchor,
            instance.anchorMarker,
            id,
            data,
            instance
        );

        instance.contentObject = content;

        anchorInstances.Add(instance);
    }

    // =====================================================
    // RECREATE ANCHORS
    // =====================================================

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

            OVRSpatialAnchor oldAnchor = instance.anchor;

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

            anchorUuidToId[newAnchor.Uuid] = instance.id;

            instance.anchor = newAnchor;

            AnchorFollower follower = instance.contentObject?.GetComponent<AnchorFollower>();
            if (follower != null)
                follower.targetAnchor = newAnchor;

            AnchorFollower augFollower = instance.objectAugmentation?.GetComponent<AnchorFollower>();
            if (augFollower != null)
                augFollower.targetAnchor = newAnchor;

            if (instance.anchorMarker != null)
            {
                instance.anchorMarker.transform.SetParent(newAnchor.transform);
                instance.anchorMarker.transform.position = newPos;
                instance.anchorMarker.transform.rotation = newRot;
                instance.anchorMarker.SetActive(true);
            }

            if (oldAnchor != null)
            {
                await oldAnchor.EraseAnchorAsync();
                anchorUuidToId.Remove(oldAnchor.Uuid);
                Destroy(oldAnchor.gameObject);
            }
        }

        SaveAnchorUuidToIdMapping();

        Debug.Log("All anchors recreated correctly.");
    }

    // =====================================================
    // LOCK BUTTONS
    // =====================================================

    public void LockAllButtons()
    {
        foreach (var instance in anchorInstances)
        {
            if (instance.contentObject != null)
            {
                BtnObjectAugmentation btn =
                    instance.contentObject.GetComponent<BtnObjectAugmentation>();

                if (btn != null)
                    btn.LockButtonClick();
            }
        }
    }

    // =====================================================
    // CHANGE SCENE
    // =====================================================

    private void ChangeScene()
    {
        SceneManager.LoadScene("ARLabelScene");
    }

    // =====================================================
    // DATA LOOKUP
    // =====================================================

    AnchorObjectAugmentationData GetDataById(int id)
    {
        foreach (var entry in anchorObjects)
        {
            if (entry.id == id)
                return entry;
        }

        return null;
    }

    // =====================================================
    // INPUT
    // =====================================================

    void Update()
    {
        if (OVRInput.GetDown(deleteButton, OVRInput.Controller.RTouch))
            _ = DeleteAllAnchorsAsync();

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
}