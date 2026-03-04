using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AnchorObjectData
{
    public int id;
    public GameObject prefab; 
    
    [Header("UI Config")]
    public string labelSpanish;
    public string labelEnglish;
    public Sprite iconSprite;
    public AudioClip audioClip;
    
    [HideInInspector]
    public GameObject spawnedObject;
}

[System.Serializable]
public class PersistedAnchorInfo
{
    public string uuid;
    public int id;
}

[System.Serializable]
public class PersistedAnchorInfoList
{
    public List<PersistedAnchorInfo> anchors = new List<PersistedAnchorInfo>();
}

[System.Serializable]
public class AnchorInstance
{
    public OVRSpatialAnchor anchor;      // Anchor lógico
    public GameObject anchorMarker;      // Prefab visual del anchor
    public GameObject contentObject;     // Objeto del array
    public int id;                       // ID del objeto
}


[System.Serializable]
public class AnchorMarkInstance
{
    public OVRSpatialAnchor anchor;
    public GameObject visual;
    public int id;
}