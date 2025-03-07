using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadCreator : MonoBehaviour
{
    public Material color;
    // Start is called before the first frame update
        void Start()
    {
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-2, 1, 0), // Bottom-left
            new Vector3(2, 1, 0),  // Bottom-right
            new Vector3(1, 0, 0),  // Top-left
            new Vector3(1, 2, 0)    // Top-right (different height)
        };

        mesh.triangles = new int[]
        {
            0, 2, 1, // First triangle
            1, 3, 0  // Second triangle
        };

        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshRenderer.material = color;

        gameObject.transform.position = new Vector3(0, 0, 0);
    }


// Update is called once per frame
void Update()
    {
        
    }
}
