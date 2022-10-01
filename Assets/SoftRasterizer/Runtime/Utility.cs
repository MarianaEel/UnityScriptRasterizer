using UnityEngine;
using System;

/// <summary>
/// This class contains static methods of vertex processing, view&projection matrix setting and frustum culling
/// </summary>
public class Utility
{
    const float MY_PI = 3.1415926f;
    const float ToRadian = MY_PI / 180.0f;
    static Vector4[] s_tmp8Vector4s = new Vector4[8];



    /// <summary>
    /// Transform to Homogenous Coord.
    /// </summary>
    /// <param name="inputVector">input vector</param>
    /// <returns> Vector4 (inputvector, 1) </returns>
    public static Vector4 ToVec4(Vector3 inputVector)
    {
        return new Vector4(inputVector.x, inputVector.y, inputVector.z, 1);
    }
    /// <summary>
    /// Transform a Vec4 to Vec3, w is lost during operation
    /// </summary>
    /// <param name="inputVector">input vector4</param>
    /// <returns>output vector3</returns>
    public static Vector3 ToVec3(Vector4 inputVector)
    {
        return new Vector3(inputVector.x, inputVector.y, inputVector.z);
    }
    /// <summary>
    /// Fill an array of typename T with given value
    /// </summary>
    /// <param name="arr">array input</param>
    /// <param name="value">value to fill array</param>
    /// <typeparam name="T">input type</typeparam>
    public static void FillArray<T>(T[] arr, T value)
    {
        int length = arr.Length;
        if (length == 0)
        {
            return;
        }
        arr[0] = value;

        int arrayHalfLen = length / 2;
        int copyLength;
        for (copyLength = 1; copyLength <= arrayHalfLen; copyLength <<= 1)
        {
            Array.Copy(arr, 0, arr, copyLength, copyLength);
        }
        Array.Copy(arr, 0, arr, copyLength, length - copyLength);
    }

    /// <summary>
    /// SetupmatVP setup View and Projection matrix using camera and aspect ratial
    /// </summary>
    /// <param name="camera">input camera </param>
    /// <param name="aspratial">the aspect ratial of the camera</param>
    /// <param name="matView">the output View Matrix</param>
    /// <param name="matProjection">the output Projection Matrix</param>
    public static void SetupmatVP
        (Camera camera, float aspratial, out Matrix4x4 matView, out Matrix4x4 matProjection)
    {
        var camPos = camera.transform.position;
        camPos.z *= -1;
        var lookDir = camera.transform.forward;
        lookDir.z *= 1;
        var upDir = camera.transform.up;
        upDir.z *= -1;

        // matView = GetMatView(camPos, lookDir, upDir);
        Transform camT = camera.transform;
        matView = Matrix4x4.TRS(camT.position, camT.rotation, Vector3.one).inverse;
        matView.m20 = -matView.m20;
        matView.m21 = -matView.m21;
        matView.m22 = -matView.m22;
        matView.m23 = -matView.m23;

        if (camera.orthographic)
        {
            float halfOrthHeight = camera.orthographicSize;
            float halfOrthWidth = halfOrthHeight * aspratial;
            float zFar = -camera.farClipPlane; // minus for RH coord
            float zNear = -camera.nearClipPlane;
            // matProjection = GetMatOrthProj(-halfOrthWidth, halfOrthWidth, -halfOrthHeight, halfOrthHeight, zFar, zNear);
            matProjection = camera.projectionMatrix;
            matProjection.m20 = -matProjection.m20;
            matProjection.m21 = -matProjection.m21;
            matProjection.m22 = -matProjection.m22;
            matProjection.m23 = -matProjection.m23;
        }
        else
        {
            float zNear = -camera.nearClipPlane;
            float zFar = -camera.farClipPlane;
            float halfPersHeight = Mathf.Abs(zNear) * Mathf.Tan(0.5f * camera.fieldOfView * ToRadian);
            float halfPersWidth = halfPersHeight * aspratial;
            // matProjection = GetMatPersProj(-halfPersWidth, halfPersWidth, -halfPersHeight, halfPersHeight, zFar, zNear); // broken method
            matProjection = camera.projectionMatrix;
            matProjection.m20 = -matProjection.m20;
            matProjection.m21 = -matProjection.m21;
            matProjection.m22 = -matProjection.m22;
            matProjection.m23 = -matProjection.m23;
        }
    }

