using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum ObjectTypes
{
    SOFA,
    CARPET,
    YOGA
}
public class Visualizer : MonoBehaviour
{
    public GameObject MenuCanvas;
    public GameObject leftRay;
    public GameObject rightRay;
    public bool isMenuVisible;

    public TMP_Text text;
    public TMP_Dropdown objectType;

    // Start is called before the first frame update
    void Start()
    {
        isMenuVisible = false;
        MenuCanvas.SetActive(isMenuVisible);
        objectType.AddOptions(new List<string>(System.Enum.GetNames(typeof(ObjectTypes))));
    }

    // Update is called once per frame
    void Update()
    {
        if (isMenuVisible)
        {
            if (OVRInput.GetDown(OVRInput.Button.Start))
            {
                isMenuVisible = !isMenuVisible;
                MenuCanvas.SetActive(isMenuVisible);
            }
            leftRay.SetActive(true);
            rightRay.SetActive(true);
            return;
        }
        else
        {
            if (OVRInput.GetDown(OVRInput.Button.Start))
            {
                isMenuVisible = !isMenuVisible;
                MenuCanvas.SetActive(isMenuVisible);
            }

            leftRay.SetActive(false);
            rightRay.SetActive(false);
        }
    }
    public void Hello()
    {
        text.text = "hello";
    }

}
