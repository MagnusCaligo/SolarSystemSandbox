
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class ComputeShaderHelper
{

    public ComputeShader shader;

    public Vector3 origin;
    public float radius;
    public float magnitude;
    public float frequency;

    public List<Vector3> recalculateVertices(List<Vector3> inputVertices)
    {
        Vector3[] output = new Vector3[inputVertices.Count];

        shader.SetInt("numVertexes", inputVertices.Count);
        ComputeBuffer vertexBuffer = new ComputeBuffer(inputVertices.Count, sizeof(float) * 3);
        vertexBuffer.SetData(inputVertices);

        int kernel = shader.FindKernel("CSMain");
        shader.SetBuffer(kernel, "vertexBuffer", vertexBuffer);
        shader.SetVector("normalizeOrigin", origin);
        shader.SetFloat("radius", radius);

        shader.SetFloat("magnitude", magnitude);
        shader.SetFloat("frequency", frequency);

        shader.Dispatch(kernel, (inputVertices.Count / 64) + 1, 1, 1);
        vertexBuffer.GetData(output);

        return output.ToList();
    }

}