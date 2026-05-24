using UnityEngine;
using System.Collections.Generic;

public class BezierPatchManager : MonoBehaviour
{
    public Material patchMaterial;
    public Material controlPointMaterial; // Material for your neon spheres
    public Mesh sphereMesh;               // Assign standard Unity Sphere mesh via Inspector
    
    public int gridWidth = 10;
    public int gridLength = 10;
    public float spacing = 4.5f; 

    private Mesh basePatchMesh;
    private Matrix4x4[] patchMatrices;
    private List<Matrix4x4[]> controlPointBatches = new List<Matrix4x4[]>(); // Batches of max 1023
    
    private RenderParams patchRenderParams;
    private RenderParams sphereRenderParams;

    void Start()
    {
        BuildBaseMesh();
        BuildInstanceMatrices();
        
        float totalWidth = gridWidth * spacing;
        float totalLength = gridLength * spacing;
        Bounds cullingBounds = new Bounds(
            transform.position + new Vector3(totalWidth * 0.5f, 0, totalLength * 0.5f), 
            new Vector3(totalWidth + 50f, 100f, totalLength + 50f)
        );

        patchRenderParams = new RenderParams(patchMaterial)
        {
            receiveShadows = true,
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
            worldBounds = cullingBounds
        };

        sphereRenderParams = new RenderParams(controlPointMaterial)
        {
            receiveShadows = true,
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
            worldBounds = cullingBounds
        };
    }

    void Update()
    {
        // 1. Render Patches
        if (patchMatrices != null && patchMatrices.Length > 0 && basePatchMesh != null)
        {
            Graphics.RenderMeshInstanced(patchRenderParams, basePatchMesh, 0, patchMatrices);
        }

        // 2. Render Control Point Spheres (Loop through batches to bypass 1023 limitation)
        if (sphereMesh != null && controlPointMaterial != null)
        {
            for (int i = 0; i < controlPointBatches.Count; i++)
            {
                Graphics.RenderMeshInstanced(sphereRenderParams, sphereMesh, 0, controlPointBatches[i]);
            }
        }
    }

    void BuildBaseMesh()
    {
        basePatchMesh = new Mesh();
        basePatchMesh.name = "Shared_Instanced_Base_Patch";
        basePatchMesh.vertices = new Vector3[4] {
            new Vector3(-2.25f, 0, -2.25f), new Vector3(2.25f, 0, -2.25f),
            new Vector3(2.25f, 0, 2.25f),  new Vector3(-2.25f, 0, 2.25f)
        };
        basePatchMesh.uv = new Vector2[4] {
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
        };
        basePatchMesh.SetIndices(new int[4] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
    }

    void BuildInstanceMatrices()
    {
        int patchCount = gridWidth * gridLength;
        if (patchCount > 1023) patchCount = 1023; // Keeping your current clamp for patches

        patchMatrices = new Matrix4x4[patchCount];
        List<Matrix4x4> allSphereMatrices = new List<Matrix4x4>();

        int patchIndex = 0;
        for (int z = 0; z < gridLength; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (patchIndex >= patchCount) break;

                // Base patch location
                Vector3 patchPosition = new Vector3(x * spacing, 0, z * spacing) + transform.position;
                patchMatrices[patchIndex] = Matrix4x4.TRS(patchPosition, Quaternion.identity, Vector3.one);

                // Generate 4x4 control point positions for this specific patch
                for (int cpZ = 0; cpZ < 4; cpZ++)
                {
                    for (int cpX = 0; cpX < 4; cpX++)
                    {
                        // Mirror the exact mathematical logic inside your Domain Shader:
                        // cpPos = float3((float)x * 1.5f - 2.25f, 0.0f, (float)z * 1.5f - 2.25f);
                        Vector3 localCpPos = new Vector3(cpX * 1.5f - 2.25f, 0f, cpZ * 1.5f - 2.25f);
                        Vector3 worldCpPos = patchPosition + localCpPos;

                        // Give them a distinct visual scale (e.g., 0.15 unit spheres)
                        Matrix4x4 sphereMatrix = Matrix4x4.TRS(worldCpPos, Quaternion.identity, Vector3.one * 0.15f);
                        allSphereMatrices.Add(sphereMatrix);
                    }
                }
                patchIndex++;
            }
        }

        // Split sphere matrices into blocks of max 1023 to keep Graphics.RenderMeshInstanced happy
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