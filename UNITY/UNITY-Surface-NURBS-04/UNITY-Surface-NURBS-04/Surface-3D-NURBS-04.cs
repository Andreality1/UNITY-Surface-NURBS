using UnityEngine;

public class BezierPatchManager : MonoBehaviour
{
    public Material patchMaterial;
    public int gridWidth = 10;
    public int gridLength = 10;
    public float spacing = 4.5f; // Matches the physical size of your 4.5x4.5 patch

    private Mesh basePatchMesh;
    private Matrix4x4[] instancedMatrices;
    private RenderParams renderParams;

    void Start()
    {
        BuildBaseMesh();
        BuildInstanceMatrices();
        
        // Calculate a bounding box that encapsulates the entire patch grid
        float totalWidth = gridWidth * spacing;
        float totalLength = gridLength * spacing;
        Bounds cullingBounds = new Bounds(
            transform.position + new Vector3(totalWidth * 0.5f, 0, totalLength * 0.5f), 
            new Vector3(totalWidth + 50f, 100f, totalLength + 50f)
        );

        // Configure render parameters cleanly using the worldBounds property
        renderParams = new RenderParams(patchMaterial)
        {
            receiveShadows = true,
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
            worldBounds = cullingBounds // Bypasses the need to pass bounds as a function parameter
        };
    }

    void Update()
    {
        if (instancedMatrices == null || instancedMatrices.Length == 0 || basePatchMesh == null || patchMaterial == null) 
            return;

        // Clean, standard overload taking exactly 4 parameters: 
        // 1. RenderParams, 2. Mesh, 3. Submesh Index, 4. Matrix Array
        Graphics.RenderMeshInstanced(renderParams, basePatchMesh, 0, instancedMatrices);
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
        // Limit to Unity's maximum single batch array limit for basic instancing (1023)
        if (count > 1023) count = 1023; 

        instancedMatrices = new Matrix4x4[count];

        int index = 0;
        for (int z = 0; z < gridLength; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (index >= count) break;

                // Calculate positions to stitch them perfectly side-by-side
                Vector3 position = new Vector3(x * spacing, 0, z * spacing) + transform.position;
                Quaternion rotation = Quaternion.identity;
                Vector3 scale = Vector3.one;

                instancedMatrices[index] = Matrix4x4.TRS(position, rotation, scale);
                index++;
            }
        }
    }
}