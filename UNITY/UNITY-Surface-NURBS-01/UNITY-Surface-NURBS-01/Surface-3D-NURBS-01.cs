using UnityEngine;

[ExecuteInEditMode]
public class BezierPatchRenderer : MonoBehaviour
{
    private Mesh patchMesh;

    void OnEnable()
    {
        GenerateBaseGrid();
    }

    void GenerateBaseGrid()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (patchMesh == null)
        {
            patchMesh = new Mesh();
            patchMesh.name = "4x4_Initial_Grid";
            patchMesh.hideFlags = HideFlags.DontSave;
        }
        else
        {
            patchMesh.Clear();
        }

        // Initialize 16 Control Vertices
        Vector3[] vertices = new Vector3[16];
        Vector2[] uvs = new Vector2[16];

        for (int z = 0; z < 4; z++)
        {
            for (int x = 0; x < 4; x++)
            {
                int index = z * 4 + x;
                
                // Replicate exact matrix configuration calculations from vs3.hlsl
                float posX = (float)x * 1.5f - 2.25f;
                float posZ = (float)z * 1.5f - 2.25f;
                vertices[index] = new Vector3(posX, 0.0f, posZ);
                
                // Set normalized parametric coordinates
                uvs[index] = new Vector2((float)x / 3.0f, (float)z / 3.0f);
            }
        }

        // Create standard index layouts for a 3x3 quad-patch assembly using traditional triangles
        int[] triangles = new int[3 * 3 * 6]; 
        int triOffset = 0;

        for (int z = 0; z < 3; z++)
        {
            for (int x = 0; x < 3; x++)
            {
                int row1 = z * 4 + x;
                int row2 = (z + 1) * 4 + x;

                // Triangle 1
                triangles[triOffset++] = row1;
                triangles[triOffset++] = row2;
                triangles[triOffset++] = row1 + 1;

                // Triangle 2
                triangles[triOffset++] = row1 + 1;
                triangles[triOffset++] = row2;
                triangles[triOffset++] = row2 + 1;
            }
        }

        patchMesh.vertices = vertices;
        patchMesh.uv = uvs;
        patchMesh.triangles = triangles;

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