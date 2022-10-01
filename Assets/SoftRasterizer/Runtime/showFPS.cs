using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// This class calculate framerate every Update()
/// </summary>
public class showFPS : MonoBehaviour
{
    public Text fpsText;
    public int fontSize = 20;
    public Color textColor = Color.blue;
    GUIStyle style;
    public float SampleTime = 1f;
    int frameCount;
    float timeTotal;
    string textDisplay;

    private void Start()
    {
        frameCount = 0;
        timeTotal = 0;
        textDisplay = "";

        if (fpsText != null)
        {
            fpsText.fontSize = fontSize;
            fpsText.color = textColor;
        }
    }
    private void Awake()
    {
        style = new GUIStyle();
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
    }
    void Update()
    {
        frameCount++;
        timeTotal += Time.unscaledDeltaTime;
        if (timeTotal >= SampleTime)
        {
            float fps = frameCount / timeTotal;
            textDisplay = $"FPS : {fps.ToString()}";
            frameCount = 0;
            timeTotal = 0;
            if (fpsText != null)
            {
                if (fpsText.fontSize != fontSize)
                {
                    fpsText.fontSize = fontSize;
                }
                if (fpsText.color != textColor)
                {
                    fpsText.color = textColor;
                }
                fpsText.text = textDisplay;
            }
        }

    }
    private void OnGUI()
    {
        if (fpsText == null)
        {
            GUI.Label(new Rect(10, 10, 200, 100), textDisplay, style);
        }
    }
}