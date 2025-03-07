using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using TMPro;
using System;

class AnchorPair
{
    public OVRSpatialAnchor topLeft = null;
    public OVRSpatialAnchor btmRight = null;
    public Vector3 normal = Vector3.negativeInfinity;
    public int label = -1; // 0 = wall, 1 = floor, -1 = nothing
    public const int WALL = 0;
    public const int FLOOR = 1;

    int mostRecentIdx = -1; // 0 = Left, 1 = Right, -1 = nothing
    
    
    public void SetSurfaceType(int _label)
    {
        Debug.Log($"HYUNSOO: SetLabel {_label}");
        if (label == -1)
        {
            label = _label;
        }
        else if (label == -1 && label != _label)
        {
            Debug.Log($"Left and Right anchors have different labels... {label} != {_label}");
        }
    }
    public void SetLeftAnchor(OVRSpatialAnchor anchor)
    {
        topLeft = anchor;
        mostRecentIdx = 0;
    }
    public void SetRightAnchor(OVRSpatialAnchor anchor)
    {
        btmRight = anchor;
        mostRecentIdx = 1;
    }

    public void SetNormal(Vector3 _normal)
    {
        Debug.Log($"HYUNSOO: SetNormal {_normal} vs {normal} vs {Vector3.negativeInfinity} ==> {normal == Vector3.negativeInfinity} vs {normal.Equals(Vector3.negativeInfinity)}");
        if (normal.Equals(Vector3.negativeInfinity))
        {
            normal = _normal;
        }
        else if(!normal.Equals(Vector3.negativeInfinity) && normal != _normal)
        {
            Debug.Log($"Left and Right anchors have different normal... {normal} != {_normal}");
        }
    }

    public void Reset()
    {
        Debug.Log("HYUNSOO: Reset AnchorPair");
        // TODO
        topLeft = null;
        btmRight = null;
        normal = Vector3.negativeInfinity;
        label = -1;
        mostRecentIdx = -1;
    }

    public Vector3 GetCenter()
    {
        return (topLeft.transform.position + btmRight.transform.position) / 2f; ;
    }

    public Vector3 GetScale()
    {
        var x = Mathf.Abs(topLeft.transform.localPosition.x - btmRight.transform.localPosition.x);
        var y = Mathf.Abs(topLeft.transform.localPosition.y - btmRight.transform.localPosition.y);
        var z = Mathf.Abs(topLeft.transform.localPosition.z - btmRight.transform.localPosition.z);

        return new Vector3(x, y, z);
    }

    public float GetWidth()
    {
        return Mathf.Abs(topLeft.transform.position.x - btmRight.transform.position.x);
    }

    public float GetHeight()
    {
        if(label == WALL)
        {
            return Mathf.Abs(topLeft.transform.position.y - btmRight.transform.position.y);
        }
        else if(label == FLOOR)
        {
            return Mathf.Abs(topLeft.transform.position.z - btmRight.transform.position.z);
        }
        else
        {
            return -1;
        }
        
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
            Reset();
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
    public GameObject leftAnchorPreviewPrefab;
    public GameObject rightAnchorPrefab;
    public GameObject rightAnchorPreviewPrefab;
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject cubePrefab;
    public TMP_Text console;

    private GameObject leftPreviewAnchor;
    private GameObject rightPreviewAnchor;
    private bool isInitialized = false;

    private AnchorPair anchorPair;

    // Start is called before the first frame update
    void Start()
    {
        anchorPair = new AnchorPair();
        anchorPair.Reset();
        leftPreviewAnchor = Instantiate(leftAnchorPreviewPrefab);
        rightPreviewAnchor = Instantiate(rightAnchorPreviewPrefab);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInitialized) return;

        // Create Ray
        Vector3 leftRayOrigin = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        Vector3 leftRayDirection = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch) * Vector3.forward;

