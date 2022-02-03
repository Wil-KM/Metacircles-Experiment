using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleManager : MonoBehaviour
{
    public Camera cam;
    public ComputeShader signalProcessor;
    public Transform background;
    public Material bgMaterial;
    public ComputeShader signalRenderer;
    // Resolution of blocks used in marching cubes, higher res = better picture
    [Range(5, 200)]
    public int resolutionY;
    private int prevResY;
    private int resolutionX;
    private int pointSeparation;
    // All metacircles in the simulation
    private Metacircle[]  circles;
    // Stores the summation of all circle signal values for marching cubes
    private ValuePoint[] signalMap;
    private RenderTexture signalTexture;
    public float lineThickness;
    public bool displayField;

    public struct ValuePoint
    {
        public int posX;
        public int posY;
        public float val;
    }
    
    void Start()
    {
        circles = GetComponentsInChildren<Metacircle>();
    }

    void Update()
    {

        if (prevResY != resolutionY)
            calculateGrid();

        runSimulation();
    }

    private void OnDrawGizmos() 
    {
        for (int i = 0; i < resolutionX; i++)
        {
            for (int j = 0; j < resolutionY; j++)
            {
                Gizmos.color = (signalMap[i * resolutionY + j].val >= 0.001 ? Color.green : Color.red);

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
            
        signalTexture = new RenderTexture(dimensions[0], dimensions[1], 24);
        signalTexture.enableRandomWrite = true;
        signalTexture.Create();
    }

    private void runSimulation()
    {
        for (int i = 0; i < resolutionX ; i++)
            for (int j = 0; j < resolutionY; j++)
                signalMap[i * resolutionY + j].val = 0;

        ComputeBuffer valuePointBuffer = new ComputeBuffer(signalMap.Length, sizeof(float) + sizeof(int) * 2);
        valuePointBuffer.SetData(signalMap);

        calculateValues(valuePointBuffer);
        
        valuePointBuffer.GetData(signalMap);

        drawField(valuePointBuffer, signalTexture);

        bgMaterial.SetTexture("_MainTex", signalTexture);

        valuePointBuffer.Dispose();
    }

    private void calculateValues(ComputeBuffer vals)
    {
        signalProcessor.SetBuffer(0, "valuePoints", vals);

        foreach (Metacircle circle in circles)
        {
            Vector2 pixelPos = cam.WorldToScreenPoint(circle.transform.position);
            signalProcessor.SetFloats("sourceCoords", new float[] { pixelPos.x, pixelPos.y });
            signalProcessor.SetFloat("sourceStrength", circle.strength);
            signalProcessor.Dispatch(0, vals.count / 8, 1, 1);
        }
    }

    private void drawField(ComputeBuffer vals, RenderTexture tex)
    {
        signalRenderer.SetTexture(0, "Texture", tex);
        signalRenderer.SetBuffer(0, "valuePoints", vals);
        signalRenderer.SetInts("pixelResolution", new int[] { tex.width, tex.height });
        signalRenderer.SetInts("pointResolution", new int[] { resolutionX, resolutionY });
        signalRenderer.SetInt("pointSeparation", pointSeparation);
        signalRenderer.SetFloat("lineThickness", lineThickness);
        signalRenderer.SetInt("displayField", displayField ? 1 : 0);
        
        signalRenderer.Dispatch(0, tex.width / 8, tex.height / 8, 1);

        bgMaterial.SetTexture("_MainTex", tex);
    }
}