    /// <summary>
    /// Returns View Matrix of the camera, BROKEN METHOD NEED TO BE FIXED
    /// </summary>
    /// <param name="eye_pos">camera position</param>
    /// <param name="lookDir">camera look direction</param>
    /// <param name="upDir">camera up direction</param>
    /// <returns></returns>
    public static Matrix4x4 GetMatView(Vector3 eye_pos, Vector3 lookDir, Vector3 upDir)
    {
        Vector3 camZ = -lookDir.normalized; // camera looking at -z
        Vector3 camY = upDir.normalized;
        Vector3 camX = Vector3.Cross(camY, camZ);
        camY = Vector3.Cross(camZ, camX);
        Matrix4x4 matRotation = Matrix4x4.identity;
        matRotation.SetColumn(0, camX);
        matRotation.SetColumn(1, camY);
        matRotation.SetColumn(2, camZ);

        Matrix4x4 matTranslation = Matrix4x4.identity;
        // matTranslation.SetColumn(3, new Vector4(-eye_pos.x, -eye_pos.y, -eye_pos.z, 1f));
        matTranslation.SetColumn(3, new Vector4(eye_pos.x, eye_pos.y, eye_pos.z, 1f));

        Matrix4x4 view = matRotation.transpose * matTranslation;

        return view;
    }

    /// <summary>
    /// Return perspective matrix
    /// BROKEN METHOD NEED TO BE FIXED, use camera.projectionMatrix to get Projection matrix instead , kept for future debugging
    /// /// </summary>
    /// <returns>projectionMatrix/returns>
    public static Matrix4x4 GetMatPersProj(float l, float r, float b, float t, float f, float n)
    {
        Matrix4x4 matPersp2Ortho = Matrix4x4.identity;
        matPersp2Ortho.m00 = n;
        matPersp2Ortho.m11 = n;
        matPersp2Ortho.m22 = n + f;
        matPersp2Ortho.m23 = -n * f;
        matPersp2Ortho.m32 = 1.0f;
        matPersp2Ortho.m33 = 0.0f;
        Matrix4x4 matOrthProj = GetMatOrthProj(l, r, b, t, f, n);
        Matrix4x4 matProjection = matOrthProj * matPersp2Ortho;
        return matProjection;
    }

    /// <summary>
    /// Return orthographic matrix
    /// BROKEN METHOD NEED TO BE FIXED, use camera.projectionMatrix to get Projection matrix instead , kept for future debugging
    /// /// </summary>
    /// <returns>projectionMatrix/returns>
    public static Matrix4x4 GetMatOrthProj(float l, float r, float b, float t, float f, float n)
    {
        Matrix4x4 matTranslation = Matrix4x4.identity;
        matTranslation.SetColumn(3, new Vector4(-(r + l) * 0.5f, -(t + b) * 0.5f, -(n + f) * 0.5f, 1f));
        Matrix4x4 matScale = Matrix4x4.identity;
        matScale.m00 = 2.0f / (r - l); // width
        matScale.m11 = 2.0f / (t - b); // height
        matScale.m22 = 2.0f / (n - f); // length, n-f cuz lookat -z, far is smaller
        return matScale * matTranslation;
    }
    
    /// <summary>
    /// BROKEN METHOD NEED TO BE FIXED
    /// </summary>
    /// <param name="rotation_angle"></param>
    /// <returns></returns>
    public static Matrix4x4 GetRotZMatrix(float rotation_angle)
    {
        Matrix4x4 model = Matrix4x4.identity;

        float cs = Mathf.Cos(rotation_angle * ToRadian);
        float si = Mathf.Sin(rotation_angle * ToRadian);

        model.m00 = cs;
        model.m01 = -si;
        model.m10 = si;
        model.m11 = cs;
        return model;
    }

    /// <summary>
    /// Turn vector scale to matrix scale matrix
    /// </summary>
    /// <param name="scale">vector scale input</param>
    /// <returns> Matrix4x4 scale matrix </returns>
    public static Matrix4x4 GetScaleMatrix(Vector3 scale)
    {
        Matrix4x4 matScale = Matrix4x4.identity;
        matScale.m00 = scale.x;
        matScale.m11 = scale.y;
        matScale.m22 = scale.z;
        return matScale;
    }

    /// <summary>
    /// Get the rotation matrix, unity store rotation information as quaternions, 
    /// this function convert quaternions to euler angle and construct rotatin matrix.
    /// </summary>
    /// <param name="axis">input axix</param>
    /// <param name="angle">input angle</param>
    /// <returns></returns>
    public static Matrix4x4 GetRotationMatrix(Vector3 axis, float angle)
    {
        Vector3 vx = new Vector3(1, 0, 0);
        Vector3 vy = new Vector3(0, 1, 0);
        Vector3 vz = new Vector3(0, 0, 1);

        axis.Normalize();
        float radius = angle * ToRadian;

        var tx = RotateVector(axis, vx, radius);
        var ty = RotateVector(axis, vy, radius);
        var tz = RotateVector(axis, vz, radius);

        Matrix4x4 rotMat = Matrix4x4.identity;
        rotMat.SetColumn(0, tx);
        rotMat.SetColumn(1, ty);
        rotMat.SetColumn(2, tz);
        return rotMat;
    }

