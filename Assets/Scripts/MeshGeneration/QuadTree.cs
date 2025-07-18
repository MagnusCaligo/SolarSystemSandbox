
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Animations;
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

    // Previous data 
    private GeneratePlanet.GeneratedMeshData previousData;
    List<QuadTree> children;

    public static Dictionary<int, float> LODMapping = new Dictionary<int, float>()
    {
        [0] = 20.0f,
        [1] = 10.0f,
        [2] = 5.0f,
    }
    ;



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

    public GeneratePlanet.GeneratedMeshData GenerateMesh()
    {
        float distance = (origin - this.distanceObjectPosition).magnitude;

        // Calculate radius of a sphere that covers the points, if the object enters this sphere then we traverse the tree
        float radius = new Vector2(resolution * squareSize * .5f, resolution * squareSize * .5f).magnitude;

        //if (depth < maxDepth && distance < radius * radiusMultiplier)
        if (depth < maxDepth && inCone())
        {

            if (children == null)
            {

                children = new List<QuadTree>();
                Vector3 topLeftOrigin = this.origin - new Vector3(resolution * squareSize * .25f, -resolution * squareSize * .25f, 0);
                Vector3 topRightOrigin = this.origin - new Vector3(-resolution * squareSize * .25f, -resolution * squareSize * .25f, 0);
                Vector3 bottomRightOrigin = this.origin - new Vector3(-resolution * squareSize * .25f, resolution * squareSize * .25f, 0);
                Vector3 bottomLeftOrigin = this.origin - new Vector3(resolution * squareSize * .25f, resolution * squareSize * .25f, 0);

                QuadTree topLeft = new QuadTree(topLeftOrigin, squareSize / 2.0f, distanceObjectPosition, depth + 1, radiusMultiplier);
                QuadTree topRight = new QuadTree(topRightOrigin, squareSize / 2.0f, distanceObjectPosition, depth + 1, radiusMultiplier);
                QuadTree bottomRight = new QuadTree(bottomRightOrigin, squareSize / 2.0f, distanceObjectPosition, depth + 1, radiusMultiplier);
                QuadTree bottomLeft = new QuadTree(bottomLeftOrigin, squareSize / 2.0f, distanceObjectPosition, depth + 1, radiusMultiplier);

                children.Add(topLeft);
                children.Add(topRight);
                children.Add(bottomRight);
                children.Add(bottomLeft);
            }

            GeneratePlanet.GeneratedMeshData gmd = new GeneratePlanet.GeneratedMeshData();
            foreach (var child in children)
            {
                gmd += child.GenerateMesh();
            }

            previousData = null;
            return gmd;
        }
        if (children != null)
            foreach (var child in children)
                child.previousData = null;

        if (previousData == null)
            previousData = GenerateVerticesAndTrianglesAroundOrigin();
        return previousData;
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

        return (originAvg - distanceObjectPosition).magnitude < squareSize * resolution * radiusMultiplier;
        //return a2 <= a1;
        //return pointAngle <= angle;
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

    public  GeneratePlanet.GeneratedMeshData GenerateVerticesAndTrianglesAroundOrigin()
    {
        GeneratePlanet.GeneratedMeshData gmd = new GeneratePlanet.GeneratedMeshData();

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
