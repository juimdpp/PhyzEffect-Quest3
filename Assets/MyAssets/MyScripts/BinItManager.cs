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
    private List<AnchorList> myBins;
    private AnchorList MyAnchorList;
    private AnchorList meshAnchorQuad2;
    private string textPath = "";

    private enum PlayModes
    {
        EditPositionMode,
        PlayMode
    };
    private PlayModes currMode;

    // Start is called before the first frame update
    void Start()
    {
        visualizer.isMenuVisible = false;

        MyAnchorList = new AnchorList();
        MyAnchorList.Reset();

        anchorPreviewPrefab.transform.localScale = new Vector3(scale, scale, scale);
        anchorPrefab.transform.localScale = new Vector3(scale, scale, scale);
        previewAnchor = Instantiate(anchorPreviewPrefab);
        

        myBins = new List<AnchorList>();
        textPath = Application.persistentDataPath + "/savedBins.txt";

        meshPreviewAnchor = Instantiate(meshAnchorPreviewPrefab);
        meshAnchorQuad2 = new AnchorList();
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
                        MyAnchorList.AddAnchor(createdAnchor);
                    }));
                }
            }

            if (OVRInput.GetDown(OVRInput.Button.Three)) // save all bins
            {
                SaveBins();
            }

            if (OVRInput.GetDown(OVRInput.Button.Four)) // erase most recent bin
            {
                MyAnchorList.EraseRecentAnchor();
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
                rb.AddForce(throwDirection * 5f, ForceMode.VelocityChange);
                rb.useGravity = true;
                rb.isKinematic = false
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

    private async void SaveBins()
    {
        List<List<string>> surfaceCollection = new List<List<string>>();
        for(int i=0; i<myBins.Count; i++)
        {
            // Save the anchors
            await SaveSurfaceAnchors(myBins[i].anchorList);

            // Then save each surface as JSON
            surfaceCollection.Add(myBins[i].guidList.ConvertAll(g => g.ToString()));
        }
        SaveBinsAsText(surfaceCollection);
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


    // 🔹 Save bins using a custom text format
    private void SaveBinsAsText(List<List<string>> bins)
    {
        Log("Saving bins as custom text format...");
        using (StreamWriter writer = new StreamWriter(textPath))
        {
            foreach (var bin in bins)
            {
                writer.WriteLine("SURFACE_START");
                foreach (var guid in bin)
                {
                    writer.WriteLine(guid);
                }
                writer.WriteLine("SURFACE_END");
            }
        }
        Log($"Saved bins to {textPath}");
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
