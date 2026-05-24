using UnityEngine;

public class BezierPatchManager : MonoBehaviour
{
    public Material patchMaterial;
    [Header("Sphere Instancing Configuration")]
    public Mesh sphereMesh;          
    public Material sphereMaterial;  
    [Range(0.05f, 0.5f)]
    public float sphereScale = 0.15f; 

    [Header("Control Net Line Configuration")]
    public Material lineMaterial; // Assign your Custom/ControlNetLine-Instanced shader material here

    [Header("Grid Layout")]
    public int gridWidth = 10;
    public int gridLength = 10;
    public float spacing = 4.5f; 

    private Mesh basePatchMesh;
    private Mesh combinedSphereGridMesh; 
    private Mesh combinedLineGridMesh; // New: Combined line grid topology
    private Matrix4x4[] instancedMatrices;
    private RenderParams renderParams;
    private RenderParams sphereRenderParams; 
    private RenderParams lineRenderParams; // New: Render parameters for lines

    void Start()
    {
        BuildBaseMesh();
        BuildInstanceMatrices();
        BuildCombinedSphereMesh();
        BuildCombinedLineMesh(); // Build the network wireframe
        
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

        // Initialize Line render configuration
        lineRenderParams = new RenderParams(lineMaterial)
        {
            receiveShadows = false,
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
            worldBounds = cullingBounds
        };
    }

    void Update()
    {
        if (instancedMatrices == null || instancedMatrices.Length == 0 || basePatchMesh == null || patchMaterial == null) 
            return;

        // 1. Render Surfaces
        Graphics.RenderMeshInstanced(renderParams, basePatchMesh, 0, instancedMatrices);

        // 2. Render Rigid/Deforming Control Spheres
        if (combinedSphereGridMesh != null && sphereMaterial != null)
        {
            Graphics.RenderMeshInstanced(sphereRenderParams, combinedSphereGridMesh, 0, instancedMatrices);
        }

        // 3. Render Connecting Control Lines
        if (combinedLineGridMesh != null && lineMaterial != null)
        {
            Graphics.RenderMeshInstanced(lineRenderParams, combinedLineGridMesh, 0, instancedMatrices);
        }
    }

    void BuildCombinedLineMesh()
    {
        // 16 points form a 4x4 structural grid
        Vector3[] lineVertices = new Vector3[16];
        
        // Generate the 16 local vertex points exactly matching the control structure
        for (int z = 0; z < 4; z++)
        {
            for (int x = 0; x < 4; x++)
            {
                int index = z * 4 + x;
                float xPos = (float)x * 1.5f - 2.25f;
                float zPos = (float)z * 1.5f - 2.25f;
                lineVertices[index] = new Vector3(xPos, 0f, zPos);
            }
        }

        // Calculate the indices needed for a grid layout using standard Lines topology
        // 4 rows * 3 segments = 12 horizontal segments (24 indices)
        // 4 cols * 3 segments = 12 vertical segments (24 indices)
        int[] lineIndices = new int[48];
        int idx = 0;

        // Horizontal connecting lines (along X axis)
        for (int z = 0; z < 4; z++)
        {
            for (int x = 0; x < 3; x++)
            {
                lineIndices[idx++] = z * 4 + x;
                lineIndices[idx++] = z * 4 + (x + 1);
            }
        }

        // Vertical connecting lines (along Z axis)
        for (int x = 0; x < 4; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                lineIndices[idx++] = z * 4 + x;
                lineIndices[idx++] = (z + 1) * 4 + x;
            }
        }

        combinedLineGridMesh = new Mesh();
        combinedLineGridMesh.name = "Control_Net_Lines_Mesh";
        combinedLineGridMesh.vertices = lineVertices;
        
        // Inform Unity to treat this mesh topology explicitly as separate lines
        combinedLineGridMesh.SetIndices(lineIndices, MeshTopology.Lines, 0);
        combinedLineGridMesh.RecalculateBounds();
    }

    void BuildCombinedSphereMesh()
    {
        if (sphereMesh == null) return;

        Vector3[] sourceVertices = sphereMesh.vertices;
        int[] sourceTriangles = sphereMesh.triangles;

        int totalVerts = sourceVertices.Length * 16;
        int totalTris = sourceTriangles.Length * 16;

        Vector3[] combinedVertices = new Vector3[totalVerts];
        Vector2[] combinedUV2s = new Vector2[totalVerts]; 
        int[] combinedTriangles = new int[totalTris];

        int vertOffset = 0;
        int triOffset = 0;

        for (int i = 0; i < 16; i++)
        {
            int cx = i % 4;
            int cz = i / 4;
            
            float xCenter = (float)cx * 1.5f - 2.25f;
            float zCenter = (float)cz * 1.5f - 2.25f;
            Vector3 sphereCenter = new Vector3(xCenter, 0, zCenter);

            for (int v = 0; v < sourceVertices.Length; v++)
            {
                int currentVertIndex = vertOffset + v;
                combinedVertices[currentVertIndex] = sphereCenter + (sourceVertices[v] * sphereScale);
                combinedUV2s[currentVertIndex] = new Vector2(xCenter, zCenter);
            }

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