using UnityEngine;

[ExecuteInEditMode]
public class BezierPatchRenderer : MonoBehaviour
{
    private Mesh patchMesh;

    void OnEnable()
    {
        GenerateTessellationPatch();
    }

    void OnValidate()
    {
        GenerateTessellationPatch();
    }

    // void GenerateTessellationPatch()
    // {
    //     MeshFilter meshFilter = GetComponent<MeshFilter>();
    //     if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

    //     MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
    //     if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

    //     if (patchMesh == null)
    //     {
    //         patchMesh = new Mesh();
    //         patchMesh.name = "Tessellation_Base_Patch";
    //         patchMesh.hideFlags = HideFlags.DontSave;
    //     }
    //     else
    //     {
    //         patchMesh.Clear();
    //     }

    //     // For a Hull/Domain setup, we can pass a single Quad patch representing 
    //     // the normalized [0,1] UV domain space.
    //     Vector3[] vertices = new Vector3[4]
    //     {
    //         new Vector3(-2.25f, 0, -2.25f), // Bottom-Left
    //         new Vector3(2.25f, 0, -2.25f),  // Bottom-Right
    //         new Vector3(-2.25f, 0, 2.25f),  // Top-Left
    //         new Vector3(2.25f, 0, 2.25f)    // Top-Right
    //     };

    //     Vector2[] uvs = new Vector2[4]
    //     {
    //         new Vector2(0f, 0f),
    //         new Vector2(1f, 0f),
    //         new Vector2(0f, 1f),
    //         new Vector2(1f, 1f)
    //     };

    //     // We use a 4-point Quad layout. 
    //     // The Hull shader will interpret this patch type.
    //     int[] indices = new int[4] { 0, 1, 3, 2 }; 

    //     patchMesh.vertices = vertices;
    //     patchMesh.uv = uvs;
    //     patchMesh.SetIndices(indices, MeshTopology.Quads, 0);
    //     patchMesh.bounds = new Bounds(Vector3.zero, new Vector3(20, 20, 20));
        
    //     meshFilter.sharedMesh = patchMesh;
    // }

void GenerateTessellationPatch()
{
    MeshFilter meshFilter = GetComponent<MeshFilter>();
    if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

    MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
    if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

    if (patchMesh == null)
    {
        patchMesh = new Mesh();
        patchMesh.name = "Tessellation_Base_Patch";
        patchMesh.hideFlags = HideFlags.DontSave;
    }
    else
    {
        patchMesh.Clear();
    }

    // Realigned corners to prevent crossing diagonals
    Vector3[] vertices = new Vector3[4]
    {
        new Vector3(-2.25f, 0, -2.25f), // 0: Bottom-Left
        new Vector3(2.25f, 0, -2.25f),  // 1: Bottom-Right
        new Vector3(2.25f, 0, 2.25f),   // 2: Top-Right
        new Vector3(-2.25f, 0, 2.25f)   // 3: Top-Left
    };

    // UVs strictly mapped to match the spatial corners above
    Vector2[] uvs = new Vector2[4]
    {
        new Vector2(0f, 0f), // 0: Bottom-Left
        new Vector2(1f, 0f), // 1: Bottom-Right
        new Vector2(1f, 1f), // 2: Top-Right
        new Vector2(0f, 1f)  // 3: Top-Left
    };

    // Standard sequential Quad topology index (0 -> 1 -> 2 -> 3)
    int[] indices = new int[4] { 0, 1, 2, 3 }; 

    patchMesh.vertices = vertices;
    patchMesh.uv = uvs;
    patchMesh.SetIndices(indices, MeshTopology.Quads, 0);
    patchMesh.bounds = new Bounds(Vector3.zero, new Vector3(20, 20, 20));
    
    meshFilter.sharedMesh = patchMesh;
}



    void OnDisable()
    {
        if (patchMesh != null)
        {
            if (Application.isPlaying) Destroy(patchMesh);
            else DestroyImmediate(patchMesh);
        }
    }
}