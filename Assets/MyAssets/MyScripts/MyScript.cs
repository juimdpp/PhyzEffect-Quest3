using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using TMPro;
using Meta.XR.MRUtilityKit;

public class MyScript: MonoBehaviour
{
    public TMP_Text console; // Assign this in Unity Inspector

    async void Start()
    {
        if (console == null)
        {
            Debug.LogError("❌ TMP_Text console is not assigned!");
            return;
        }

        // Ensure MRUK exists
        if (MRUK.Instance == null)
        {
            AppendToConsole("❌ MRUK Instance not found! Ensure MRUK prefab is in the scene.");
            return;
        }

        // Check if user has granted Scene API permission
        if (!Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission))
        {
            AppendToConsole("🔄 Requesting Scene API permission...");
            Permission.RequestUserPermission(OVRPermissionsRequester.ScenePermission);
        }
        else
        {
            AppendToConsole("✅ Scene API permission granted! Loading Scene Model...");
            CallLoadScene();
        }
    }
    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            AppendToConsole("Pressed down");
        }
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            AppendToConsole("Pressed two down");
            CallLoadScene();
        }
    }

    async void OnPermissionGranted(bool granted)
    {
        if (granted)
        {
            AppendToConsole("✅ Scene permission granted! Loading Scene Model...");
            CallLoadScene();
        }
        else
        {
            AppendToConsole($"❌ Scene permission denied. Cannot load Scene Model.");
        }
    }
    async void CallLoadScene()
    {
        await LoadScene();
    }
    async Task LoadScene()
    {
        AppendToConsole("📡 Loading Scene Model from device...");

        // Load Scene Model from Quest device
        MRUK.LoadDeviceResult result = await MRUK.Instance.LoadSceneFromDevice(
            requestSceneCaptureIfNoDataFound: true // If no scene exists, prompt Space Setup
        );

        // Handle different loading results
        switch (result)
        {
            case MRUK.LoadDeviceResult.Success:
                AppendToConsole("✅ Scene Model loaded successfully!");
                MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
                break;


            default:
                AppendToConsole($"❌ Failed to load Scene Model. {result}");
                break;
        }
    }

    public void OnSceneLoaded()
    {
        AppendToConsole("🎉 Scene Model fully loaded and ready!");
    }

    void AppendToConsole(string message)
    {
        if (console != null)
        {
            console.text += "HYUNSOO: " + message + "\n";
            Debug.Log("HYUNSOO: " + message);
        }
    }
}
