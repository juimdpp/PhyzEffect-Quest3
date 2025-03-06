using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using TMPro;
using System;

class AnchorPair
{
    public OVRSpatialAnchor topLeft;
    public OVRSpatialAnchor btmRight;

    int mostRecentIdx = -1; // 0 = Left, 1 = Right, -1 = nothing
    
    public void SetLeftAnchor(OVRSpatialAnchor anchor)
    {
        Debug.Log("HYUNSOO: setleftanchor begin");
        topLeft = anchor;
        mostRecentIdx = 0;
        Debug.Log("HYUNSOO: setleftanchor end");
    }
    public void SetRightAnchor(OVRSpatialAnchor anchor)
    {
        Debug.Log("HYUNSOO: setrightanchor begin");
        btmRight = anchor;
        mostRecentIdx = 1;
        Debug.Log("HYUNSOO: setrightanchor end");
    }

    public Vector3 GetCenter()
    {
        return (topLeft.transform.position + btmRight.transform.position) / 2f; ;
    }

    public float GetWidth()
    {
        return Mathf.Abs(topLeft.transform.position.x - btmRight.transform.position.x);
    }

    public float GetHeight()
    {
        return Mathf.Abs(topLeft.transform.position.y - btmRight.transform.position.y);
    }

    public void EraseRecentAnchor()
    {
        if (topLeft && mostRecentIdx == 0)
        {
            // Destroy topLeft
            // Remove from canvas
            if (btmRight) 
            {
                mostRecentIdx = 1; 
            }
            else
            {
                mostRecentIdx = -1;
            }
        }
        if(btmRight && mostRecentIdx == 1)
        {
            // Destroy topLeft
            // Remove from canvas
            if (topLeft)
            {
                mostRecentIdx = 0;
            }
            else
            {
                mostRecentIdx = -1;
            }
        }
        if(mostRecentIdx == -1)
        {
            topLeft = null;
            btmRight = null;
        }
    }

    public bool isValid()
    {
        return topLeft && btmRight;
    }
}

public class SpatialAnchorManager: MonoBehaviour
{
    public GameObject leftAnchorPrefab;
    public GameObject rightAnchorPrefab;
    public GameObject surfacePrefab;
    public TMP_Text console;

    private OVRSpatialAnchor lastCreatedAnchor;
    private TMP_Text savedStatusOfLastCreatedAnchor;
    private bool isInitialized = false;

    private AnchorPair anchorPair;

    // Start is called before the first frame update
    void Start()
    {
        anchorPair = new AnchorPair();
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
            if (anchor != null && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) // top left anchor creation
            {
                Quaternion rotation = Quaternion.LookRotation(-hit.normal);
                StartCoroutine(CreateSpatialAnchor(leftAnchorPrefab, hit.point, rotation, (createdAnchor) =>
                {
                    anchorPair.SetLeftAnchor(createdAnchor);
                    Log($"Check validity of anchor: {createdAnchor.Uuid}");
                }));
            }

            if (anchor != null && OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) // btm right anchor creation
            {
                Quaternion rotation = Quaternion.LookRotation(-hit.normal);
                StartCoroutine(CreateSpatialAnchor(rightAnchorPrefab, hit.point, rotation, (createdAnchor) =>
                {
                    anchorPair.SetRightAnchor(createdAnchor);
                    Log($"Check validity of anchor: {createdAnchor.Uuid}");
                }));
            }
        }

        if (OVRInput.GetDown(OVRInput.Button.One)) // create surface
        {
            CreateSurface();
        }

        if (OVRInput.GetDown(OVRInput.Button.Three)) // reset
        {
            
        }

        if (OVRInput.GetDown(OVRInput.Button.Four)) // erase most recent
        {

        }

    }

    public void Initialized()
    {
        Log("Initialized");
        isInitialized = true;
    }

    private void CreateSurface()
    {
        Log("Creating Surface");
        if (!anchorPair.isValid())
        {
            Log($"Anchors are not valid! {anchorPair.topLeft.Uuid} - {anchorPair.btmRight.Uuid}");
            return;
        }

        // Find the center of the plane
        Vector3 center = anchorPair.GetCenter();

        // Create plane
        GameObject surface = Instantiate(surfacePrefab, center, Quaternion.identity);

        // Scale the plane
        surface.transform.localScale = new Vector3(anchorPair.GetWidth(), 10f, anchorPair.GetHeight());

        // TODO
    }

   

    private IEnumerator CreateSpatialAnchor(GameObject anchorPrefab, Vector3 position, Quaternion rotation, Action<OVRSpatialAnchor> callback)
    {
        GameObject prefab = Instantiate(anchorPrefab, position, rotation);
        var anchor = prefab.AddComponent<OVRSpatialAnchor>();

        // Wait for the async creation
        yield return new WaitUntil(() => anchor.Created);

        Log($"Created anchor {anchor.Uuid}");

        //var canvas = anchor.GetComponentInChildren<Canvas>();
        //canvas.gameObject.transform.GetChild(0).GetComponent<TMP_Text>().text = anchor.Uuid.ToString(); // uuid
        //savedStatusOfLastCreatedAnchor = canvas.gameObject.transform.GetChild(1).GetComponent<TMP_Text>();
        //savedStatusOfLastCreatedAnchor.text = "Created but not saved"; // savedStatus

        callback.Invoke(anchor);
    }

    private void SaveSpatialAnchor()
    {
        Log("TODO");
    }

    void Log(string str)
    {
        console.text += "HYUNSOO " + str + "\n";
        Debug.Log("HYUNSOO " + str);
    }
}
