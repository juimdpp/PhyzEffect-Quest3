using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using TMPro;

public class SpatialAnchorManager: MonoBehaviour
{
    public GameObject anchorPrefab;
    public TMP_Text console;

    private OVRSpatialAnchor lastCreatedAnchor;
    private TMP_Text savedStatusOfLastCreatedAnchor;
    private bool isInitialized = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInitialized) return;

        // Create Ray
        Vector3 rayOrigin = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        Vector3 rayDirection = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.forward;

        // Check if it intersects with Scene API elements
        if (MRUK.Instance.GetCurrentRoom().Raycast(new Ray(rayOrigin, rayDirection), float.MaxValue, out RaycastHit hit, out MRUKAnchor anchor))
        {
            if (anchor != null && OVRInput.GetDown(OVRInput.Button.One))
            {
                Quaternion rotation = Quaternion.LookRotation(-hit.normal);
                StartCoroutine(CreateSpatialAnchor(hit.point, rotation));
            }
        }

        //if (OVRInput.GetDown(OVRInput.Button.One))
        //{
        //    SaveSpatialAnchor(); // TODO: currently saves last created anchor. Should change to the anchor that is pointed at
        //}
    }

    public void Initialized()
    {
        Log("HYUNSOO - initialized");
        isInitialized = true;
    }

    private IEnumerator CreateSpatialAnchor(Vector3 position, Quaternion rotation)
    {
        Log("HYUNSOO CreateaSpatialAnchor");
        GameObject prefab = Instantiate(anchorPrefab, position, rotation);
        Log("HYUNSOO Created gameobject");
        var anchor = prefab.AddComponent<OVRSpatialAnchor>();

        // Wait for the async creation
        yield return new WaitUntil(() => anchor.Created);

        Log($"HYUNSOO Created anchor {anchor.Uuid}");

        lastCreatedAnchor = anchor;

        var canvas = anchor.GetComponentInChildren<Canvas>();
        canvas.gameObject.transform.GetChild(0).GetComponent<TMP_Text>().text = anchor.Uuid.ToString(); // uuid
        savedStatusOfLastCreatedAnchor = canvas.gameObject.transform.GetChild(1).GetComponent<TMP_Text>();
        savedStatusOfLastCreatedAnchor.text = "Created but not saved"; // savedStatus
    }

    private void SaveSpatialAnchor()
    {
        Log("TODO");
    }

    void Log(string str)
    {
        console.text += "HYUNSOO " + str + "\n";
    }
}
