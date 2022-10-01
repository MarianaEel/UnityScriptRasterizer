using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class CameraRenderer : MonoBehaviour
{
    public RawImage outImg;

    private Camera _camera;
    private List<RenderingObject> _renderingObjects = new List<RenderingObject>();
    private Light _light;

    Rasterizer rasterizer;

    ScriptableRenderContext context;
    const string bufferName = "Render Camera";
    CullingResults cullingResults;

    /// <summary>
    /// start function is the initializer of camera object,
    /// it will first pass camera input to private _camera,
    /// then go search every object in the scene and add to renderingObject list for later use
    /// </summary>
    /// <param name="camera"> camera reference </param>
    private void Start()
    {
        // _camera = GetComponent<Camera>();
        _camera = Camera.main;
        // Debug.Log(_camera.transform.position);
        var root = this.gameObject.scene.GetRootGameObjects();
        _renderingObjects.Clear();
        foreach (var obj in root)
        {
            _renderingObjects.AddRange(obj.GetComponentsInChildren<RenderingObject>());
        }
        Debug.Log($"Find rendering objs count:{_renderingObjects.Count}");

        RectTransform rect = outImg.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(Screen.width, Screen.height);
        int width = Mathf.FloorToInt(rect.rect.width);
        int height = Mathf.FloorToInt(rect.rect.height);
        Debug.Log("screen size: " + width + "x" + height);

        _light = GameObject.Find("Directional Light").GetComponent<Light>();
        Debug.Log($"Light : {_light.name}; Camera: {_camera.name}; Object count: {_renderingObjects.Count}");

        rasterizer = new Rasterizer(width, height);

        outImg.texture = rasterizer.texture;
    }

    /// <summary>
    /// OnPostRender is called before engine tell camera to render object.  
    /// </summary>
    private void OnPostRender()
    {
        // Debug.Log("on post render called");
        rasterizer.Render(_camera, _light, _renderingObjects);
    }



}