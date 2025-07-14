using System.Linq;
using Unity.Mathematics;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class GeneratePlanet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Material material;
    public ComputeShader shader;
    public GameObject distanceObject;

    private ComputeShaderHelper csHelper;

    [Range(1, 100)]
    public float squareSize= 1;

    [Range(1, 5)]
    public float LODRadiusMultiplier = 1f;

    [Range(1, 10)]
    public int maxDepth = 2;

    [Range(-10, 10)]
    public float frequency = 1;

    [Range(0, 10)]
    public float magnitude = 1;

    public bool doComputeShaderFunction = false;

    public float planetRadius = 100f;
    public Vector3 normalizePos = Vector3.zero;

    private bool needUpdateMesh = true;
    private Mesh customMesh;
    void Start()
    {
        GenerateMesh();
    }

    public void GenerateMesh()
    {

        updateComputeShaderHelper();
        customMesh = new Mesh();
        customMesh.name = "Custom Mesh";
        customMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        gameObject.GetComponent<MeshFilter>().mesh = customMesh;

        if (distanceObject == null)
            return;
        QuadTree tree = new QuadTree(transform.position, squareSize, distanceObject.transform.position, LODRadiusMultiplier, csHelper);
        QuadTree.GeneratedMeshData data = tree.GenerateMesh();

        Vector3[] resultVertices = new Vector3[data.verticies.Count];

        // Generate Surface
        resultVertices = csHelper.recalculateVertices(data.verticies).ToArray();

        if(doComputeShaderFunction)
          data.verticies = resultVertices.ToList<Vector3>();

        customMesh.vertices = data.verticies.ToArray();
        customMesh.uv = data.uv.ToArray();
        customMesh.triangles = data.triangles.ToArray();
        customMesh.RecalculateNormals();

    }

    public void OnDrawGizmos()
    {
        foreach (QuadTree tree in Object.FindObjectsByType(typeof(QuadTree), FindObjectsSortMode.None){

        }
    }

    public void updateComputeShaderHelper()
    {
        if (csHelper == null)
        {
            csHelper = new ComputeShaderHelper();
        }

        csHelper.shader = shader;
        csHelper.origin = normalizePos;
        csHelper.radius = planetRadius;
        csHelper.magnitude = magnitude;
        csHelper.frequency = frequency;

    }
    public void OnValidate()
    {
        needUpdateMesh = true;
        QuadTree.maxDepth = maxDepth;
    }

    // Update is called once per frame
    void Update()
    {
        if (needUpdateMesh)
        {
            GenerateMesh();
        }
        
    }
}
