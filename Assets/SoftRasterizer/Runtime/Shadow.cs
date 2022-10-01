using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class contains methods used for shadow map calculation, IT IT NOT FINISHED YET
/// </summary>
public class Shadow
{
    struct MainCameraSettings
    {
        public Vector3 position;
        public Quaternion rotation;
        public float nearClipPlane;
        public float farClipPlane;
        public float aspect;
    };
    static MainCameraSettings settings;

    // main cam frustum
    static Vector3[] farCorners = new Vector3[4];
    static Vector3[] nearCorners = new Vector3[4];
    static Vector3[] box;
    public static float orthoWidths;


    static Vector3 matTransform(Matrix4x4 m, Vector3 v, float w)
    {
        Vector4 v4 = new Vector4(v.x, v.y, v.z, w);
        v4 = m * v4;
        return new Vector3(v4.x, v4.y, v4.z);
    }

    static Vector3[] LightSpaceAABB(Vector3[] nearCorners, Vector3[] farCorners, Vector3 lightDir)
    {
        Matrix4x4 toShadowViewInv = Matrix4x4.LookAt(Vector3.zero, lightDir, Vector3.up);
        Matrix4x4 toShadowView = toShadowViewInv.inverse;

        // turn frustum to light direction
        for (int i = 0; i < 4; i++)
        {
            farCorners[i] = matTransform(toShadowView, farCorners[i], 1.0f);
            nearCorners[i] = matTransform(toShadowView, nearCorners[i], 1.0f);
        }

        // calculate axis-aligned bounding boxes (AABB) 
        float[] x = new float[8];
        float[] y = new float[8];
        float[] z = new float[8];
        for (int i = 0; i < 4; i++)
        {
            x[i] = nearCorners[i].x; x[i + 4] = farCorners[i].x;
            y[i] = nearCorners[i].y; y[i + 4] = farCorners[i].y;
            z[i] = nearCorners[i].z; z[i + 4] = farCorners[i].z;
        }
        float xmin = Mathf.Min(x), xmax = Mathf.Max(x);
        float ymin = Mathf.Min(y), ymax = Mathf.Max(y);
        float zmin = Mathf.Min(z), zmax = Mathf.Max(z);

        // translate bondingbox verteces to world coord
        Vector3[] points = {
            new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymin, zmax), new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymax, zmax),
            new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymax, zmax)
        };
        for (int i = 0; i < 8; i++)
            points[i] = matTransform(toShadowViewInv, points[i], 1.0f);

        // return frustum
        for (int i = 0; i < 4; i++)
        {
            farCorners[i] = matTransform(toShadowViewInv, farCorners[i], 1.0f);
            nearCorners[i] = matTransform(toShadowViewInv, nearCorners[i], 1.0f);
        }

        return points;
    }

    /// <summary>
    /// save cam propertiy and change to ortho
    /// </summary>
    /// <param name="camera"></param>
    public static void SaveMainCameraSettings(ref Camera camera)
    {
        settings.position = camera.transform.position;
        settings.rotation = camera.transform.rotation;
        settings.farClipPlane = camera.farClipPlane;
        settings.nearClipPlane = camera.nearClipPlane;
        settings.aspect = camera.aspect;
        camera.orthographic = true;
    }

    /// <summary>
    /// take back previews properties and change to perspective
    /// </summary>
    /// <param name="camera"></param>
    public static void RevertMainCameraSettings(ref Camera camera)
    {
        camera.transform.position = settings.position;
        camera.transform.rotation = settings.rotation;
        camera.farClipPlane = settings.farClipPlane;
        camera.nearClipPlane = settings.nearClipPlane;
        camera.aspect = settings.aspect;
        camera.orthographic = false;
    }

    public static void Update(Camera mainCam, Vector3 lightDir)
    {
        // get main cam frustum
        mainCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), mainCam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, farCorners);
        mainCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), mainCam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nearCorners);

        // vertex coord to world coord
        for (int i = 0; i < 4; i++)
        {
            farCorners[i] = mainCam.transform.TransformVector(farCorners[i]) + mainCam.transform.position;
            nearCorners[i] = mainCam.transform.TransformVector(nearCorners[i]) + mainCam.transform.position;
        }

        // calculate bounding box
        box = LightSpaceAABB(nearCorners, farCorners, lightDir);


        // update Ortho width
        orthoWidths = Vector3.Magnitude(farCorners[2] - nearCorners[0]);

    }

    public static void ConfigCameraToShadowSpace(ref Camera camera, Vector3 lightDir, float distance, float resolution)
    {
        var f_near = new Vector3[4]; var f_far = new Vector3[4];
        f_near = nearCorners;
        f_far = farCorners;

        // get box center and aspect ratial
        Vector3 center = (box[3] + box[4]) / 2;
        float w = Vector3.Magnitude(box[0] - box[4]);
        float h = Vector3.Magnitude(box[0] - box[2]);
        //float len = Mathf.Max(h, w);
        float len = Vector3.Magnitude(f_far[2] - f_near[0]);
        float disPerPix = len / resolution;

        Matrix4x4 toShadowViewInv = Matrix4x4.LookAt(Vector3.zero, lightDir, Vector3.up);
        Matrix4x4 toShadowView = toShadowViewInv.inverse;

        // Corrdinate Transform
        center = matTransform(toShadowView, center, 1.0f);
        for (int i = 0; i < 3; i++)
            center[i] = Mathf.Floor(center[i] / disPerPix) * disPerPix;
        center = matTransform(toShadowViewInv, center, 1.0f);

        // Set camera
        camera.transform.rotation = Quaternion.LookRotation(lightDir);
        camera.transform.position = center;
        camera.nearClipPlane = -distance;
        camera.farClipPlane = distance;
        camera.aspect = 1.0f;
        camera.orthographicSize = len * 0.5f;
    }
}
