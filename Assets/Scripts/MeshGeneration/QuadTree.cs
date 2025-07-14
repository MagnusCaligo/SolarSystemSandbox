
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

class QuadTree
{
    public static int resolution = 10;

    private Vector3 origin;
    private float squareSize;
    private int depth;
    private Vector3 distanceObjectPosition;
    private float radiusMultiplier = 1f;
    private static ComputeShaderHelper CSH;

    public static int maxDepth = 2;

    private Vector3 point1;
    private Vector3 point2;

    public static Dictionary<int, float> LODMapping = new Dictionary<int, float>()
    {
        [0] = 20.0f,
        [1] = 10.0f,
        [2] = 5.0f,
    }
    ;

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


    public QuadTree(float3 origin, float squareSize, float3 objectPos, float radiusMul, ComputeShaderHelper csh) {
        setup(origin, squareSize, objectPos, 0, radiusMul);
        QuadTree.CSH = csh;
    }

    public QuadTree(float3 origin, float squareSize, float3 objectPos, int depth, float radiusMul) {
        setup(origin, squareSize, objectPos, depth, radiusMul);
    }

    public void setup(float3 origin, float squareSize, float3 objectPos, int depth, float radiusMul)
    {
        this.origin = origin;
        this.squareSize = squareSize;
        this.distanceObjectPosition = objectPos;
        this.depth = depth;
        this.radiusMultiplier = radiusMul;
    }


    public GeneratedMeshData GenerateMesh()
    {
        float distance = (origin - this.distanceObjectPosition).magnitude;

        // Calculate radius of a sphere that covers the points, if the object enters this sphere then we traverse the tree
        float radius = new Vector2(resolution * squareSize * .5f, resolution * squareSize * .5f).magnitude;

        //if (depth < maxDepth && distance < radius * radiusMultiplier)
        if (depth < maxDepth && inCone())
        {
            Vector3 topLeftOrigin = this.origin - new Vector3(resolution * squareSize * .25f, -resolution * squareSize *.25f, 0);
            Vector3 topRightOrigin = this.origin - new Vector3(-resolution * squareSize * .25f, -resolution * squareSize *.25f, 0);
            Vector3 bottomRightOrigin = this.origin - new Vector3(-resolution * squareSize * .25f, resolution * squareSize *.25f, 0);
            Vector3 bottomLeftOrigin = this.origin - new Vector3(resolution * squareSize * .25f, resolution * squareSize *.25f, 0);

            QuadTree topLeft     = new QuadTree(topLeftOrigin, squareSize / 2.0f, distanceObjectPosition, depth + 1, radiusMultiplier);
            QuadTree topRight    = new QuadTree(topRightOrigin, squareSize / 2.0f, distanceObjectPosition, depth + 1, radiusMultiplier);
            QuadTree bottomRight = new QuadTree(bottomRightOrigin, squareSize / 2.0f, distanceObjectPosition, depth + 1, radiusMultiplier);
            QuadTree bottomLeft  = new QuadTree(bottomLeftOrigin, squareSize / 2.0f, distanceObjectPosition, depth + 1, radiusMultiplier);

            var gmd = topLeft.GenerateMesh();
            gmd += topRight.GenerateMesh();
            gmd += bottomLeft.GenerateMesh();
            gmd += bottomRight.GenerateMesh();
            return gmd;
        }

       return GenerateVerticesAndTrianglesAroundOrigin();
    }

