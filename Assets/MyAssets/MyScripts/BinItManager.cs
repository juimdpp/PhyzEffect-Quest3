using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using TMPro;
using System;
using System.IO;

class AnchorQuad2
{
    public List<OVRSpatialAnchor> anchorList;
    public List<Guid> guidList;

    public AnchorQuad2()
    {
        anchorList = new List<OVRSpatialAnchor>();
        guidList = new List<Guid>();
    }
    public void SetAnchor(OVRSpatialAnchor anchor)
    {
        if (anchorList.Count < 4)
        {
            anchorList.Add(anchor);
            guidList.Add(anchor.Uuid);
        }
        else
        {
            Debug.Log("HYUNSOO: Trying to add too many anchors");
        }
    }

    public void Reset()
    {
        if (anchorList == null) anchorList = new List<OVRSpatialAnchor>();
        anchorList.Clear();
        if (guidList == null) guidList = new List<Guid>();
        guidList.Clear();
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


public class BinItManager : MonoBehaviour
{
    public GameObject anchorPrefab;
    public GameObject anchorPreviewPrefab;
    public GameObject meshAnchorPrefab;
    public GameObject meshAnchorPreviewPrefab;
    public GameObject meshPrefab;
    public TMP_Text console;
    public Material color;
    public int scale = 10;
    public Visualizer visualizer;
    
    private GameObject previewAnchor;
    private float objectHeight;
    private GameObject meshPreviewAnchor;
    private GameObject meshObject;
    private bool isInitialized = false;
    private List<AnchorQuad2> mySurfaces;
    private AnchorQuad2 AnchorQuad2;
    private AnchorQuad2 meshAnchorQuad2;
    private string textPath = "";

    // Start is called before the first frame update
    void Start()
    {
        visualizer.isMenuVisible = false;

        AnchorQuad2 = new AnchorQuad2();
        AnchorQuad2.Reset();

        anchorPreviewPrefab.transform.localScale = new Vector3(scale, scale, scale);
        anchorPrefab.transform.localScale = new Vector3(scale, scale, scale);
        previewAnchor = Instantiate(anchorPreviewPrefab);
        

        mySurfaces = new List<AnchorQuad2>();
        textPath = Application.persistentDataPath + "/savedSurfaces.txt";

        meshPreviewAnchor = Instantiate(meshAnchorPreviewPrefab);
        meshAnchorQuad2 = new AnchorQuad2();
        meshAnchorQuad2.Reset();

        Renderer renderer = previewAnchor.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            objectHeight = renderer.bounds.size.y;
        }

    }

    // Update is called once per frame
    void Update()
    { 
        if (!isInitialized) return;

        if (visualizer.isMenuVisible) return;

        // Create Ray
        Vector3 leftRayOrigin = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        Vector3 leftRayDirection = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch) * Vector3.forward;

        // Check if it intersects with Scene API elements
        if (MRUK.Instance.GetCurrentRoom().Raycast(new Ray(leftRayOrigin, leftRayDirection), float.MaxValue, out RaycastHit lHit, out MRUKAnchor lAnchor))
        {
            previewAnchor.transform.position = lHit.point + lHit.normal * (objectHeight / 2f);
            previewAnchor.transform.rotation = Quaternion.FromToRotation(Vector3.up, lHit.normal);
            if (lAnchor != null && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) // MUST CREATE IN THIS ORDER: bottom left -> bottom right -> top left -> top right
            {
                Quaternion rotation = Quaternion.identity; // Quaternion.LookRotation(-lHit.normal);
                StartCoroutine(CreateSpatialAnchor(anchorPrefab, lHit.point + lHit.normal * (objectHeight / 2f), rotation, (createdAnchor) =>
                {
                    AnchorQuad2.SetAnchor(createdAnchor);
                }));
            }
        }

        if (OVRInput.GetDown(OVRInput.Button.One)) // create surface
        {
            CreateSurface();
            AnchorQuad2.Reset();
        }


        // Create Ray
        Vector3 rightRayOrigin = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        Vector3 rightRayDirection = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.forward;

        // Check if it intersects with Scene API elements
        if (MRUK.Instance.GetCurrentRoom().Raycast(new Ray(rightRayOrigin, rightRayDirection), float.MaxValue, out RaycastHit rHit, out MRUKAnchor rAnchor))
        {
            meshPreviewAnchor.transform.position = rHit.point;
            meshPreviewAnchor.transform.rotation = Quaternion.FromToRotation(Vector3.up, rHit.normal);
            if (rAnchor != null && OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) // MUST CREATE IN THIS ORDER: bottom left -> bottom right -> top left -> top right
            {
                Quaternion rotation = Quaternion.LookRotation(-rHit.normal);
                StartCoroutine(CreateSpatialAnchor(meshAnchorPrefab, rHit.point, rotation, (createdAnchor) =>
                {
                    meshAnchorQuad2.SetAnchor(createdAnchor);
                }));
            }
        }
        if (OVRInput.GetDown(OVRInput.Button.Two)) // create mesh
        {
            ResizeAndPositionMesh();
            meshAnchorQuad2.Reset();
        }


        if (OVRInput.GetDown(OVRInput.Button.Three)) // save all surfaces
        {
            SaveAllSurfaces();
        }

        if (OVRInput.GetDown(OVRInput.Button.Four)) // erase most recent
        {
            AnchorQuad2.EraseRecentAnchor();
        }

    }

