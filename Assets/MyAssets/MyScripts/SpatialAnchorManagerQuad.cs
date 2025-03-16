using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using TMPro;
using System;
using System.IO;

class AnchorQuad
{
    public List<OVRSpatialAnchor> anchorList;
    public List<Guid> guidList;

    public AnchorQuad()
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


public class SpatialAnchorManagerQuad : MonoBehaviour
{
    public GameObject anchorPrefab;
    public GameObject anchorPreviewPrefab;
    public TMP_Text console;
    public Material color;

    private GameObject previewAnchor;
    private bool isInitialized = false;
    private List<AnchorQuad> mySurfaces;
    private AnchorQuad anchorQuad;
    private string textPath = "";

    // Start is called before the first frame update
    void Start()
    {
        anchorQuad = new AnchorQuad();
        anchorQuad.Reset();
        previewAnchor = Instantiate(anchorPreviewPrefab);
        mySurfaces = new List<AnchorQuad>();
        textPath = Application.persistentDataPath + "/savedSurfaces.txt";
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
            SaveAllSurfaces();
        }

        if (OVRInput.GetDown(OVRInput.Button.Four)) // erase most recent
        {
            anchorQuad.EraseRecentAnchor();
        }

    }

    public void Initialized()
    {

        isInitialized = true;
        if(textPath == "") textPath = Application.persistentDataPath + "/savedSurfaces.txt";
        // Load anchors
        LoadSurfaces();
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
        Log($"Creating Surface - anchorQuad number of anchors: {anchorQuad.anchorList.Count}");
        if (!anchorQuad.isValid())
        {
            Log($"Number of anchors is not four! {anchorQuad.anchorList.Count}");
            return;
        }
        GameObject surface = CreateQuad(0.4f, 0.8f);
        var cpy = CopyAnchorQuad(anchorQuad);
        mySurfaces.Add(cpy);
        Log($"Created Surface");
    }

    private AnchorQuad CopyAnchorQuad(AnchorQuad src)
    {
        AnchorQuad dst = new AnchorQuad();
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
