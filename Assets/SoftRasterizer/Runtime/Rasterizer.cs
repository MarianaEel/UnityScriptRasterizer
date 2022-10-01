using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


public class Rasterizer
{
    // Here goes properties
    public string name { get => "rasterizer"; }
    public Texture2D texture;
    public int shadowMapResolution = 512;
    public float orthoDistance = 500.0f;

    private int _scrwidth;
    private int _scrheight;
    private float _aspratial;

    private Matrix4x4 _matModel;
    private Matrix4x4 _matView;
    private Matrix4x4 _matShadowView;
    private Matrix4x4 _matProj;
    private Matrix4x4 _matShadowProjection;

    private float[] _bufDepth;
    private float[] _bufShadow;
    private Color[] _bufColor;
    private int _triangleTotal, _triangleRendered, _verticesTotal;
    // private VertexShader vshader;
    private VertexPayload vpayload;
    private FragmentShader fragshader;
    private FragShaderPayload payload;
    private FragShaderWorldInfo finfo;
    private Vector4[] vec4Vertex = new Vector4[3];
    private Vector3[] vec3Vertex = new Vector3[3];
    private int[] ind = new int[3];// store index
    private Triangle tri = new Triangle();


    // Here goes constructor
    public Rasterizer(int width, int height)
    {
        Debug.Log($"CPU Rasterizer screen size: {width} x {height}");
        _scrwidth = width;
        _scrheight = height;
        _bufDepth = new float[_scrwidth * _scrheight];
        _bufShadow = new float[shadowMapResolution * shadowMapResolution];
        _bufColor = new Color[_scrwidth * _scrheight];
        texture = new Texture2D(_scrwidth, _scrheight);
        texture.filterMode = FilterMode.Point;
    }

    // Here goes public methods
    /// <summary>
    /// Render defines the basic render pipeline of the SoftRasterizer.
    /// Render is called every CameraRender OnPost Render call.
    /// It perform Clear, Setup, Shadow Cast, Draw  and Submit in order.
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="light"></param>
    /// <param name="renderingObjects"></param>
    public void Render(Camera camera, Light light, List<RenderingObject> renderingObjects)
    {
        Clear();
        SetupInfo(camera, light);
        // ShadowCastingPass(camera,light); // broken, need fixing
        Draw(renderingObjects);
        Submit();
    }

    // Here goes private methods
    /// <summary>
    /// Clear clears all buffers and counters (Fill them initial values) 
    /// </summary>
    private void Clear()
    {
        ProfileManager.BeginSample("Clear");
        Utility.FillArray(_bufColor, Color.white);
        Utility.FillArray(_bufDepth, .0f);
        Utility.FillArray(_bufShadow,.0f);

        _triangleTotal = _triangleRendered = _verticesTotal = 0; // clear count
        ProfileManager.EndSample();
    }

    /// <summary>
    /// Set up info set up all info needed for rendering,
    /// It get camera and light information and setup View and Projection matrices based on it
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="light"></param>
    private void SetupInfo(Camera camera, Light light)
    {
        var campos = camera.transform.position;
        campos.z *= -1; // flip to right hand coord
        // Debug.Log($"Cam pos: {campos}");
        finfo.worldSpaceCameraPos = campos;

        var lightDir = light.transform.forward;
        lightDir.z *= -1;
        finfo.worldSpaceLightDir = -lightDir; // already minus!!!!!!!!!!!!!!!!!!!!!

        finfo.lightColor = light.color * light.intensity;
        finfo.ambientColor = Color.yellow; // later can add IBL to this. [to be modified]

        _aspratial = _scrwidth / _scrheight;

        Utility.SetupmatVP(camera, _aspratial, out _matView, out _matProj); // now we have View and Projection Matrix
    }