    //Get the result of rotate a vector around axis with angle radius.
    //axis must be normalized.
    public static Vector3 RotateVector(Vector3 axis, Vector3 v, float radius)
    {
        Vector3 v_parallel = Vector3.Dot(axis, v) * axis;
        Vector3 v_vertical = v - v_parallel;
        float v_vertical_len = v_vertical.magnitude;

        Vector3 a = axis;
        Vector3 b = v_vertical.normalized;
        Vector3 c = Vector3.Cross(a, b);

        Vector3 v_vertical_rot = v_vertical_len * (Mathf.Cos(radius) * b + Mathf.Sin(radius) * c);
        return v_parallel + v_vertical_rot;
    }
    public static Matrix4x4 GetTranslationMatrix(Vector3 translate)
    {
        Matrix4x4 matTranslation = Matrix4x4.identity;
        matTranslation.SetColumn(3, new Vector4(translate.x, translate.y, -translate.z, 1));
        return matTranslation;
    }
    
    /// <summary>
    /// check vertices out of frustum clip space, called in frustumculling
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static bool CheckVerticesOutFrustumClipSpace(Vector4[] v)
    {
        //left
        int cnt = 0;
        for (int i = 0; i < 8; ++i)
        {
            var w = v[i].w >= 0 ? v[i].w : -v[i].w;
            if (v[i].x < -w)
            {
                ++cnt;
            }
            if (cnt == 8)
            {
                return true;
            }
        }
        //right
        cnt = 0;
        for (int i = 0; i < 8; ++i)
        {
            var w = v[i].w >= 0 ? v[i].w : -v[i].w;
            if (v[i].x > w)
            {
                ++cnt;
            }
            if (cnt == 8)
            {
                return true;
            }
        }
        //bottom
        cnt = 0;
        for (int i = 0; i < 8; ++i)
        {
            var w = v[i].w >= 0 ? v[i].w : -v[i].w;
            if (v[i].y < -w)
            {
                ++cnt;
            }
            if (cnt == 8)
            {
                return true;
            }
        }
        //top
        cnt = 0;
        for (int i = 0; i < 8; ++i)
        {
            var w = v[i].w >= 0 ? v[i].w : -v[i].w;
            if (v[i].y > w)
            {
                ++cnt;
            }
            if (cnt == 8)
            {
                return true;
            }
        }
        //near
        cnt = 0;
        for (int i = 0; i < 8; ++i)
        {
            var w = v[i].w >= 0 ? v[i].w : -v[i].w;
            if (v[i].z < -w)
            {
                ++cnt;
            }
            if (cnt == 8)
            {
                return true;
            }
        }
        //far
        cnt = 0;
        for (int i = 0; i < 8; ++i)
        {
            var w = v[i].w >= 0 ? v[i].w : -v[i].w;
            if (v[i].z > w)
            {
                ++cnt;
            }
            if (cnt == 8)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// This method do frustum culling
    /// </summary>
    /// <param name="localAABB">mesh.bound of input mesh</param>
    /// <param name="mvp">model view projection matrix</param>
    /// <returns>ture if need culling (ver)</returns>
    public static bool FrustumCulling(Bounds localAABB, Matrix4x4 mvp)
    {
        var v = s_tmp8Vector4s;
        var min = localAABB.min; min.z = -min.z;
        var max = localAABB.max; max.z = -max.z;
        v[0] = mvp * new Vector4(min.x, min.y, min.z, 1.0f);
        v[1] = mvp * new Vector4(min.x, min.y, max.z, 1.0f);
        v[2] = mvp * new Vector4(min.x, max.y, min.z, 1.0f);
        v[3] = mvp * new Vector4(min.x, max.y, max.z, 1.0f);
        v[4] = mvp * new Vector4(max.x, min.y, min.z, 1.0f);
        v[5] = mvp * new Vector4(max.x, min.y, max.z, 1.0f);
        v[6] = mvp * new Vector4(max.x, max.y, min.z, 1.0f);
        v[7] = mvp * new Vector4(max.x, max.y, max.z, 1.0f);

        return CheckVerticesOutFrustumClipSpace(v);
    }

}