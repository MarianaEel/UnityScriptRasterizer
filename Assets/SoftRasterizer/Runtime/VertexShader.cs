using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct VertexBuff
{
    public Vector4 clipPos; //clip space vertices
    public Vector3 worldPos; //world space vertices
    public Vector3 objectNormal; //obj space normals
    public Vector3 worldNormal; //world space normals
}
public struct VertexPayload
{
    public RenderingObject renderingObject;
    public Matrix4x4 matmvp;
    public Matrix4x4 matModel;
}
public class VertexShader
{
    public static void DoVertexShading(VertexPayload payload, VertexBuff[] vertexBuff)
    {
        // Debug.Log("Calling VertShader");
        for (int i = 0; i < payload.renderingObject.mesh.vertexCount; ++i)
        {
            Vector3 vertex = payload.renderingObject.meshVertices[i];
            Vector4 homoVertex = Utility.ToVec4(vertex);
            homoVertex.z *= -1;
            vertexBuff[i].clipPos = payload.matmvp * homoVertex;
            vertexBuff[i].worldPos = payload.matModel * homoVertex;

            Vector3 normal = payload.renderingObject.meshNormals[i];
            var homoNormal = new Vector3(normal.x, normal.y, -normal.z);
            vertexBuff[i].objectNormal = homoNormal;
            vertexBuff[i].worldNormal = payload.matModel.inverse.transpose * homoNormal;
        }
    }

}