    /// <summary>
    /// Draw takes RenderingObject list and pass rendering objects in it to DrawOneObj
    /// </summary>
    /// <param name="renderingObjects"></param>
    private void Draw(List<RenderingObject> renderingObjects)
    {
        for (int i = 0; i < renderingObjects.Count; ++i)
        {
            if (renderingObjects[i].gameObject.activeInHierarchy) // if active
            {
                DrawOneObj(renderingObjects[i]);
            }
        }
    }

    /// <summary>
    /// Draw a object, first do frustum culling.
    /// If the object pass the culling, it will be sent into vertex shader.
    /// Vertex buffer is set during vertex shader, which contains all verteces need to be rendered.
    /// Then primitive assembly start, matches triangle indecies with verteces 
    /// Then do clip and strech clip space to NDC coordinate.
    /// Then check primitives' normal and do back-culling.
    /// Then do viewport transform, strech NDC [0,1],[0,1] to screen coordinate[0,w],[0,h]
    /// Then setup triangles and call RasterizeTriangle() to rasterize triangle
    /// </summary>
    /// <param name="renderingObject"></param>
    private void DrawOneObj(RenderingObject renderingObject)
    {
        // Debug.Log($"Draw one obj {renderingObject.name}");
        Mesh mesh = renderingObject.mesh;
        _matModel = renderingObject.GetModelMatrix();

        Matrix4x4 matmvp = _matProj * _matView * _matModel;
        Matrix4x4 matShadowmvp = _matShadowProjection * _matShadowView * _matModel;

        // FrustumCulling
        if (Utility.FrustumCulling(mesh.bounds, matmvp))
        {
            ProfileManager.EndSample();
            return;// dont draw this obj cuz its outside frustrum
        }

        _verticesTotal += mesh.vertexCount;
        _triangleTotal += renderingObject.meshTriangles.Length / 3; // meshTriangle is a int array with all vertical info

        // ----------- Light pass -----------
        // ------ vertex shader
        // initial vertex shader
        ProfileManager.BeginSample("VertexShader");
        VertexBuff[] vertexBuff = renderingObject.vertexBuffer;

        vpayload.matmvp = matmvp;
        vpayload.renderingObject = renderingObject;
        vpayload.matModel = _matModel;
        // vshader.SetPayload(vpayload);
        VertexShader.DoVertexShading(vpayload, vertexBuff);
        ProfileManager.EndSample();

        /// loop through all triangles
        int[] triIndexes = renderingObject.meshTriangles;
        for (int i = 0; i < triIndexes.Length; i += 3)
        {
            // ------ primitive assembly
            ind[0] = triIndexes[i + 1];
            ind[1] = triIndexes[i];
            ind[2] = triIndexes[i + 2];

            var v = vec4Vertex;

            v[0] = vertexBuff[ind[0]].clipPos;
            v[1] = vertexBuff[ind[1]].clipPos;
            v[2] = vertexBuff[ind[2]].clipPos;

            /// Normally Engines do clipping at geometry stage before primitive assembly,
            /// and it is done by engine, clip vertex outside frustum and add new vertexes at the edge.
            /// I do clipping here to cut triangle totally out side frustrum as a replacement 
            if (Clipped(v))
            {
                continue; // skip this triangle cuz it's not in frustum
            }
            // Debug.Log("after clip");

            // stretch clip space info to NDC coord (normalize with w)
            for (int j = 0; j < 3; j++)
            {
                v[j].x /= v[j].w;
                v[j].y /= v[j].w;
                v[j].z /= v[j].w;
            }

            // back face culling, skip triangles on the back, judge by the sequence (clockwise/counter clockwise)
            Vector3 normal = Vector3.Cross(Utility.ToVec3(v[1] - v[0]), Utility.ToVec3(v[2] - v[0]));
            if (-normal.z < 0) // RHC, invert z
            {
                continue;
            }

            // Debug.Log($"{_triangleRendered} triangle rendered");

            // --------- Viewport Transform
            // NDC to screen 
            for (int j = 0; j < 3; j++)
            {
                Vector4 vec = v[j];
                vec.x = 0.5f * (_scrwidth - 1) * (vec.x + 1.0f); // [-1,1] to [0,w]
                vec.y = 0.5f * (_scrheight - 1) * (vec.y + 1.0f); // [-1,1] to [0,h]
                vec.z = vec.z * 0.5f + 0.5f; // [-1,1] (-1=>f, 1=>n) to [0,1] (0=>f, 1=>n), RH Coord
                v[j] = vec;
            }

            for (int j = 0; j < 3; j++)
            {
                tri.vertexes[j].pos = vec4Vertex[j];
                tri.vertexes[j].normal = vertexBuff[ind[j]].objectNormal;
                if (renderingObject.meshUV.Length > 0) // has texture
                {
                    tri.vertexes[j].tex_coord = renderingObject.meshUV[ind[j]];
                }
                tri.vertexes[j].Color = Color.red; // set vertex color to red
                tri.vertexes[j].WorldPos = vertexBuff[ind[j]].worldPos;
                tri.vertexes[j].WorldNormal = vertexBuff[ind[j]].worldNormal;
            }

            // Debug.Log($"Start ras {renderingObject.name}");

            // ---------- Rasterization
            RasterizeTriangle(tri, renderingObject);

            _triangleRendered++;
        }
    }
    
