using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleManager : MonoBehaviour
{
    public Camera cam;
    public ComputeShader signalProcessor;
    public ComputeShader textureRenderer;
    public Transform background;
    public Material bgMaterial;
    // Resolution of blocks used in marching cubes, higher res = better picture
    [Range(10, 400)]
    public int resolutionY;
    private int prevResY;
    private int resolutionX;
    private int pointSeparation;
    // All metacircles in the simulation
    private Metacircle[]  circles;
    // Stores the summation of all circle signal values for marching cubes
    private ValuePoint[] signalMap;
    private RenderTexture texture;
    public int lineThickness;
    public bool displayField;
    public bool interpolate;
    private const int growthSpeed = 5;

    public struct ValuePoint
    {
        public int posX;
        public int posY;
        public float val;
    }
    
    void Start()
    {
        circles = GetComponentsInChildren<Metacircle>();

        foreach (Metacircle circle in circles)
        {
            circle.velocity = new int[] { Random.Range(20, 50) * (Random.Range(0, 2) == 1 ? 1 : -1), Random.Range(20, 50) * (Random.Range(0, 2) == 1? 1 : -1) };
            circle.radius = Random.Range(20f, 40f);
            circle.growth = (Random.Range(0, 2) == 1 ? 1 : -1);
        }
    }

    void Update()
    {

        if (prevResY != resolutionY)
            calculateGrid();

        foreach (Metacircle circle in circles)
        {
            Vector3 newPixelPos = cam.WorldToScreenPoint(circle.transform.position) + new Vector3 (circle.velocity[0], circle.velocity[1], 0) * Time.deltaTime;

            if (newPixelPos.x - circle.radius < 0) 
            {
                newPixelPos.x = circle.radius;
                circle.velocity[0] *= -1;
            }
            else if (newPixelPos.x + circle.radius > Screen.width)
            {
                newPixelPos.x = Screen.width - circle.radius;
                circle.velocity[0] *= -1;
            }

            if (newPixelPos.y - circle.radius < 0) 
            {
                newPixelPos.y = circle.radius;
                circle.velocity[1] *= -1;
            }
            else if (newPixelPos.y + circle.radius > Screen.height) 
            {
                newPixelPos.y = Screen.height - circle.radius;
                circle.velocity[1] *= -1;
            }

            circle.transform.position = cam.ScreenToWorldPoint(newPixelPos);

            circle.radius += growthSpeed * circle.growth * Time.deltaTime;

            if (circle.radius < 20f)
            {
                circle.radius = 20f;
                circle.growth *= -1;
            }
            else if (circle.radius > 40f)
            {
                circle.radius = 40f;
                circle.growth *= -1;
            }
        }

        renderGraphics();
    }

    private void OnDrawGizmos() 
    {
        for (int i = 0; i < resolutionX; i++)
        {
            for (int j = 0; j < resolutionY; j++)
            {
                Gizmos.color = (signalMap[i * resolutionY + j].val >= 1 ? Color.green : Color.red);

                Gizmos.DrawSphere(cam.ScreenToWorldPoint(new Vector3(i * pointSeparation, j * pointSeparation, 1)) - cam.transform.position.z * Vector3.forward, 0.1f);
            }
        }
    }

    private void calculateGrid()
    {
        prevResY = resolutionY;
        
        resolutionX = (Screen.width * (resolutionY - 1)) / Screen.height + 2;
        pointSeparation = Screen.height / (resolutionY - 1) + 1;

        int[] dimensions = new int[] { (resolutionX - 1) * pointSeparation, (resolutionY - 1) * pointSeparation };
        int[] screenOverlap = new int[] { dimensions[0] - Screen.width, dimensions[1] - Screen.height };

        signalMap = new ValuePoint[resolutionX * resolutionY];

        for (int i = 0; i < resolutionX; i++)
            for (int j = 0; j < resolutionY; j++)
            {
                signalMap[i * resolutionY + j].posX =  i * pointSeparation;
                signalMap[i * resolutionY + j].posY =  j * pointSeparation;
            }

        background.localScale = cam.ScreenToWorldPoint(new Vector2(Screen.width + screenOverlap[0] / 2, Screen.height + screenOverlap[1] / 2)) * 2;
        background.localPosition = cam.ScreenToWorldPoint(new Vector2((Screen.width + screenOverlap[0]) / 2, (Screen.height + screenOverlap[1]) / 2)) - cam.transform.position.z * Vector3.forward;
            
        texture = new RenderTexture(dimensions[0], dimensions[1], 24);
        texture.enableRandomWrite = true;
        texture.Create();
    }

    private void renderGraphics()
    {
        for (int i = 0; i < resolutionX ; i++)
            for (int j = 0; j < resolutionY; j++)
                signalMap[i * resolutionY + j].val = 0;

        ComputeBuffer valuePointBuffer = new ComputeBuffer(signalMap.Length, sizeof(float) + sizeof(int) * 2);
        valuePointBuffer.SetData(signalMap);

        calculateValues(valuePointBuffer);
        
        valuePointBuffer.GetData(signalMap);
        
        draw(valuePointBuffer, texture);

        bgMaterial.SetTexture("_MainTex", texture);

        valuePointBuffer.Dispose();
    }

    private void calculateValues(ComputeBuffer vals)
    {
        signalProcessor.SetBuffer(0, "valuePoints", vals);

        foreach (Metacircle circle in circles)
        {
            Vector2 pixelPos = cam.WorldToScreenPoint(circle.transform.position);
            signalProcessor.SetFloats("sourceCoords", new float[] { pixelPos.x, pixelPos.y });
            signalProcessor.SetFloat("sourceRadius", circle.radius);
            signalProcessor.Dispatch(0, vals.count / 64, 1, 1);
        }
    }

    private void draw(ComputeBuffer vals, RenderTexture tex)
    {
        textureRenderer.SetTexture(0, "Texture", tex);
        textureRenderer.SetBuffer(0, "valuePoints", vals);
        textureRenderer.SetTexture(1, "Texture", tex);
        textureRenderer.SetBuffer(1, "valuePoints", vals);
        textureRenderer.SetTexture(2, "Texture", tex);
        textureRenderer.SetInt("interpolate", interpolate ? 1 : 0);
        textureRenderer.SetInt("displayField", displayField ? 1 : 0);
        textureRenderer.SetInts("pixelResolution", new int[] { tex.width, tex.height });
        textureRenderer.SetInts("pointResolution", new int[] { resolutionX, resolutionY });
        textureRenderer.SetInt("pointSeparation", pointSeparation);
        textureRenderer.SetInt("lineThickness", lineThickness);

        textureRenderer.Dispatch(2, tex.width / 8, tex.height / 8, 1);
        textureRenderer.Dispatch(0, resolutionX / 8, resolutionY / 8, 1);
        if (displayField) textureRenderer.Dispatch(1, tex.width / 8, tex.height / 8, 1);
    }
}
