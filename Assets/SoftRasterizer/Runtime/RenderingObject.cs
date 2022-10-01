using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RenderingObjects is a class define object in scene that needs to be rendered
/// </summary>
public class RenderingObject : MonoBehaviour
{
    public Mesh mesh;
    public Texture2D tex;
    // use buffer to avoid direct copy from mesh in draw call
    public Vector3[] meshVertices;
    public Vector3[] meshNormals;
    public int[] meshTriangles;
    public Vector2[] meshUV;
    public VertexBuff[] vertexBuffer;

    void Start()
    {
        var meshFilter = GetComponent<MeshFilter>(); // use meshfilter to get renference to target mesh, avoid crush of nullptr
        if (meshFilter != null)
            mesh = meshFilter.mesh;

        var meshRenderer = GetComponent<MeshRenderer>(); // use MeshRenderer to get materials
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            tex = meshRenderer.sharedMaterial.mainTexture as Texture2D; // tex is null if no texture

        // when no tex found, assign one
        if (tex == null)
            tex = Texture2D.whiteTexture;

        if(mesh != null)
        {
            meshVertices = mesh.vertices;
            meshNormals = mesh.normals;
            meshTriangles= mesh.triangles;
            meshUV = mesh.uv;
            vertexBuffer = new VertexBuff[mesh.vertexCount];
        }
    }

    /// <summary>
    /// Unity has a convention of calculation Model transformation matrix, we have to use TRS
    /// </summary>
    /// <returns></returns>
    public Matrix4x4 GetModelMatrix()
    {
        Matrix4x4 matModel = transform.localToWorldMatrix;

        return matModel;
        // if (transform == null)
        // {
        //     // return Matrix4x4.identity;
        //     return Utility.GetRotZMatrix(0);
        // }

        // var matScale = Utility.GetScaleMatrix(transform.lossyScale); // lossyScale is gloabal scale

        // var rotation = transform.rotation.eulerAngles;
        // var rotX = Utility.GetRotationMatrix(Vector3.right, -rotation.x); // pitch
        // var rotY = Utility.GetRotationMatrix(Vector3.up, -rotation.y); // yaw
        // var rotZ = Utility.GetRotationMatrix(Vector3.forward, rotation.z); // roll
        // var matRot = rotY * rotX * rotZ; // rotation apply order: z(roll), x(pitch), y(yaw) 

        // var matTranslation = Utility.GetTranslationMatrix(transform.position);

        // return matTranslation * matRot * matScale;
    }

}
