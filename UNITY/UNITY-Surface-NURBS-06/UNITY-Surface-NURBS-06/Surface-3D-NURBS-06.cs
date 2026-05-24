using UnityEngine;
using System.Collections.Generic;

public class BezierPatchManager : MonoBehaviour
{
    public Material patchMaterial;
    public Material controlPointMaterial;
    public Material lineHullMaterial;     // New: Material for your control net lines (e.g., neon amber/green)
    
    public Mesh sphereMesh;
    public int gridWidth = 10;
    public int gridLength = 10;
    public float spacing = 4.5f; 

    private Mesh basePatchMesh;
    private Mesh controlNetLineMesh;      // New: Mesh containing the 24 line segments
    private Matrix4x4[] patchMatrices;
    private List<Matrix4x4[]> controlPointBatches = new List<Matrix4x4[]>();
    
    private RenderParams patchRenderParams;
    private RenderParams sphereRenderParams;
    private RenderParams lineRenderParams; // New: Render parameters for lines

    void Start()
    {
        BuildBaseMesh();
        BuildControlNetLineMesh(); // Build the line wireframe architecture
        BuildInstanceMatrices();
        
        float totalWidth = gridWidth * spacing;
        float totalLength = gridLength * spacing;
        Bounds cullingBounds = new Bounds(
            transform.position + new Vector3(totalWidth * 0.5f, 0, totalLength * 0.5f), 
            new Vector3(totalWidth + 50f, 100f, totalLength + 50f)
        );

        patchRenderParams = new RenderParams(patchMaterial) { receiveShadows = true, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On, worldBounds = cullingBounds };
        sphereRenderParams = new RenderParams(controlPointMaterial) { receiveShadows = true, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On, worldBounds = cullingBounds };
        
        // Configure line rendering parameters cleanly
        lineRenderParams = new RenderParams(lineHullMaterial)
        {
            receiveShadows = false,
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
            worldBounds = cullingBounds
        };
    }

    void Update()
    {
        // 1. Render Patches
        if (patchMatrices != null && patchMatrices.Length > 0 && basePatchMesh != null)
            Graphics.RenderMeshInstanced(patchRenderParams, basePatchMesh, 0, patchMatrices);

        // 2. Render Control Net Lines (Matches patch count exactly, fits in 1023 budget!)
        if (patchMatrices != null && patchMatrices.Length > 0 && controlNetLineMesh != null && lineHullMaterial != null)
            Graphics.RenderMeshInstanced(lineRenderParams, controlNetLineMesh, 0, patchMatrices);

        // 3. Render Control Point Spheres
        if (sphereMesh != null && controlPointMaterial != null)
        {
            for (int i = 0; i < controlPointBatches.Count; i++)
                Graphics.RenderMeshInstanced(sphereRenderParams, sphereMesh, 0, controlPointBatches[i]);
        }
    }

    void BuildBaseMesh()
    {
        basePatchMesh = new Mesh();
        basePatchMesh.name = "Shared_Instanced_Base_Patch";
        basePatchMesh.vertices = new Vector3[4] { new Vector3(-2.25f, 0, -2.25f), new Vector3(2.25f, 0, -2.25f), new Vector3(2.25f, 0, 2.25f), new Vector3(-2.25f, 0, 2.25f) };
        basePatchMesh.uv = new Vector2[4] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
        basePatchMesh.SetIndices(new int[4] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
    }

    // Generates a 4x4 grid framework represented purely by lines
    void BuildControlNetLineMesh()
    {
        controlNetLineMesh = new Mesh();
        controlNetLineMesh.name = "ControlNet_LineHull";

        Vector3[] vertices = new Vector3[16];
        List<int> indices = new List<int>();

        // 1. Populate the 16 local vertex spaces exactly mirroring your shader math
        int index = 0;
        for (int z = 0; z < 4; z++)
        {
            for (int x = 0; x < 4; x++)
            {
                vertices[index] = new Vector3(x * 1.5f - 2.25f, 0f, z * 1.5f - 2.25f);
                index++;
            }
        }

        // 2. Map row connections (Horizontal Lines)
        for (int z = 0; z < 4; z++)
        {
            for (int x = 0; x < 3; x++)
            {
                int current = z * 4 + x;
                int next = current + 1;
                indices.Add(current);
                indices.Add(next);
            }
        }

        // 3. Map column connections (Vertical Lines)
        for (int x = 0; x < 4; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                int current = z * 4 + x;
                int next = current + 4;
                indices.Add(current);
                indices.Add(next);
            }
        }

        controlNetLineMesh.vertices = vertices;
        // Crucial: Use MeshTopology.Lines so the GPU knows to render single-pixel primitives
        controlNetLineMesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
    }

    void BuildInstanceMatrices()
    {
        int patchCount = gridWidth * gridLength;
        if (patchCount > 1023) patchCount = 1023; 

        patchMatrices = new Matrix4x4[patchCount];
        List<Matrix4x4> allSphereMatrices = new List<Matrix4x4>();

        int patchIndex = 0;
        for (int z = 0; z < gridLength; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (patchIndex >= patchCount) break;

                Vector3 patchPosition = new Vector3(x * spacing, 0, z * spacing) + transform.position;
                patchMatrices[patchIndex] = Matrix4x4.TRS(patchPosition, Quaternion.identity, Vector3.one);

                for (int cpZ = 0; cpZ < 4; cpZ++)
                {
                    for (int cpX = 0; cpX < 4; cpX++)
                    {
                        Vector3 localCpPos = new Vector3(cpX * 1.5f - 2.25f, 0f, cpZ * 1.5f - 2.25f);
                        Vector3 worldCpPos = patchPosition + localCpPos;
                        allSphereMatrices.Add(Matrix4x4.TRS(worldCpPos, Quaternion.identity, Vector3.one * 0.15f));
                    }
                }
                patchIndex++;
            }
        }

        controlPointBatches.Clear();
        for (int i = 0; i < allSphereMatrices.Count; i += 1023)
        {
            int chunkSize = Mathf.Min(1023, allSphereMatrices.Count - i);
            Matrix4x4[] batch = new Matrix4x4[chunkSize];
            allSphereMatrices.CopyTo(i, batch, 0, chunkSize);
            controlPointBatches.Add(batch);
        }
    }
}