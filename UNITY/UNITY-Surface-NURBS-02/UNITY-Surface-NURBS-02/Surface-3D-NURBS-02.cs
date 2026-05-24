using UnityEngine;

[ExecuteInEditMode]
public class BezierPatchRenderer : MonoBehaviour
{
    private Mesh patchMesh;
    [Range(4, 60)] public int resolution = 30; // Mesh density variable

    void OnEnable()
    {
        GenerateHighResGrid();
    }

    void OnValidate()
    {
        // Regenerate mesh if you tweak the resolution slider in the inspector
        GenerateHighResGrid();
    }

    void GenerateHighResGrid()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (patchMesh == null)
        {
            patchMesh = new Mesh();
            patchMesh.name = "Parametric_Evaluation_Grid";
            patchMesh.hideFlags = HideFlags.DontSave;
        }
        else
        {
            patchMesh.Clear();
        }

        int vertexCount = resolution * resolution;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        for (int z = 0; z < resolution; z++)
        {
            float normZ = (float)z / (resolution - 1);
            for (int x = 0; x < resolution; x++)
            {
                float normX = (float)x / (resolution - 1);
                int index = z * resolution + x;

                // Vertices start flat at zero. The Vertex Shader updates 
                // spatial positions using the parametric UV channels!
                vertices[index] = Vector3.zero;
                uvs[index] = new Vector2(normX, normZ);
            }
        }

        // Build index topology array
        int numQuads = (resolution - 1) * (resolution - 1);
        int[] triangles = new int[numQuads * 6];
        int triOffset = 0;

        for (int z = 0; z < resolution - 1; z++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                int row1 = z * resolution + x;
                int row2 = (z + 1) * resolution + x;

                triangles[triOffset++] = row1;
                triangles[triOffset++] = row2;
                triangles[triOffset++] = row1 + 1;

                triangles[triOffset++] = row1 + 1;
                triangles[triOffset++] = row2;
                triangles[triOffset++] = row2 + 1;
            }
        }

        patchMesh.vertices = vertices;
        patchMesh.uv = uvs;
        patchMesh.triangles = triangles;
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