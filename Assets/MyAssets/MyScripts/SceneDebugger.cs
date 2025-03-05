using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Meta.XR.BuildingBlocks;

public class MySceneDebugger : MonoBehaviour
{
    public TMP_Text console;
    public GameObject anchorPrefab;
    public SpatialAnchorCoreBuildingBlock coreBuildingBlock;

    private Dictionary<System.Guid, OVRSpatialAnchor> uuids;

    // Start is called before the first frame update
    void Start()
    {
        if (coreBuildingBlock)
        {
            coreBuildingBlock.OnAnchorCreateCompleted.AddListener(SaveAnchorUuidToLocalStorage);
            coreBuildingBlock.OnAnchorsEraseAllCompleted.AddListener(ErasedAllAnchors);
            coreBuildingBlock.OnAnchorsLoadCompleted.AddListener(LoadedAnchors);
        }

        uuids = new Dictionary<System.Guid, OVRSpatialAnchor>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            AppendToConsole("Pressed A - Creating anchor...");
        }

        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            AppendToConsole("Pressed B - Loading anchors...");
        }


        if (OVRInput.GetDown(OVRInput.Button.Four))
        {
            AppendToConsole("Position gameobject to spatialanchor position");
            PositionObjectToAnchorPosition();
        }
    }

    internal void PositionObjectToAnchorPosition()
    {
        // Choose a random spatialanchor
        if(uuids.Count == 0)
        {
            AppendToConsole("No anchors saved");
            return;
        }

        System.Guid randomGuid = GetRandomGuid();
        if(uuids.TryGetValue(randomGuid, out OVRSpatialAnchor anchor)){
            Vector3 position = anchor.transform.position;
            AppendToConsole($"Got the anchor at position {position}");
            gameObject.transform.position = position;
            
        }
        else
        {
            AppendToConsole("Couldn't get random spatial anchor");
        }
        
        // Create gameobject at its position
    }

    System.Guid GetRandomGuid()
    {
        List<System.Guid> keys = new List<System.Guid>(uuids.Keys);
        return keys[Random.Range(0, uuids.Count)];
    }
    internal void SaveAnchorUuidToLocalStorage(OVRSpatialAnchor anchor, OVRSpatialAnchor.OperationResult result)
    {
        AppendToConsole("Anchor creation succeeded!");
        if (result != OVRSpatialAnchor.OperationResult.Success)
        {
            AppendToConsole("Anchor creation failed...");
            return;
        }
        AppendToConsole("Accessing anchor uuid");
        uuids.Add(anchor.Uuid, anchor);
        AppendToConsole($"Saved anchor {anchor.Uuid} to list");
    }

    internal void LoadedAnchors(List<OVRSpatialAnchor> anchors)
    {
        AppendToConsole("Loaded all anchors");
    }

    internal void ErasedAllAnchors(OVRSpatialAnchor.OperationResult result)
    {
        if (result != OVRSpatialAnchor.OperationResult.Success)
        {
            AppendToConsole("Anchor erase failed...");
            return;
        }
        AppendToConsole("Erased all anchors");
    }



    public void OnRoomLoaded()
    {
        AppendToConsole("Hello! Room is loaded.");
    }

    void AppendToConsole(string str)
    {
        console.text += ("[HYUNSOO] " + str + "\n");
        Debug.Log($"[HYUNSOO] {str}");
    }
}