    public void Initialized()
    {

        isInitialized = true;
        if(textPath == "") textPath = Application.persistentDataPath + "/savedSurfaces.txt";
        // Load anchors
        LoadSurfaces();
    }

    private void ResizeAndPositionMesh()
    {
        Log("ResizeAndPositionMesh");
        if(meshAnchorQuad2.anchorList.Count != 3)
        {
            LogError("Too many or too few anchors to create and resize mesh");
            return;
        }
        meshObject = Instantiate(meshPrefab);

        Log(meshObject.transform.GetChild(0).childCount + "Child count for meshObject");

        GameObject RefCube1 = meshAnchorQuad2.anchorList[0].gameObject;
        GameObject RefCube2 = meshAnchorQuad2.anchorList[1].gameObject;
        GameObject RefCube3 = meshAnchorQuad2.anchorList[2].gameObject;
        Log("1");
        GameObject DeskCube1 = meshObject.transform.GetChild(0).GetChild(0).gameObject;
        GameObject DeskCube2 = meshObject.transform.GetChild(0).GetChild(1).gameObject;
        GameObject DeskCube3 = meshObject.transform.GetChild(0).GetChild(2).gameObject;
        Log("2");
        RefCube1.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.red);
        RefCube2.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.green);
        RefCube3.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.blue);
        Log("2-2");
        DeskCube1.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.red);
        DeskCube2.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.green);
        DeskCube3.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.blue);

        Log("3");
        Vector3 RefPos1 = RefCube1.GetComponent<Transform>().position;
        Vector3 RefPos2 = RefCube2.GetComponent<Transform>().position;
        Vector3 RefPos3 = RefCube3.GetComponent<Transform>().position;
        Log("4");
        Vector3 DeskPos1 = DeskCube1.GetComponent<Transform>().position;
        Vector3 DeskPos2 = DeskCube2.GetComponent<Transform>().position;
        Vector3 DeskPos3 = DeskCube3.GetComponent<Transform>().position;
        Log("5");
        // Match scale
        float refWidth = Mathf.Abs(RefPos1.x - RefPos2.x);
        float refHeight = Mathf.Abs(RefPos1.y - RefPos3.y);
        float refDepth = Mathf.Abs(RefPos1.z - RefPos3.z);
        float deskWidth = Mathf.Abs(DeskPos1.x - DeskPos2.x);
        float deskHeight = Mathf.Abs(DeskPos1.y - DeskPos3.y);
        float deskDepth = Mathf.Abs(DeskPos1.z - DeskPos3.z);

        Log(refWidth + ", " + refHeight + ", " + refDepth);
        Log(deskWidth + ", " + deskHeight + ", " + deskDepth);


        meshObject.transform.localScale = new Vector3(refWidth / deskWidth, refHeight / deskHeight, refDepth / deskDepth);

        // Update position to the middle cube, but put it a bit in front and on top. Define bit as half the size of the arucoMarker (one of the RefCubes)
        meshObject.transform.position = RefPos1;

        Log("Position: " + RefPos1);

        // Compute direction vectors
        Vector3 refDirection = (RefPos2 - RefPos1).normalized;  // Desired direction
        Vector3 deskDirection = (DeskPos2 - DeskPos1).normalized;  // Current direction

        // Compute rotation needed to align deskDirection with refDirection
        Quaternion rotationCorrection = Quaternion.FromToRotation(deskDirection, refDirection);

        // Apply the rotation while keeping DeskCube1 fixed
        meshObject.transform.rotation = rotationCorrection * meshObject.transform.rotation;

    }
    private async void LoadSurfaces()
    {
        List<List<Guid>> collection = LoadSurfacesFromText();
        
        foreach (var guidList in collection)
        {
            await LoadAnchorsByUuid(guidList);
        }
    }

    private async Task LoadAnchorsByUuid(List<Guid> guids)
    {
        List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new();
        var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(guids, _unboundAnchors);
        if (result.Success)
        {
            Log("Loaded anchors successfully!");
            foreach(var unbouncAnchor in result.Value)
            {
                unbouncAnchor.LocalizeAsync().ContinueWith((success) =>
                {
                    if (success)
                    {
                        var spatialAnchor = Instantiate(anchorPrefab).AddComponent<OVRSpatialAnchor>();
                        spatialAnchor.name = $"Anchor {unbouncAnchor.Uuid}";
                        unbouncAnchor.BindTo(spatialAnchor);
                    }
                    else
                    {
                        Debug.LogError($"HYUNSOO failed to localise {unbouncAnchor.Uuid} anchor");
                        Log("Failed to localise anchor");
                    }
                });
            }
        }
        else
        {
            Log($"Failed to load unbound anchors {result.Status}");
        }
        Log("HYUNSOO - 7");
    }

    private void CreateSurface()
    {
        Log($"Creating Surface - AnchorQuad2 number of anchors: {AnchorQuad2.anchorList.Count}");
        if (!AnchorQuad2.isValid())
        {
            Log($"Number of anchors is not four! {AnchorQuad2.anchorList.Count}");
            return;
        }
        GameObject surface = CreateQuad(0.4f, 0.8f);
        var cpy = CopyAnchorQuad2(AnchorQuad2);
        mySurfaces.Add(cpy);
        Log($"Created Surface");
    }

    private AnchorQuad2 CopyAnchorQuad2(AnchorQuad2 src)
    {
        AnchorQuad2 dst = new AnchorQuad2();
        for(int i=0; i<src.anchorList.Count; i++)
        {
            dst.anchorList.Add(src.anchorList[i]);
            dst.guidList.Add(src.guidList[i]);
        }
        return dst;
    }

    private GameObject CreateQuad(float width, float height)
    {
        GameObject obj = new GameObject();
        MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = color;

        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();

        
        Vector3[] vertices = new Vector3[4];
        int idx = 0;
        AnchorQuad2.anchorList.ForEach(anchor =>
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
        Log("Created Quad");
        return obj;
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

    private async void SaveAllSurfaces()
    {
        List<List<string>> surfaceCollection = new List<List<string>>();
        for(int i=0; i<mySurfaces.Count; i++)
        {
            // Save the anchors
            await SaveSurfaceAnchors(mySurfaces[i].anchorList);

            // Then save each surface as JSON
            surfaceCollection.Add(mySurfaces[i].guidList.ConvertAll(g => g.ToString()));
        }
        SaveSurfacesAsText(surfaceCollection);
    }

    private async Task SaveSurfaceAnchors(List<OVRSpatialAnchor> anchors)
    {
        var result = await OVRSpatialAnchor.SaveAnchorsAsync(anchors);
        if (result.Success)
        {
            Log($"4. Anchors saved successfully.");
        }
        else
        {
            LogError($"Failed to save {anchors.Count} anchor(s) with error {result.Status}");
        }
    }


    // 🔹 Save surfaces using a custom text format
    private void SaveSurfacesAsText(List<List<string>> surfaces)
    {
        Log("Saving surfaces as custom text format...");
        using (StreamWriter writer = new StreamWriter(textPath))
        {
            foreach (var surface in surfaces)
            {
                writer.WriteLine("SURFACE_START");
                foreach (var guid in surface)
                {
                    writer.WriteLine(guid);
                }
                writer.WriteLine("SURFACE_END");
            }
        }
        Log($"Saved surfaces to {textPath}");
    }

    // 🔹 Load surfaces from a custom text format
    private List<List<Guid>> LoadSurfacesFromText()
    {
        Log($"Loading surfaces from custom text format at {textPath}");

        if (!File.Exists(textPath))
        {
            Log("No saved surfaces found.");
            return new List<List<Guid>>();
        }

        string[] lines = File.ReadAllLines(textPath);
        List<List<Guid>> surfaces = new List<List<Guid>>();
        List<Guid> currentSurface = null;

        foreach (string line in lines)
        {
            if (line == "SURFACE_START")
            {
                currentSurface = new List<Guid>();
            }
            else if (line == "SURFACE_END" && currentSurface != null)
            {
                surfaces.Add(currentSurface);
                currentSurface = null;
            }
            else if (currentSurface != null)
            {
                currentSurface.Add(Guid.Parse(line));
            }
        }

        Log("Loaded surfaces successfully.");
        return surfaces;
    }


    // Button Handlers
    public void PositionBins()
    {
        Log("PositionBins clicked");
    }
    public void BringSavedBins()
    {
        Log("BringSavedBins clicked");
    }

    public void RandomBins()
    {
        Log("RandomBins clicked");
    }




    void Log(string str)
    {
        console.text += "HYUNSOO " + str + "\n";
        Debug.Log("HYUNSOO " + str);
    }

    void LogError(string str)
    {
        console.text += "HYUNSOO " + str + "\n";
        Debug.LogError("HYUNSOO " + str);
    }
}
