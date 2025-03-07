using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using TMPro;
using System;

class AnchorQuad
{
    public List<OVRSpatialAnchor> anchorList;

    public void SetAnchor(OVRSpatialAnchor anchor)
    {
        if (anchorList.Count < 4)
        {
            anchorList.Add(anchor);
        }
        else
        {
            Debug.Log("Trying to add too many anchors");
        }
    }

    public void Reset()
    {
        Debug.Log("HYUNSOO: Reset AnchorPair");
        if (anchorList == null) anchorList = new List<OVRSpatialAnchor>();
        anchorList.Clear();
    }

    public void EraseRecentAnchor()
    {
        
        int idx = anchorList.Count - 1;
        if (idx >= 0)
        {
            Debug.Log("HYUNSOO: Erased previous anchor");
            anchorList.RemoveAt(idx);
        }
        else
        {
            Debug.Log("HYUNSOO: Nothing to erase");
        }
    }

    public bool isValid()
    {
        return anchorList.Count == 4;
    }
}

public class SpatialAnchorManagerQuad : MonoBehaviour
{
    public GameObject anchorPrefab;
    public GameObject anchorPreviewPrefab;
    public TMP_Text console;
    public Material color;

    private GameObject previewAnchor;
    private bool isInitialized = false;
    private List<GameObject> mySurfaces;
    private AnchorQuad anchorQuad;

    // Start is called before the first frame update
    void Start()
    {
        anchorQuad = new AnchorQuad();
        anchorQuad.Reset();
        previewAnchor = Instantiate(anchorPreviewPrefab);
        mySurfaces = new List<GameObject>();
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
            previewAnchor.transform.position = lHit.point;
            previewAnchor.transform.rotation = Quaternion.FromToRotation(Vector3.up, lHit.normal);

            if (lAnchor != null && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) // MUST CREATE IN THIS ORDER: bottom left -> bottom right -> top left -> top right
            {
                Quaternion rotation = Quaternion.LookRotation(-lHit.normal);
                StartCoroutine(CreateSpatialAnchor(anchorPrefab, lHit.point, rotation, (createdAnchor) =>
                {
                    anchorQuad.SetAnchor(createdAnchor);
                    Log($"Check validity of anchor: {createdAnchor.Uuid}");
                }));
            }
        }


        if (OVRInput.GetDown(OVRInput.Button.One)) // create surface
        {
            CreateSurface();
            anchorQuad.Reset();
        }

        if (OVRInput.GetDown(OVRInput.Button.Three)) // save all surfaces
        {
            // SaveAllSurfaces();
        }

        if (OVRInput.GetDown(OVRInput.Button.Four)) // erase most recent
        {
            anchorQuad.EraseRecentAnchor();
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

        CreateQuad(0f, 0f);
    }

    private void CreateSurface()
    {
        Log("Creating Surface");
        if (!anchorQuad.isValid())
        {
            Log($"Number of anchors is not four! {anchorQuad.anchorList.Count}");
            return;
        }
        GameObject surface = CreateQuad(0.4f, 0.8f);

        mySurfaces.Add(surface);

        Log("Created quad at first anchor poisition");
    }


    private GameObject CreateQuad(float width, float height)
    {
        Log("CreateQuad");
        GameObject obj = new GameObject();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = color;

        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();

        
        Vector3[] vertices = new Vector3[4];
        int idx = 0;
        anchorQuad.anchorList.ForEach(anchor =>
        {
            vertices[idx++] = anchor.transform.position;
            Log($"vertex {idx}th = {anchor.transform.position}");
        });
            
        mesh.vertices = vertices;


        mesh.triangles = new int[]
        {
            0, 2, 1, // First triangle
            2, 3, 1  // Second triangle
        };

        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshRenderer.material = color; // Apply a default material

        obj.AddComponent<MeshCollider>();
        Log("CreatedQuad");
        return obj;
    }


    private IEnumerator CreateSpatialAnchor(GameObject anchorPrefab, Vector3 position, Quaternion rotation, Action<OVRSpatialAnchor> callback)
    {
        Log("Creating anchor");
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


    void Log(string str)
    {
        console.text += "HYUNSOO " + str + "\n";
        Debug.Log("HYUNSOO " + str);
    }
}