        // Check if it intersects with Scene API elements
        if (MRUK.Instance.GetCurrentRoom().Raycast(new Ray(leftRayOrigin, leftRayDirection), float.MaxValue, out RaycastHit lHit, out MRUKAnchor lAnchor))
        {
            leftPreviewAnchor.transform.position = lHit.point;
            leftPreviewAnchor.transform.rotation = Quaternion.FromToRotation(Vector3.up, lHit.normal);

            if (lAnchor != null && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) // top left anchor creation
            {
                Quaternion rotation = Quaternion.LookRotation(-lHit.normal);
                StartCoroutine(CreateSpatialAnchor(leftAnchorPrefab, lHit.point, rotation, (createdAnchor) =>
                {
                    anchorPair.SetLeftAnchor(createdAnchor);
                    anchorPair.SetNormal(MRUK.Instance.GetCurrentRoom().GetFacingDirection(lAnchor));
                    anchorPair.SetSurfaceType(LabelToInt(lAnchor.Label));
                    Log($"Check validity of anchor: {createdAnchor.Uuid}");
                }));
            }
        }

        // Create Ray
        Vector3 rightRayOrigin = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        Vector3 rightRayDirection = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.forward;

        // Check if it intersects with Scene API elements
        if (MRUK.Instance.GetCurrentRoom().Raycast(new Ray(rightRayOrigin, rightRayDirection), float.MaxValue, out RaycastHit rHit, out MRUKAnchor rAnchor))
        {
            rightPreviewAnchor.transform.position = rHit.point;
            rightPreviewAnchor.transform.rotation = Quaternion.FromToRotation(rHit.normal, Vector3.up);

            if (rAnchor != null && OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) // btm right anchor creation
            {
                Quaternion rotation = Quaternion.LookRotation(-rHit.normal);
                StartCoroutine(CreateSpatialAnchor(rightAnchorPrefab, rHit.point, rotation, (createdAnchor) =>
                {
                    anchorPair.SetRightAnchor(createdAnchor);
                    anchorPair.SetNormal(MRUK.Instance.GetCurrentRoom().GetFacingDirection(rAnchor));
                    anchorPair.SetSurfaceType(LabelToInt(rAnchor.Label));
                    Log($"Check validity of anchor: {createdAnchor.Uuid}");
                }));
            }
        }

        

        if (OVRInput.GetDown(OVRInput.Button.One)) // create surface
        {
            CreateSurface();
            anchorPair.Reset();
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
        var room = MRUK.Instance.GetCurrentRoom();
        // Visualize normals of all walls and floors
        foreach (var wall in room.WallAnchors)
        {
            // Draw a red line showing the normal direction
            Debug.DrawRay(wall.GetAnchorCenter(), room.GetFacingDirection(wall) * 0.5f, Color.red);
        }

        Debug.DrawRay(room.CeilingAnchor.GetAnchorCenter(), room.GetFacingDirection(room.CeilingAnchor) * 0.5f, Color.red);
    }

    private int LabelToInt(MRUKAnchor.SceneLabels label)
    {
        return label == MRUKAnchor.SceneLabels.FLOOR ? AnchorPair.FLOOR : label == MRUKAnchor.SceneLabels.WALL_FACE ? AnchorPair.WALL : -1;
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
        // GameObject surface = Instantiate(anchorPair.label == AnchorPair.WALL ? wallPrefab : floorPrefab, center, Quaternion.identity);
        GameObject surface = Instantiate(cubePrefab);

        // Scale the plane
        float width = anchorPair.GetWidth();
        float height = anchorPair.GetHeight();
        Log($"Before: LocalScale: {surface.transform.localScale} && Rotation: {surface.transform.rotation.eulerAngles}");
        surface.transform.localScale = anchorPair.GetScale(); // new Vector3(width, height, 0.1f);
        Log($"Before - anchorNormal: {anchorPair.normal} with {anchorPair.label}");
        surface.transform.rotation = Quaternion.LookRotation(anchorPair.normal);
        surface.transform.position = anchorPair.GetCenter();
        Log($"Width = {width}, Height = {height} => LocalScale: {surface.transform.localScale} && Rotation: {surface.transform.rotation.eulerAngles}");
        // TODO
    }

   

    private IEnumerator CreateSpatialAnchor(GameObject anchorPrefab, Vector3 position, Quaternion rotation, Action<OVRSpatialAnchor> callback)
    {
        GameObject prefab = Instantiate(anchorPrefab, position, rotation);
        var anchor = prefab.AddComponent<OVRSpatialAnchor>();

        // Wait for the async creation
        yield return new WaitUntil(() => anchor.Created);

        Log($"Created anchor {anchor.Uuid} at {position}");

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
