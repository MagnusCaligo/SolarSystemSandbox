using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class GeneratePlanet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Material material;
    public ComputeShader shader;
    [Range(1, 100)]
    public float pyramidHeight = 1;

    [Range(1, 500)]
    public int subdivisions = 1;

    private bool needUpdateMesh = true;
    private Mesh customMesh;
    void Start()
    {
        GenerateMesh();
    }

    public void GenerateMesh()
    {
        if (customMesh != null)
        {
            DestroyImmediate(customMesh);
        }
        customMesh = new Mesh();
        customMesh.name = "Custom Mesh";
        gameObject.GetComponent<MeshFilter>().mesh = customMesh;


        var initialTriangle = new ComputeBuffer(3, sizeof(float) * 3);

        Vector3[] initialTriangleVectors = new Vector3[] {
            new Vector3(0,pyramidHeight,0),
            new Vector3(pyramidHeight * -(2/Mathf.Sqrt(3))/2.0f, 0, pyramidHeight * -(2/Mathf.Sqrt(3))/2.0f),
            new Vector3( pyramidHeight * (2/Mathf.Sqrt(3))/2.0f, 0, pyramidHeight * -(2/Mathf.Sqrt(3))/2.0f),
        };

        initialTriangle.SetData(initialTriangleVectors);

        float height = pyramidHeight / (subdivisions + 1);
        int maxTriangles = (int) Mathf.Pow(subdivisions+1, 2);

        // Create buffers
        var vertexBuffer = new ComputeBuffer(maxTriangles * 3, sizeof(float) * 3);
        var uvBuffer = new ComputeBuffer(maxTriangles * 3, sizeof(float) * 2);
        var indexBuffer = new ComputeBuffer(maxTriangles * 3, sizeof(uint));

        int kernel = shader.FindKernel("CSMain");

        shader.SetBuffer(kernel, "verticesBuffer", vertexBuffer);
        shader.SetBuffer(kernel, "uvsBuffer", uvBuffer);
        shader.SetBuffer(kernel, "indicesBuffer", indexBuffer);
        shader.SetBuffer(kernel, "initialTriangle", initialTriangle);

        shader.SetInt("numberOfSubdivisions", subdivisions);
        shader.SetFloat("layerHeight", height);
        shader.SetInt("maxTriangles", maxTriangles);
        shader.SetFloat("sideToHeightRatio", Mathf.Sqrt(3) / 2.0f);
        shader.SetFloat("heightToSideRatio", 2.0f/ Mathf.Sqrt(3));




        shader.Dispatch(kernel, maxTriangles, 1, 1);

        // Read back data
        Vector3[] vertices = new Vector3[maxTriangles * 3];
        Vector2[] uvs = new Vector2[maxTriangles * 3];
        int[] triangles = new int[maxTriangles * 3];

        vertexBuffer.GetData(vertices);
        uvBuffer.GetData(uvs);
        indexBuffer.GetData(triangles);

        // Create mesh
        customMesh.vertices = vertices;
        customMesh.uv = uvs;
        customMesh.triangles = triangles;
        customMesh.RecalculateNormals();
    }

    public void OnValidate()
    {
        needUpdateMesh = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (needUpdateMesh)
        {
            GenerateMesh();
            needUpdateMesh = false;
        }
        
    }
}
