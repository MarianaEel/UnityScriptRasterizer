using System.Collections;
using System.Collections.Generic;
using UnityEngine; // vecs

/// <summary>
/// TVertex struct is for presenting vertex of a triangle.
/// 
/// </summary>
public struct TVertex
{
    public Vector4 pos;
    public Vector3 normal;
    public Vector2 tex_coord;
    public Color Color;    
    public Vector3 WorldPos;
    public Vector3 WorldNormal;
}

/// <summary>
/// Triangle class represent a triangle,
/// it has 3 TVertex
/// </summary>
public class Triangle
{
    public TVertex[] vertexes = new TVertex[3]; // 3 vertex
}