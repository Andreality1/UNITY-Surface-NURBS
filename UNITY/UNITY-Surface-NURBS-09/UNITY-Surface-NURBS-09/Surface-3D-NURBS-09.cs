using UnityEngine;

[ExecuteInEditMode]
public class BezierPatchRenderer : MonoBehaviour
{
    [Header("Surface Patch Configuration")]
    [SerializeField] private Material patchMaterial;
    private Mesh patchMesh;

    [Header("Control Point Visualization")]
    [SerializeField] private Mesh sphereMesh;
    [SerializeField] private Material sphereMaterial;
    [Range(0.05f, 0.5f)] [SerializeField] private float sphereScale = 0.15f;

    // 4x4 Grid cache arrays for GPU Instancing (16 points total)
    private Matrix4x4[] instancedMatrices = new Matrix4x4[16];
    private MaterialPropertyBlock propertyBlock;

    void OnEnable()
    {
        GenerateTessellationPatch();
        propertyBlock = new MaterialPropertyBlock();
    }

    void OnValidate()
    {
        GenerateTessellationPatch();
    }

    void Update()
    {
        RenderControlPoints();
    }

    void GenerateTessellationPatch()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Bind the surface shader material directly if assigned
        if (patchMaterial != null && meshRenderer.sharedMaterial != patchMaterial)
        {
            meshRenderer.sharedMaterial = patchMaterial;
        }

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

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-2.25f, 0, -2.25f), // 0: Bottom-Left
            new Vector3(2.25f, 0, -2.25f),  // 1: Bottom-Right
            new Vector3(2.25f, 0, 2.25f),   // 2: Top-Right
            new Vector3(-2.25f, 0, 2.25f)   // 3: Top-Left
        };

        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0f, 0f), 
            new Vector2(1f, 0f), 
            new Vector2(1f, 1f), 
            new Vector2(0f, 1f)  
        };

        int[] indices = new int[4] { 0, 1, 2, 3 }; 

        patchMesh.vertices = vertices;
        patchMesh.uv = uvs;
        patchMesh.SetIndices(indices, MeshTopology.Quads, 0);
        patchMesh.bounds = new Bounds(Vector3.zero, new Vector3(20, 20, 20));
        
        meshFilter.sharedMesh = patchMesh;
    }

    void RenderControlPoints()
    {
        if (sphereMesh == null || sphereMaterial == null) return;

        // Fetch shared wave properties directly from the surface material if available
        float waveSpeed = 2.5f;
        float waveAmp = 0.75f;
        if (patchMaterial != null)
        {
            waveSpeed = patchMaterial.GetFloat("_WaveSpeed");
            waveAmp = patchMaterial.GetFloat("_WaveAmp");
        }

        int index = 0;
        float time = Time.timeSinceLevelLoad; // Leverages identical timing to shader _Time.y
        Matrix4x4 localToWorld = transform.localToWorldMatrix;

        for (int z = 0; z < 4; ++z)
        {
            for (int x = 0; x < 4; ++x)
            {
                // Reconstruct exact raw control grid spacing matching the domain shader
                // Base grid coordinates step by 1.5 units from -2.25 to 2.25
                float rawX = (float)x * 1.5f - 2.25f;
                float rawZ = (float)z * 1.5f - 2.25f;
                float rawY = 0.0f;

                // Apply the wave formula to internal control points synchronously
                if (x > 0 && x < 3 && z > 0 && z < 3)
                {
                    rawY += Mathf.Sin(time * waveSpeed + (rawX * 1.5f)) * waveAmp;
                }

                // Transform local patch coordinates relative to the Parent Actor transform
                Vector3 localPos = new Vector3(rawX, rawY, rawZ);
                Vector3 worldPos = localToWorld.MultiplyPoint3x4(localPos);
                Vector3 worldScale = Vector3.Scale(transform.lossyScale, new Vector3(sphereScale, sphereScale, sphereScale));

                // Formulate the transformation matrix for this specific instance
                instancedMatrices[index] = Matrix4x4.TRS(worldPos, transform.rotation, worldScale);
                index++;
            }
        }

        // Issue a single instanced procedural GPU draw command
        Graphics.DrawMeshInstanced(
            sphereMesh, 
            0, 
            sphereMaterial, 
            instancedMatrices, 
            16, 
            propertyBlock, 
            UnityEngine.Rendering.ShadowCastingMode.On, 
            true, 
            gameObject.layer
        );
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