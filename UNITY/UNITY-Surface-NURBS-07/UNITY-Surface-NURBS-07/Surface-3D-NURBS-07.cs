using UnityEngine;
using System.Collections.Generic;

public class BezierPatchManager : MonoBehaviour
{
    public Material patchMaterial;
    public Material controlPointMaterial; // Uses the updated Sphere shader below
    public Material lineHullMaterial;     
    
    public Mesh sphereMesh;               // Keep this assigned just in case, but we will generate the grid mesh dynamically
    public int gridWidth = 10;
    public int gridLength = 10;
    public float spacing = 4.5f; 

    private Mesh basePatchMesh;
    private Mesh controlNetLineMesh;      
    private Mesh controlPointGridMesh;    // New: One mesh holding all 16 sphere locations
    private Matrix4x4[] patchMatrices;
    
    private RenderParams patchRenderParams;
    private RenderParams sphereRenderParams;
    private RenderParams lineRenderParams; 

    void Start()
    {
        BuildBaseMesh();
        BuildControlNetLineMesh();
        BuildControlPointGridMesh();      // Build the unified sphere layout framework
        BuildInstanceMatrices();
        
        float totalWidth = gridWidth * spacing;
        float totalLength = gridLength * spacing;
        Bounds cullingBounds = new Bounds(
            transform.position + new Vector3(totalWidth * 0.5f, 0, totalLength * 0.5f), 
            new Vector3(totalWidth + 50f, 100f, totalLength + 50f)
        );

        patchRenderParams = new RenderParams(patchMaterial) { receiveShadows = true, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On, worldBounds = cullingBounds };
        lineRenderParams = new RenderParams(lineHullMaterial) { receiveShadows = false, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off, worldBounds = cullingBounds };
        sphereRenderParams = new RenderParams(controlPointMaterial) { receiveShadows = true, shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On, worldBounds = cullingBounds };
    }

    void Update()
    {
        if (patchMatrices == null || patchMatrices.Length == 0) return;

        // 1. Render Patches
        if (basePatchMesh != null && patchMaterial != null)
            Graphics.RenderMeshInstanced(patchRenderParams, basePatchMesh, 0, patchMatrices);

        // 2. Render Control Net Lines
        if (controlNetLineMesh != null && lineHullMaterial != null)
            Graphics.RenderMeshInstanced(lineRenderParams, controlNetLineMesh, 0, patchMatrices);

        // 3. Render Control Point Spheres (Now matches the patch count and uses patch matrices!)
        if (controlPointGridMesh != null && controlPointMaterial != null)
            Graphics.RenderMeshInstanced(sphereRenderParams, controlPointGridMesh, 0, patchMatrices);
    }

    void BuildBaseMesh()
    {
        basePatchMesh = new Mesh();
        basePatchMesh.name = "Shared_Instanced_Base_Patch";
        basePatchMesh.vertices = new Vector3[4] { new Vector3(-2.25f, 0, -2.25f), new Vector3(2.25f, 0, -2.25f), new Vector3(2.25f, 0, 2.25f), new Vector3(-2.25f, 0, 2.25f) };
        basePatchMesh.uv = new Vector2[4] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
        basePatchMesh.SetIndices(new int[4] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
    }

    void BuildControlNetLineMesh()
    {
        controlNetLineMesh = new Mesh();
        controlNetLineMesh.name = "ControlNet_LineHull";
        Vector3[] vertices = new Vector3[16];
        List<int> indices = new List<int>();

        int index = 0;
        for (int z = 0; z < 4; z++) {
            for (int x = 0; x < 4; x++) {
                vertices[index] = new Vector3(x * 1.5f - 2.25f, 0f, z * 1.5f - 2.25f);
                index++;
            }
        }
        for (int z = 0; z < 4; z++) {
            for (int x = 0; x < 3; x++) {
                indices.Add(z * 4 + x); indices.Add(z * 4 + (x + 1));
            }
        }
        for (int x = 0; x < 4; x++) {
            for (int z = 0; z < 3; z++) {
                indices.Add(z * 4 + x); indices.Add((z + 1) * 4 + x);
            }
        }
        controlNetLineMesh.vertices = vertices;
        controlNetLineMesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
    }

    // Combines 16 distinct sub-mesh sphere instances into a single structural local patch layout
    void BuildControlPointGridMesh()
    {
        // If no reference sphere is explicitly provided, we fallback to a simple point topology 
        // to prevent hard engine crashes
        Mesh referenceMesh = sphereMesh != null ? sphereMesh : basePatchMesh; 
        
        CombineInstance[] combine = new CombineInstance[16];
        int index = 0;
        
        for (int z = 0; z < 4; z++)
        {
            for (int x = 0; x < 4; x++)
            {
                Vector3 localPosition = new Vector3(x * 1.5f - 2.25f, 0f, z * 1.5f - 2.25f);
                
                combine[index].mesh = referenceMesh;
                // Bake the scale (0.15f) and local placement offset directly into the mesh combination step
                combine[index].transform = Matrix4x4.TRS(localPosition, Quaternion.identity, Vector3.one * 0.15f);
                index++;
            }
        }

        controlPointGridMesh = new Mesh();
        controlPointGridMesh.name = "ControlPoint_GridMesh";
        controlPointGridMesh.CombineMeshes(combine, true, true);
    }

    void BuildInstanceMatrices()
    {
        int patchCount = gridWidth * gridLength;
        if (patchCount > 1023) patchCount = 1023; 

        patchMatrices = new Matrix4x4[patchCount];

        int patchIndex = 0;
        for (int z = 0; z < gridLength; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (patchIndex >= patchCount) break;

                Vector3 patchPosition = new Vector3(x * spacing, 0, z * spacing) + transform.position;
                patchMatrices[patchIndex] = Matrix4x4.TRS(patchPosition, Quaternion.identity, Vector3.one);
                patchIndex++;
            }
        }
    }
}