    /// <summary>
    /// Submit color buffer and apply to texture binded by output rawImg
    /// </summary>
    private void Submit()
    {
        texture.SetPixels(_bufColor);
        texture.Apply();
    }

    /// <summary>
    /// this is the shadow casting pass, it collect main camera and move it to light posision and record shadow map to _bufShadow
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="light"></param>
    void ShadowCastingPass(Camera camera, Light light)
    {
        ProfileManager.BeginSample("ShadowCastingPass");

        // get light
        Vector3 lightDir = light.transform.rotation * Vector3.forward;

        // save camera info
        Shadow.SaveMainCameraSettings(ref camera);

        // update shadow info
        Shadow.Update(camera,lightDir);

        // Move camera to light
        Shadow.ConfigCameraToShadowSpace(ref camera, lightDir, orthoDistance, shadowMapResolution);

        // record shadow map

        // revert camera to previous place
        Shadow.RevertMainCameraSettings(ref camera);

        ProfileManager.EndSample();
    }

    /// <summary>
    /// check all 6 plane of frustum, if all vertex of a triangle is out side same plane, condider this triangle out of frustum, do clipping
    /// </summary>
    /// <param name="v">input triangle vertexes</param>
    /// <returns>ture if need clip</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Clipped(Vector4[] v)
    {
        var v0 = v[0];
        var w0 = v0.w >= 0 ? v0.w : -v0.w;
        var v1 = v[1];
        var w1 = v1.w >= 0 ? v1.w : -v1.w;
        var v2 = v[2];
        var w2 = v2.w >= 0 ? v2.w : -v2.w;

        //left
        if (v0.x < -w0 && v1.x < -w1 && v2.x < -w2)
        {
            return true;
        }
        //right
        if (v0.x > w0 && v1.x > w1 && v2.x > w2)
        {
            return true;
        }
        //bottom
        if (v0.y < -w0 && v1.y < -w1 && v2.y < -w2)
        {
            return true;
        }
        //top
        if (v0.y > w0 && v1.y > w1 && v2.y > w2)
        {
            return true;
        }
        //near
        if (v0.z < -w0 && v1.z < -w1 && v2.z < -w2)
        {
            return true;
        }
        //far
        if (v0.z > w0 && v1.z > w1 && v2.z > w2)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Use cross product to check if all vertex not in triangle, if all vertex outside of triangle, clip
    /// However if triangle extreme big and all vertex outside of camera, it will clip the triangle, resulting false clip
    /// </summary>
    /// <param name="v">input triangle vertexes</param>
    /// <returns>ture if need clip</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool CrossProdClipped(Vector4[] v)
    {
        for (int i = 0; i < 3; ++i)
        {
            var vertex = v[i];
            var w = vertex.w;
            w = w >= 0 ? w : -w;

            bool inside = (vertex.x <= w && vertex.x >= -w
                && vertex.y <= w && vertex.y >= -w
                && vertex.z <= w && vertex.z >= -w);

            if (inside)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Rasterize the triangle and write result in _bufColor
    /// </summary>
    /// <param name="t"></param>
    /// <param name="renderingObject"></param>
    private void RasterizeTriangle(Triangle t, RenderingObject renderingObject)
    {
        // assign vertex pos
        var v = vec4Vertex;
        for (int i = 0; i < 3; i++)
        {
            v[i] = t.vertexes[i].pos;
        }
        int minX, minY, maxX, maxY;
        GetBoundingBox(v, out minX, out minY, out maxX, out maxY);
        // Debug.Log($"BoudingBox: [{minX},{maxX}][{minY},{maxY}]");

        // acreen [0, _scrwidth-1] [0, _scrheight-1]
        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                // Debug.Log($"At {x}, {y}");
                Vector3 baryCoord = ComputeBarycentric2D(x, y, t);
                float alpha = baryCoord.x;
                float beta = baryCoord.y;
                float gamma = baryCoord.z;
                /// judge if inside triangle, Games 101 use cross product and see if they are of same direction,
                ///  but that is slower than check if barycentric coord < 0
                if (alpha < 0 || beta < 0 || gamma < 0)
                {
                    continue;
                }
                // Debug.Log($"({x}, {y}) in triangle");

                // ---------- Perspective Correct Interpolation for z value in world coord
                /// the ratial on screen is distorted after projection transformation, 
                /// and is not a linear mapping to original coord. However, take a look at projection matrix, 1/z is lineared mapped to original,
                /// ref Fundamentals of computer Graphics, Fourth edition, Chapter 11, Texture mapping, P256
                /// I Think there is something wrong with formula (11.3), l_s should be alpha(1/w_0) + beta(1/w_1) + gamma(1/w_2), instead of 2* gamma/w_2
                /// 
                float ls = 1.0f / (alpha / v[0].w + beta / v[1].w + 1 * gamma / v[2].w);
                float correctZ = (alpha * v[0].z / v[0].w + beta * v[1].z / v[1].w + gamma * v[2].z / v[2].w) * ls;

                // ---------- Depth Test near plane = 1 far plane = 0
                int index = y * _scrwidth + x;
                if (correctZ >= _bufDepth[index])
                {
                    // ----- Shadow test
                    /// this place is left for shadow test to be added, normally inside fragment shader but add it here will be faster
                    /// check distance to light with shadow map, fail this test will color the pixel as shadow


                    _bufDepth[index] = correctZ;
                    // ---------- Perspective Correct Interpolation for texture coord, same formula as interpolate z
                    ProfileManager.BeginSample("Perspective Correct Interpolation");
                    Vector2 correctUV = (alpha * t.vertexes[0].tex_coord / v[0].w +
                                            beta * t.vertexes[1].tex_coord / v[1].w +
                                            gamma * t.vertexes[2].tex_coord / v[2].w) * ls;
                    Vector3 correctNormal = (alpha * t.vertexes[0].normal / v[0].w +
                                            beta * t.vertexes[1].normal / v[1].w +
                                            gamma * t.vertexes[2].normal / v[2].w) * ls;
                    Vector3 correctWorldPos = (alpha * t.vertexes[0].WorldPos / v[0].w +
                                            beta * t.vertexes[1].WorldPos / v[1].w +
                                            gamma * t.vertexes[2].WorldPos / v[2].w) * ls;
                    Vector3 correctWorldNormal = (alpha * t.vertexes[0].WorldNormal / v[0].w +
                                            beta * t.vertexes[1].WorldNormal / v[1].w +
                                            gamma * t.vertexes[2].WorldNormal / v[2].w) * ls;
                    Color correctColor = (alpha * t.vertexes[0].Color / v[0].w +
                                            beta * t.vertexes[1].Color / v[1].w +
                                            gamma * t.vertexes[2].Color / v[2].w) * ls;

                    ProfileManager.EndSample();

                    payload.UV = correctUV;
                    payload.objNormal = correctNormal;
                    payload.worldPos = correctWorldPos;
                    payload.worldNormal = correctWorldNormal;
                    payload.color = correctColor;
                    payload.texture = renderingObject.tex;
                    payload.texWidth = renderingObject.tex.width;
                    payload.texHeight = renderingObject.tex.height;
                    payload.useBilinear = false;

                    ProfileManager.BeginSample("FragmentShader");
                    _bufColor[index] = FragmentShader.shaderBlinnPhong(payload, finfo);
                    // _bufColor[index] = FragmentShader.shaderUnlit(payload);
                    ProfileManager.EndSample();
                }
            }
        }
    }

    /// <summary>
    /// Get the bounding box of input vector array (primitive)
    /// </summary>
    /// <param name="vec">input vector array</param>
    /// <param name="minX">output minX</param>
    /// <param name="minY">output maxX</param>
    /// <param name="maxX">output minY</param>
    /// <param name="maxY">output maxY</param>
    private void GetBoundingBox(Vector4[] vec, out int minX, out int minY, out int maxX, out int maxY)
    {
        // calculate bounding box
        float fminX, fmaxX, fminY, fmaxY;
        fminX = vec4Vertex[0].x;
        fmaxX = fminX;
        fminY = vec4Vertex[0].y;
        fmaxY = fminY;

        for (int i = 1; i < 3; ++i)
        {
            float x = vec4Vertex[i].x;
            fminX = x < fminX ? x : fminX;
            fmaxX = x > fmaxX ? x : fmaxX;
            float y = vec4Vertex[i].y;
            fminY = y < fminY ? y : fminY;
            fmaxY = y > fmaxY ? y : fmaxY;
        }

        // clamp to int, cuz pixels are discrete
        minX = Mathf.FloorToInt(fminX);
        maxX = Mathf.CeilToInt(fmaxX);
        minY = Mathf.FloorToInt(fminY);
        maxY = Mathf.CeilToInt(fmaxY);

        /// because of out clipping method doesnt cut all vertex outside screen space, 
        /// we have to handle them manually and clamp bounding box inside screen
        minX = minX < 0 ? 0 : minX;
        maxX = maxX > _scrwidth ? _scrwidth : maxX;
        minY = minY < 0 ? 0 : minY;
        maxY = maxY > _scrheight ? _scrheight : maxY;

    }

    /// <summary>
    /// Compute the Barycentric coord inside triangle
    /// </summary>
    /// <param name="x">x coord on screen space</param>
    /// <param name="y">y coord on screen space</param>
    /// <param name="tri">the triangle</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3 ComputeBarycentric2D(float x, float y, Triangle t)
    {
        Vector4[] v = new Vector4[3];
        v[0] = t.vertexes[0].pos;
        v[1] = t.vertexes[1].pos;
        v[2] = t.vertexes[2].pos;

        float c1 = (x * (v[1].y - v[2].y) + (v[2].x - v[1].x) * y + v[1].x * v[2].y - v[2].x * v[1].y) / (v[0].x * (v[1].y - v[2].y) + (v[2].x - v[1].x) * v[0].y + v[1].x * v[2].y - v[2].x * v[1].y);
        float c2 = (x * (v[2].y - v[0].y) + (v[0].x - v[2].x) * y + v[2].x * v[0].y - v[0].x * v[2].y) / (v[1].x * (v[2].y - v[0].y) + (v[0].x - v[2].x) * v[1].y + v[2].x * v[0].y - v[0].x * v[2].y);
        float c3 = (x * (v[0].y - v[1].y) + (v[1].x - v[0].x) * y + v[0].x * v[1].y - v[1].x * v[0].y) / (v[2].x * (v[0].y - v[1].y) + (v[1].x - v[0].x) * v[2].y + v[0].x * v[1].y - v[1].x * v[0].y);

        return new Vector3(c1, c2, c3);
    }

}
