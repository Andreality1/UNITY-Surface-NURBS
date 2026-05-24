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
        // A 4x4 control grid contains 12 horizontal segments and 12 vertical segments = 24 segments total.
        // Each segment is now drawn as a quad (4 vertices, 6 indices).
        int totalSegments = 24;
        Vector3[] vertices = new Vector3[totalSegments * 4];
        Vector3[] directions = new Vector3[totalSegments * 4]; // Custom direction of the segment line
        Vector2[] sideOffsets = new Vector2[totalSegments * 4]; // Tells shader which side of the quad edge the vertex is (-1 or 1)
        int[] indices = new int[totalSegments * 6];

        int vertIdx = 0;
        int triIdx = 0;

        // Helper to generate a thickened segment between two points
        System.Action<Vector3, Vector3> MeshSegment = (pA, pB) => 
        {
            Vector3 dir = (pB - pA).normalized;

            // 4 corners of the quad ribbon tracking the segment line
            vertices[vertIdx + 0] = pA;
            vertices[vertIdx + 1] = pA;
            vertices[vertIdx + 2] = pB;
            vertices[vertIdx + 3] = pB;

            // Store the segment direction in normal channels so the vertex shader can see it
            directions[vertIdx + 0] = dir;
            directions[vertIdx + 1] = dir;
            directions[vertIdx + 2] = dir;
            directions[vertIdx + 3] = dir;

            // Side offsets: X is side expansion direction (-1 or 1). Y is interpolation factor along segment length.
            sideOffsets[vertIdx + 0] = new Vector2(-1f, 0f);
            sideOffsets[vertIdx + 1] = new Vector2( 1f, 0f);
            sideOffsets[vertIdx + 2] = new Vector2(-1f, 1f);
            sideOffsets[vertIdx + 3] = new Vector2( 1f, 1f);

            // Standard quad index layout (Two triangles per segment)
            indices[triIdx + 0] = vertIdx + 0;
            indices[triIdx + 1] = vertIdx + 2;
            indices[triIdx + 2] = vertIdx + 1;

            indices[triIdx + 3] = vertIdx + 1;
            indices[triIdx + 4] = vertIdx + 2;
            indices[triIdx + 5] = vertIdx + 3;

            vertIdx += 4;
            triIdx += 6;
        };

        // 1. Generate Horizontal segments (along X axis)
        for (int z = 0; z < 4; z++)
        {
            for (int x = 0; x < 3; x++)
            {
                Vector3 pA = new Vector3((float)x * 1.5f - 2.25f, 0f, (float)z * 1.5f - 2.25f);
                Vector3 pB = new Vector3((float)(x + 1) * 1.5f - 2.25f, 0f, (float)z * 1.5f - 2.25f);
                MeshSegment(pA, pB);
            }
        }

        // 2. Generate Vertical segments (along Z axis)
        for (int x = 0; x < 4; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                Vector3 pA = new Vector3((float)x * 1.5f - 2.25f, 0f, (float)z * 1.5f - 2.25f);
                Vector3 pB = new Vector3((float)x * 1.5f - 2.25f, 0f, (float)(z + 1) * 1.5f - 2.25f);
                MeshSegment(pA, pB);
            }
        }

        combinedLineGridMesh = new Mesh();
        combinedLineGridMesh.name = "Control_Net_Thick_Lines_Mesh";
        combinedLineGridMesh.vertices = vertices;
        combinedLineGridMesh.normals = directions; // Reusing normals channel to pass structural line directions cleanly
        combinedLineGridMesh.uv = sideOffsets;     // Reusing UV channel for side expansion attributes

        // Use Triangles topology so the shader can billboard it properly
        combinedLineGridMesh.SetIndices(indices, MeshTopology.Triangles, 0);
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