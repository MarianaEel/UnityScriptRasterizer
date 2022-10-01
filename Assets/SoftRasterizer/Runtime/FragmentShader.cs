using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


/// <summary>
/// This structure holds shader input triangle verteces information
/// </summary>
public struct FragShaderPayload
{
    public Vector3 worldPos;
    public Vector3 worldNormal;
    public Vector3 objNormal;
    public Color color;
    public Vector2 UV;
    public Texture2D texture;
    public int texWidth;
    public int texHeight;
    public bool useBilinear;
}

/// <summary>
/// This structure holds world camera position, light direction, light color, and ambient color
/// </summary>
public struct FragShaderWorldInfo
{
    public Vector3 worldSpaceCameraPos;
    public Vector3 worldSpaceLightDir;
    public Color lightColor;
    public Color ambientColor;
}

public class FragmentShader
{
    /// <summary>
    /// Basic unlit shader for debug purpose
    /// </summary>
    /// <param name="payload">triangle information</param>
    /// <returns>magenta color</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color shaderUnlit(FragShaderPayload payload)
    {
        // Debug.Log("calling Unlit");
        return Color.magenta;
    }

    /// <summary>
    /// Blinn Phong shader, need a FragShaderPayload of triangle information and a FragShaderWorldInfo of world information (camera, light) to shade
    /// </summary>
    /// <param name="payload">triangle information payload</param>
    /// <param name="info">world information payload</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color shaderBlinnPhong(FragShaderPayload payload, FragShaderWorldInfo info)
    {
        // Debug.Log("Calling FragShader");
        Color textureColor;
        int w = payload.texWidth;
        int h = payload.texHeight;

        if (payload.useBilinear)
        {
            float u_img = payload.UV.x * (w - 1);
            int u_img_i = (int)(u_img);
            int u0 = u_img < u_img_i + 0.5 ? u_img_i - 1 : u_img_i;
            if (u0 < 0) u0 = 0;
            int u1 = u0 + 1;
            float s = u_img - (u0 + 0.5f);

            float v_img = payload.UV.y * (h - 1);
            int v_img_i = (int)(v_img);
            int v0 = v_img < v_img_i + 0.5 ? v_img_i - 1 : v_img_i;
            if (v0 < 0) v0 = 0;
            int v1 = v0 + 1;
            float t = v_img - (v0 + 0.5f);

            var color_00 = GetTextureColor(payload.texture, u0, v0);
            var color_10 = GetTextureColor(payload.texture, u1, v0);
            var color_0 = Color.Lerp(color_00, color_10, s);

            var color_01 = GetTextureColor(payload.texture, u0, v1);
            var color_11 = GetTextureColor(payload.texture, u1, v1);
            var color_1 = Color.Lerp(color_01, color_11, s);

            textureColor = Color.Lerp(color_0, color_1, t);
        }
        else
        {
            int x = (int)((w - 1) * payload.UV.x);
            int y = (int)((h - 1) * payload.UV.y);
            textureColor = GetTextureColor(payload.texture, x, y);
            // textureColor = Color.black;
            // Debug.Log($"Got texture Color: {textureColor}");
        }

        Color ambient = info.ambientColor;

        Color ks = new Color(0.7f, 0.7f, 0.7f);

        float ndotl = Vector3.Dot(payload.worldNormal, info.worldSpaceLightDir);
        Color diffuse = textureColor * info.lightColor * Mathf.Max(0f, ndotl);

        Vector3 viewDir = info.worldSpaceCameraPos - payload.worldPos;
        viewDir.Normalize();
        Vector3 halfDir = (viewDir + info.worldSpaceLightDir);
        halfDir.Normalize();
        float hdotn = Vector3.Dot(halfDir, payload.worldNormal);
        Color specular = ks * info.lightColor * Mathf.Pow(Mathf.Max(0f, hdotn), 150);

        // return ambient + diffuse + specular;
        // Debug.Log($"Fragment Shader Color: {diffuse + specular}");
        return diffuse + specular;
    }

    /// <summary>
    /// Return texture color at x, y
    /// </summary>
    /// <param name="texture">input texture</param>
    /// <param name="x">x coord</param>
    /// <param name="y">y coord</param>
    /// <returns>Color at texture(x,y)</returns>
    static Color GetTextureColor(Texture2D texture, int x, int y)
    {
        Color c = texture.GetPixel(x, y);
        return c;
    }
}
