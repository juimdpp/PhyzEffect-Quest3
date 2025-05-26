using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using TMPro;
using System;
using System.IO;

class AnchorList
{
    public List<OVRSpatialAnchor> anchorList;
    public List<Guid> guidList;

    public AnchorList()
    {
        anchorList = new List<OVRSpatialAnchor>();
        guidList = new List<Guid>();
    }
    public void AddAnchor(OVRSpatialAnchor anchor)
    {
        anchorList.Add(anchor);
        guidList.Add(anchor.Uuid);
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
    private List<AnchorList> myObjects;
    private AnchorList BinsAnchorList;
    private AnchorList objectAnchorQuad;
    private AnchorList carpetAnchorQuad;
    private AnchorList yogamatAnchorQuad;
    private string textPath = "";

    private enum PlayModes
    {
        EditPositionMode,
        PlayMode,
        DigitalTwinMode
    };
    private PlayModes currMode;

    // Start is called before the first frame update
    void Start()
    {
        visualizer.isMenuVisible = false;

        BinsAnchorList = new AnchorList();
        BinsAnchorList.Reset();

        anchorPreviewPrefab.transform.localScale = new Vector3(scale, scale, scale);
        anchorPrefab.transform.localScale = new Vector3(scale, scale, scale);
        previewAnchor = Instantiate(anchorPreviewPrefab);


        myObjects = new List<AnchorList>();
        textPath = Application.persistentDataPath + "/savedBins.txt";

        meshPreviewAnchor = Instantiate(meshAnchorPreviewPrefab);
        objectAnchorQuad = new AnchorList();
        objectAnchorQuad.Reset();

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

        if (currMode == PlayModes.EditPositionMode)
        {
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
                        BinsAnchorList.AddAnchor(createdAnchor);
                    }));
                }
            }

            if (OVRInput.GetDown(OVRInput.Button.Three)) // save all bins
            {
                SaveAnchorList(BinsAnchorList, "BINS");
            }

            if (OVRInput.GetDown(OVRInput.Button.Four)) // erase most recent bin
            {
                BinsAnchorList.EraseRecentAnchor();
            }
        }

        else if(currMode == PlayModes.DigitalTwinMode)
        {
            // Create Ray
            Vector3 leftRayOrigin = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            Vector3 leftRayDirection = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch) * Vector3.forward;

            // Check if it intersects with Scene API elements
            if (MRUK.Instance.GetCurrentRoom().Raycast(new Ray(leftRayOrigin, leftRayDirection), float.MaxValue, out RaycastHit lHit, out MRUKAnchor lAnchor))
            {
                meshPreviewAnchor.transform.position = lHit.point + lHit.normal * (objectHeight / 2f);
                meshPreviewAnchor.transform.rotation = Quaternion.FromToRotation(Vector3.up, lHit.normal);
                if (lAnchor != null && OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) // MUST CREATE IN THIS ORDER: bottom left -> bottom right -> top left -> top right
                {
                    Quaternion rotation = Quaternion.identity; // Quaternion.LookRotation(-lHit.normal);
                    StartCoroutine(CreateSpatialAnchor(meshAnchorPrefab, lHit.point + lHit.normal * (objectHeight / 2f), rotation, (createdAnchor) =>
                    {
                        objectAnchorQuad.AddAnchor(createdAnchor);
                    }));
                }
            }

            if (OVRInput.GetDown(OVRInput.Button.Two)) // position sofa
            {
                switch ((ObjectTypes)visualizer.objectType.value)
                {
                    case (ObjectTypes.SOFA):
                        ResizeAndPositionMesh();
                        SaveAnchorList(objectAnchorQuad, ObjectTypes.SOFA.ToString());
                        objectAnchorQuad.Reset();
                        break;
                    case (ObjectTypes.CARPET):
                        Log("CARPET");
                        SaveAnchorList(objectAnchorQuad, ObjectTypes.CARPET.ToString());
                        objectAnchorQuad.Reset();
                        break;
                    case (ObjectTypes.YOGA):
                        Log("YOGA");
                        SaveAnchorList(objectAnchorQuad, ObjectTypes.YOGA.ToString());
                        objectAnchorQuad.Reset();
                        break;

                }
            }


            if (OVRInput.GetDown(OVRInput.Button.Four)) // erase most recent bin
            {
                objectAnchorQuad.EraseRecentAnchor();
            }
        }

        else // PlayMode
        {
            // TODO: throw instead of shoot
            if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
            {
                GameObject ball = Instantiate(meshAnchorPrefab, OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch), OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch));
                Rigidbody rb = ball.GetComponent<Rigidbody>();
                Vector3 throwDirection = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.forward;
                rb.velocity = ball.transform.forward * 5f;
                rb.useGravity = true;
                rb.isKinematic = false;
            }
        }

    }

    public void Initialized()
    {

        isInitialized = true;
        if(textPath == "") textPath = Application.persistentDataPath + "/savedBins.txt";
        // Load anchors
        LoadBins();
    }

    private async void LoadBins()
    {
        List<List<Guid>> collection = LoadBinsFromText();
        
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


    private IEnumerator CreateSpatialAnchor(GameObject anchorPrefab, Vector3 position, Quaternion rotation, Action<OVRSpatialAnchor> callback)
    {
        GameObject prefab = Instantiate(anchorPrefab, position, rotation);
        var anchor = prefab.AddComponent<OVRSpatialAnchor>();

        // Wait for the async creation
        yield return new WaitUntil(() => anchor.Created);

        Log($"Created anchor {anchor.Uuid} at {position}");

        callback.Invoke(anchor);
    }

    private void ResizeAndPositionMesh()
    {
        Log("ResizeAndPositionMesh");
        if (objectAnchorQuad.anchorList.Count != 3)
        {
            LogError("Too many or too few anchors to create and resize mesh");
            return;
        }
        meshObject = Instantiate(meshPrefab);

        Log(meshObject.transform.GetChild(0).childCount + "Child count for meshObject");

        GameObject RefCube1 = objectAnchorQuad.anchorList[0].gameObject;
        GameObject RefCube2 = objectAnchorQuad.anchorList[1].gameObject;
        GameObject RefCube3 = objectAnchorQuad.anchorList[2].gameObject;

        GameObject DeskCube1 = meshObject.transform.GetChild(0).GetChild(0).gameObject;
        GameObject DeskCube2 = meshObject.transform.GetChild(0).GetChild(1).gameObject;
        GameObject DeskCube3 = meshObject.transform.GetChild(0).GetChild(2).gameObject;

        RefCube1.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.red);
        RefCube2.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.green);
        RefCube3.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.blue);

        DeskCube1.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.red);
        DeskCube2.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.green);
        DeskCube3.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", Color.blue);

        Vector3 RefPos1 = RefCube1.GetComponent<Transform>().position;
        Vector3 RefPos2 = RefCube2.GetComponent<Transform>().position;
        Vector3 RefPos3 = RefCube3.GetComponent<Transform>().position;
        
        Vector3 DeskPos1 = DeskCube1.GetComponent<Transform>().position;
        Vector3 DeskPos2 = DeskCube2.GetComponent<Transform>().position;
        Vector3 DeskPos3 = DeskCube3.GetComponent<Transform>().position;
        
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

    private async void SaveAnchorList(AnchorList anchorList, string tag)
    {
        List<string> surfaceCollection = new List<string>();
        
        // Save the anchors
        await SaveSurfaceAnchors(anchorList.anchorList);

        // Then save each surface as JSON
        surfaceCollection = anchorList.guidList.ConvertAll(g => g.ToString());
        SaveAnchorListAsText(surfaceCollection, tag);
    }

    private async Task SaveSurfaceAnchors(List<OVRSpatialAnchor> anchors)
    {
        var result = await OVRSpatialAnchor.SaveAnchorsAsync(anchors);
        if (result.Success)
        {
            Log($"Anchors saved successfully.");
        }
        else
        {
            LogError($"Failed to save {anchors.Count} anchor(s) with error {result.Status}");
        }
    }


    private void SaveAnchorListAsText(List<string> bins, string tag)
    {
        using (StreamWriter writer = new StreamWriter(textPath))
        {
            
            writer.WriteLine(tag + "_START");
            foreach (var guid in bins)
            {
                writer.WriteLine(guid);
            }
            writer.WriteLine(tag + "_END");
            
        }
        Log($"Saved anchors to {textPath}");
    }

    // 🔹 Load bins from a custom text format
    private List<List<Guid>> LoadBinsFromText()
    {
        Log($"Loading bins from custom text format at {textPath}");

        if (!File.Exists(textPath))
        {
            Log("No saved bins found.");
            return new List<List<Guid>>();
        }

        string[] lines = File.ReadAllLines(textPath);
        List<List<Guid>> bins = new List<List<Guid>>();
        List<Guid> currentSurface = null;

        foreach (string line in lines)
        {
            if (line == "SURFACE_START")
            {
                currentSurface = new List<Guid>();
            }
            else if (line == "SURFACE_END" && currentSurface != null)
            {
                bins.Add(currentSurface);
                currentSurface = null;
            }
            else if (currentSurface != null)
            {
                currentSurface.Add(Guid.Parse(line));
            }
        }

        Log("Loaded bins successfully.");
        return bins;
    }


    // Button Handlers
    public void PositionBins()
    {
        Log("PositionBins clicked");
        currMode = PlayModes.EditPositionMode;
    }
    public void BringSavedBins()
    {
        Log("BringSavedBins clicked");
        currMode = PlayModes.PlayMode;
        // TODO: trigger LoadSavedBins
    }

    public void RandomBins()
    {
        Log("RandomBins clicked");
        currMode = PlayModes.PlayMode;
        // TODO: trigger randomPositionBins
    }
    public void DigitalTwin()
    {
        Log("DigitalTwin mode activated");
        currMode = PlayModes.DigitalTwinMode;
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
