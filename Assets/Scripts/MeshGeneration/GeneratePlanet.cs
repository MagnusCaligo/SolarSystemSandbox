using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class GeneratePlanet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Material material;
    public ComputeShader shader;
    public GameObject distanceObject;

    private ComputeShaderHelper csHelper;

    [Range(1, 500)]
    public int resolution = 1;
    private int previousResolution = 1;


    [Range(1, 100)]
    public float sphereRadius = .1f;

    public Vector3 sphereLocation = new Vector3(0, .5f, 0f);

    public bool doComputeShaderFunction = false;

    public float planetRadius = 100f;
    public Vector3 normalizePos = Vector3.zero;

    private bool needUpdateMesh = true;
    private bool needUpdateMeshPlanes = true;
    private Mesh customMesh;

    public class GeneratedMeshData
    {
        public List<Vector3> verticies = new List<Vector3>();
        public List<Vector2> uv = new List<Vector2>();
        public List<int> triangles = new List<int>();

        public static GeneratedMeshData operator +(GeneratedMeshData operandLeft, GeneratedMeshData operandRight)
        {
            int verticesCount = operandLeft.verticies.Count;
            foreach (var vert in operandRight.verticies) operandLeft.verticies.Add(vert);
            foreach (var uv in operandRight.uv) operandLeft.uv.Add(uv);
            foreach (var triangle in operandRight.triangles) operandLeft.triangles.Add(triangle + verticesCount);
            return operandLeft;
        }
    }

    void Start()
    {
        GenerateMesh();
    }

    public void GenerateMesh()
    {
        updateComputeShaderHelper();
        if (needUpdateMeshPlanes)
        {
            customMesh = new Mesh();
            customMesh.name = "Custom Mesh";
            customMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            needUpdateMeshPlanes = false;
        }
        gameObject.GetComponent<MeshFilter>().mesh = customMesh;

        if (distanceObject == null)
            return;

        GeneratedMeshData data;
        data  = MakeSubdividedPlane(new Vector3(0, 0, -.5f), Quaternion.Euler(new Vector3(0, 0, 0)));
        data += MakeSubdividedPlane(new Vector3(-.5f, 0, 0), Quaternion.Euler(new Vector3(0, 90, 0)));
        data += MakeSubdividedPlane(new Vector3(.5f, 0, 0), Quaternion.Euler(new Vector3(0, -90, 0)));
        data += MakeSubdividedPlane(new Vector3(0, 0, .5f), Quaternion.Euler(new Vector3(0, 180, 0)));
        data += MakeSubdividedPlane(new Vector3(0, .5f, 0), Quaternion.Euler(new Vector3(90, 0, 0)));
        data += MakeSubdividedPlane(new Vector3(0, -.5f, 0), Quaternion.Euler(new Vector3(-90, 0, 0)));

        Vector3[] resultVertices = new Vector3[data.verticies.Count];

        // Generate Surface
        resultVertices = csHelper.recalculateVertices(data.verticies).ToArray();

        if(doComputeShaderFunction)
          data.verticies = resultVertices.ToList<Vector3>();

        customMesh.vertices = data.verticies.ToArray();
        customMesh.uv = data.uv.ToArray();
        customMesh.triangles = data.triangles.ToArray();
        customMesh.RecalculateNormals();
        customMesh.RecalculateBounds();
        gameObject.GetComponent<MeshCollider>().sharedMesh = customMesh;

    }

    public void OnDrawGizmos()
    {
    }

    private GeneratedMeshData MakeSubdividedPlane(Vector3 offset, Quaternion rotation)
    {
        GeneratedMeshData gmd = new GeneratedMeshData();

        // We will generate a plane of from the top left corner. 
        // We start with the triangles one square at a time. 
        // We can re-use the points from one triangle to make the next triangle.
        // By holding a reference to the previous right vertices, we can use them as the next left vertices for the next triangle.
        Queue<float3> previousRightVertices = new Queue<float3>(); 

        // We can do a similar logic for the horizontal layers.
        // By holding onto the bottom vertices of each layer, we can use them as the top vertices for the next layer.
        Queue<float3> previousBottomVertices = new Queue<float3>();
        Queue<float3> newBottomVertices = new Queue<float3>();

        float squareSize = 1f / (resolution * 2);
        float3 originOffset = -new Vector3(.5f, -.5f, 0);


        for (int y = 0; y < (resolution * 2); y++)
        {
            for (int x = 0; x < (resolution * 2); x++)
            {
                bool haveLeft = false;
                bool haveTop = false;
                // Get previous vertices if they are available
                float3[] verts = new float3[4];
                if (previousRightVertices.Count >= 2)
                {
                    verts[0] = previousRightVertices.Dequeue();
                    verts[3] = previousRightVertices.Dequeue();
                    haveLeft = true;
                }
                if (previousBottomVertices.Count >= 2)
                {
                    verts[0] = previousBottomVertices.Dequeue();
                    verts[1] = previousBottomVertices.Dequeue();
                    haveTop = true;
                }

                if (!haveLeft) {
                    verts[0] = new float3(x*squareSize, -y*squareSize, 0) + originOffset;
                    verts[3] = new float3(x*squareSize, -(y+1)*squareSize, 0) + originOffset;
                }
                if (!haveTop)
                {
                    verts[0] = new float3(x*squareSize, -y*squareSize, 0) + originOffset;
                    verts[1] = new float3((x+1)*squareSize, -y*squareSize, 0) + originOffset;
                }

                // Generate Bottom Right
                verts[2] = new float3((x+1)*squareSize, -(y+1) *  squareSize, 0) + originOffset;

                // Populate Queues
                newBottomVertices.Enqueue(verts[3]);
                newBottomVertices.Enqueue(verts[2]);

                previousRightVertices.Enqueue(verts[1]);
                previousRightVertices.Enqueue(verts[2]);


                // Add vertices to list
                int currentVerticesCount = gmd.verticies.Count;
                for (int i = 0; i < 4; i++) gmd.verticies.Add(verts[i]);

                // Add uv values, 0s for now
                for (int i = 0; i < 4; i++) gmd.uv.Add(new float2(0.0f, 0.0f));

                // Add Triangles
                gmd.triangles.Add(currentVerticesCount);
                gmd.triangles.Add(currentVerticesCount + 1);
                gmd.triangles.Add(currentVerticesCount + 2);

                gmd.triangles.Add(currentVerticesCount);
                gmd.triangles.Add(currentVerticesCount + 2);
                gmd.triangles.Add(currentVerticesCount + 3);
            }

            previousRightVertices.Clear();
            for (int i = 0; i < newBottomVertices.Count; i++) previousBottomVertices.Enqueue(newBottomVertices.Dequeue());
            newBottomVertices.Clear();
        }
        for (int i = 0; i < gmd.verticies.Count; i++)
        {
            gmd.verticies[i] = (rotation * gmd.verticies[i]) + offset;
        }

        return gmd;
    }

    public void updateComputeShaderHelper()
    {
        if (csHelper == null)
        {
            csHelper = new ComputeShaderHelper();
        }

        csHelper.shader = shader;
        csHelper.sphereLocation = sphereLocation.normalized;
        csHelper.sphereRadius = sphereRadius / 100f;

    }
    public void OnValidate()
    {
        if (resolution != previousResolution)
            needUpdateMeshPlanes = true;
        previousResolution = resolution;
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
