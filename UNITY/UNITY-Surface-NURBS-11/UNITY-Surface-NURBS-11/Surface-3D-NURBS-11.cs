using UnityEngine;

public class BezierPatchManager : MonoBehaviour
{
    public Material patchMaterial;
    [Header("Sphere Instancing Configuration")]
    public Mesh sphereMesh;          
    public Material sphereMaterial;  
    [Range(0.05f, 0.5f)]
    public float sphereScale = 0.15f; 

    [Header("Grid Layout")]
    public int gridWidth = 10;
    public int gridLength = 10;
    public float spacing = 4.5f; 

    private Mesh basePatchMesh;
    private Mesh combinedSphereGridMesh; 
    private Matrix4x4[] instancedMatrices;
    private RenderParams renderParams;
    private RenderParams sphereRenderParams; 

    void Start()
    {
        BuildBaseMesh();
        BuildInstanceMatrices();
        BuildCombinedSphereMesh();
        
        float totalWidth = gridWidth * spacing;
        float totalLength = gridLength * spacing;
        Bounds cullingBounds = new Bounds(
            transform.position + new Vector3(totalWidth * 0.5f, 0, totalLength * 0.5f), 
            new Vector3(totalWidth + 50f, 100f, totalLength + 50f)
        );

        renderParams = new RenderParams(patchMaterial)
        {
            receiveShadows = true,
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
            worldBounds = cullingBounds 
        };

        sphereRenderParams = new RenderParams(sphereMaterial)
        {
            receiveShadows = true,
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
            worldBounds = cullingBounds
        };
    }

    void Update()
    {
        if (instancedMatrices == null || instancedMatrices.Length == 0 || basePatchMesh == null || patchMaterial == null) 
            return;

        Graphics.RenderMeshInstanced(renderParams, basePatchMesh, 0, instancedMatrices);

        if (combinedSphereGridMesh != null && sphereMaterial != null)
        {
            Graphics.RenderMeshInstanced(sphereRenderParams, combinedSphereGridMesh, 0, instancedMatrices);
        }
    }

    void BuildCombinedSphereMesh()
    {
        if (sphereMesh == null) return;

        Vector3[] sourceVertices = sphereMesh.vertices;
        int[] sourceTriangles = sphereMesh.triangles;

        int totalVerts = sourceVertices.Length * 16;
        int totalTris = sourceTriangles.Length * 16;

        Vector3[] combinedVertices = new Vector3[totalVerts];
        Vector2[] combinedUV2s = new Vector2[totalVerts]; // Store sphere center anchors here
        int[] combinedTriangles = new int[totalTris];

        int vertOffset = 0;
        int triOffset = 0;

        for (int i = 0; i < 16; i++)
        {
            int cx = i % 4;
            int cz = i / 4;
            
            // Exact local control point anchors matching the patch structure
            float xCenter = (float)cx * 1.5f - 2.25f;
            float zCenter = (float)cz * 1.5f - 2.25f;
            Vector3 sphereCenter = new Vector3(xCenter, 0, zCenter);

            // Copy vertices and apply local offsets
            for (int v = 0; v < sourceVertices.Length; v++)
            {
                int currentVertIndex = vertOffset + v;
                // Scale the vertex first, then offset it to its control point position
                combinedVertices[currentVertIndex] = sphereCenter + (sourceVertices[v] * sphereScale);
                
                // Bake the center coordinate into UV2 (X and Z coordinates)
                combinedUV2s[currentVertIndex] = new Vector2(xCenter, zCenter);
            }

            // Copy indices safely
            for (int t = 0; t < sourceTriangles.Length; t++)
            {
                combinedTriangles[triOffset + t] = vertOffset + sourceTriangles[t];
            }

            vertOffset += sourceVertices.Length;
            triOffset += sourceTriangles.Length;
        }

        combinedSphereGridMesh = new Mesh();
        combinedSphereGridMesh.name = "Rigid_Control_Points_Grid";
        combinedSphereGridMesh.vertices = combinedVertices;
        combinedSphereGridMesh.uv2 = combinedUV2s; 
        combinedSphereGridMesh.triangles = combinedTriangles;
        
        combinedSphereGridMesh.RecalculateNormals();
        combinedSphereGridMesh.RecalculateBounds();
    }

    void BuildBaseMesh()
    {
        basePatchMesh = new Mesh();
        basePatchMesh.name = "Shared_Instanced_Base_Patch";

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-2.25f, 0, -2.25f),
            new Vector3(2.25f, 0, -2.25f),
            new Vector3(2.25f, 0, 2.25f),
            new Vector3(-2.25f, 0, 2.25f)
        };

        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        int[] indices = new int[4] { 0, 1, 2, 3 };

        basePatchMesh.vertices = vertices;
        basePatchMesh.uv = uvs;
        basePatchMesh.SetIndices(indices, MeshTopology.Quads, 0);
    }

    void BuildInstanceMatrices()
    {
        int count = gridWidth * gridLength;
        if (count > 1023) count = 1023; 

        instancedMatrices = new Matrix4x4[count];

        int index = 0;
        for (int z = 0; z < gridLength; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (index >= count) break;
                Vector3 position = new Vector3(x * spacing, 0, z * spacing) + transform.position;
                instancedMatrices[index] = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
                index++;
            }
        }
    }
}