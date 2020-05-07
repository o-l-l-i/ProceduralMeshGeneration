using UnityEngine;
using System.Runtime.InteropServices;


public class ProceduralMeshGeneration : MonoBehaviour
{

    public ComputeShader compute;
    public Material material;

    int kernel_generateMesh;

    ComputeBuffer vertexBuffer;
    ComputeBuffer normalBuffer;

    ComputeBuffer drawArgsBuffer;


    public int sideWidth = 256;
    int quadCount;
    int triCount;
    int triVertCount;

    public float noiseScale = 30;
    public float displacement = 10;

    static int stride = Marshal.SizeOf(typeof(Vector3));


    void Start()
    {
        // Find kernel
        kernel_generateMesh = compute.FindKernel("K_Generate");

        Debug.Log("material: " + material.name);
        Debug.Log("compute shader: " + compute.name);
        Debug.Log("stride: " + stride);
    }


    // OnPostRender() seems to be broken? - Does not render even if attached to camera
    void OnRenderObject()
    {
        // Calculate needed vertices etc.
        quadCount = sideWidth * sideWidth;
        triCount = quadCount * 2;
        triVertCount = triCount * 3;

        // If mesh configuration changes, reallocate
        if (vertexBuffer == null || vertexBuffer.count != triVertCount)
        {
            Debug.Log("sideWidth: "+ sideWidth);
            Debug.Log("quadCount: " + quadCount);
            Debug.Log("triCount: " + triCount);
            Debug.Log("triVertCount: " + triVertCount);

            ReleaseBuffers();

            ReallocateBuffers();

            // Create arguments for drawing the mesh
            // Set args array to the argument buffer
            int[] args = new int[]{ triVertCount, 1, 0, 0 };
            drawArgsBuffer.SetData(args);

            Debug.Log("Draw args: ");
            Debug.Log("arg[0]: " + args[0]);
            Debug.Log("arg[1]: " + args[1]);
            Debug.Log("arg[2]: " + args[2]);
            Debug.Log("arg[3]: " + args[3]);
        }

        // Set variables
        compute.SetFloat("_Time", Time.time);
        compute.SetInt("_Size", sideWidth);
        compute.SetFloat("_NoiseScale", noiseScale);
        compute.SetFloat("_Displacement", displacement);

        // Set buffers
        compute.SetBuffer(kernel_generateMesh, "vertexBuffer", vertexBuffer);
        compute.SetBuffer(kernel_generateMesh, "normalBuffer", normalBuffer);

        // Generate the mesh with compute shader
        compute.Dispatch(kernel_generateMesh, Mathf.Max(1, sideWidth/8), Mathf.Max(1, sideWidth/8), 1);

        // Set the structured buffer with mesh data to the material
        material.SetBuffer ("vertexBuffer", vertexBuffer);
        material.SetBuffer ("normalBuffer", normalBuffer);

        // Configure render pass
        material.SetPass(0);

        // Draw
        Graphics.DrawProceduralIndirect(MeshTopology.Triangles, drawArgsBuffer, 0);
    }


    void ReallocateBuffers()
    {
        Debug.Log("Reallocating buffers.");
        vertexBuffer   = new ComputeBuffer(triVertCount, stride, ComputeBufferType.Default);
        normalBuffer   = new ComputeBuffer(triVertCount, stride, ComputeBufferType.Default);
        drawArgsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
    }


    void ReleaseBuffers()
    {
        Debug.Log("Releasing buffers.");
        if (drawArgsBuffer != null) drawArgsBuffer.Release();
        if (vertexBuffer   != null) vertexBuffer.Release();
        if (normalBuffer   != null) normalBuffer.Release();
    }


    void OnDestroy()
    {
        ReleaseBuffers();
    }

}