    public bool inCone()
    {

        Vector3 planetOrigin = CSH.origin;
        Vector3 topLeftCorner = origin - new Vector3(resolution * squareSize * .5f, -resolution * squareSize *.5f, 0);
        Vector3 topRightCorner = origin - new Vector3(-resolution * squareSize * .5f, -resolution * squareSize *.5f, 0);
        Vector3 bottomRightCorner = origin - new Vector3(-resolution * squareSize * .5f, resolution * squareSize *.5f, 0);
        Vector3 bottomLeftCorner = origin - new Vector3(resolution * squareSize * .5f, resolution * squareSize *.5f, 0);

        List<Vector3> corners = new List<Vector3>();
        corners.Add(topLeftCorner);
        corners.Add(topRightCorner);
        corners.Add(bottomRightCorner);
        corners.Add(bottomLeftCorner);


        List<Vector3> cornersAfter = CSH.recalculateVertices(corners.ToList());

        Vector3 originAvg = Vector3.zero;
        foreach (var v in corners)
            originAvg += v;
        originAvg /= 4.0f;

        Vector3 direction = (originAvg - planetOrigin).normalized;

        var t = Vector3.Dot(cornersAfter[0].normalized, direction);
        var p = Mathf.Acos(t);
        float angle = Mathf.Acos(Vector3.Dot(cornersAfter[0].normalized, direction));

        Vector3 pointDirection = (this.distanceObjectPosition - planetOrigin).normalized;
        float pointAngle = Mathf.Acos(Vector3.Dot(pointDirection, direction));

        var a1 = Vector3.Angle(cornersAfter[0], direction);
        var a2 = Vector3.Angle(pointDirection, direction);

        if (topLeftCorner == new Vector3(-50f, 50f, 0f))
        {
            if (bottomRightCorner == Vector3.zero)
            {

                point1 = planetOrigin;
                point2 = cornersAfter[0];
            }

        }

        return pointAngle <= angle;
    }

    public void OnDrawGizmos()
    {
        if (point1 == null || point2 == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(point1, point2);
        
    }

    // Kinda useless, I realize that the frustrum of the corners doesn't work when the radius is small. Need to use a cone.
    public bool inFrustrum()
    {

        Vector3 planetOrigin = CSH.origin;

        Vector3 topLeftCorner = origin - new Vector3(resolution * squareSize * .5f, -resolution * squareSize *.5f, 0);
        Vector3 topRightCorner = origin - new Vector3(-resolution * squareSize * .5f, -resolution * squareSize *.5f, 0);
        Vector3 bottomRightCorner = origin - new Vector3(-resolution * squareSize * .5f, resolution * squareSize *.5f, 0);
        Vector3 bottomLeftCorner = origin - new Vector3(resolution * squareSize * .5f, resolution * squareSize *.5f, 0);

        List<Vector3> corners = new List<Vector3>();
        corners.Add(topLeftCorner);
        corners.Add(topRightCorner);
        corners.Add(bottomRightCorner);
        corners.Add(bottomLeftCorner);

        List<Vector3> cornersAfter = CSH.recalculateVertices(corners.ToList());

        Vector3 topPlane    = Vector3.Cross(cornersAfter[0] - planetOrigin, cornersAfter[1] - planetOrigin);
        Vector3 rightPlane  = Vector3.Cross(cornersAfter[1] - planetOrigin, cornersAfter[2] - planetOrigin);
        Vector3 bottomPlane = Vector3.Cross(cornersAfter[2] - planetOrigin, cornersAfter[3] - planetOrigin);
        Vector3 leftPlane   = Vector3.Cross(cornersAfter[3] - planetOrigin, cornersAfter[0] - planetOrigin);

        Vector3 relativeToOrigin = distanceObjectPosition - planetOrigin;
        var t = Vector3.Dot(topPlane, relativeToOrigin);
        var r = Vector3.Dot(rightPlane, relativeToOrigin);
        var b = Vector3.Dot(bottomPlane, relativeToOrigin);
        var l = Vector3.Dot(leftPlane, relativeToOrigin);

        return  Vector3.Dot(topPlane, relativeToOrigin)    >= 0 &&
                Vector3.Dot(rightPlane, relativeToOrigin)  >= 0 &&
                Vector3.Dot(bottomPlane, relativeToOrigin) >= 0 &&
                Vector3.Dot(leftPlane, relativeToOrigin)   >= 0;
    }

    public  GeneratedMeshData GenerateVerticesAndTrianglesAroundOrigin()
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

        float3 originOffset = origin - new Vector3(resolution * squareSize * .5f, -resolution * squareSize * .5f, 0);

        for (int y = 0; y < QuadTree.resolution; y++)
        {
            for (int x = 0; x < QuadTree.resolution; x++)
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

        return gmd;
    }


}