using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleManager : MonoBehaviour
{
    public Camera cam;
    private Vector2 worldDimensions;
    // Resolution of blocks used in marching cubes, higher res = better picture
    [Range(5, 200)]
    public int resolutionY;
    private int prevResY;
    private int resolutionX;
    // Size of blocks used in marching cubes, calculated with resolution
    private float pointSeparation;
    // All metacircles in the simulation
    private Metacircle[]  circles;
    // Stores the summation of all circle signal values for marching cubes
    private ValuePoint[] signalMap;
    private RenderTexture signalTexture;
    public ComputeShader signalProcessor;
    public Transform background;
    public Material bgMaterial;
    public ComputeShader signalRenderer;

    public bool displayField;

    public struct ValuePoint
    {
        public Vector2 pos;
        public float val;
    }
    
    void Start()
    {
        circles = GetComponentsInChildren<Metacircle>();
        worldDimensions = cam.ScreenToWorldPoint(new Vector2(0, cam.pixelHeight));
            
        signalTexture = new RenderTexture(Screen.width, Screen.height, 24);
        signalTexture.enableRandomWrite = true;
        signalTexture.Create();
    }

    void Update()
    {

        if (prevResY != resolutionY)
            calculateGrid();

        runSimulation();
    }

    private void calculateGrid()
    {
        prevResY = resolutionY;
        
        resolutionX = (Screen.width * (resolutionY - 1)) / Screen.height + 1;
        pointSeparation = (worldDimensions.y * 2) / (resolutionY - 1);
        worldDimensions.x = ((resolutionX - 1) * pointSeparation) / 2;

        signalMap = new ValuePoint[resolutionX * resolutionY];

        for (int i = 0; i < resolutionX; i++)
            for (int j = 0; j < resolutionY; j++)
                signalMap[i * resolutionY + j].pos = new Vector2(i * pointSeparation - worldDimensions.x, j * pointSeparation - worldDimensions.y);

        background.localScale = new Vector3(worldDimensions.x * 2, worldDimensions.y * 2, 1);
    }

    private void runSimulation()
    {
        for (int i = 0; i < resolutionX ; i++)
            for (int j = 0; j < resolutionY; j++)
                signalMap[i * resolutionY + j].val = 0;

        ComputeBuffer valuePointBuffer = new ComputeBuffer(signalMap.Length, sizeof(float) * 3);
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
            signalProcessor.SetFloats("sourceCoords", new float[] { circle.transform.position.x, circle.transform.position.y });
            signalProcessor.SetFloat("sourceStrength", circle.strength);
            signalProcessor.Dispatch(0, vals.count / 8, 1, 1);
        }
    }

    private void drawField(ComputeBuffer vals, RenderTexture tex)
    {
        signalRenderer.SetTexture(0, "SignalMap", tex);
        signalRenderer.SetBuffer(0, "valuePoints", vals);
        signalRenderer.SetInts("pixelResolution", new int[] { tex.width, tex.height });
        signalRenderer.SetInts("pointResolution", new int[] { resolutionX, resolutionY });
        signalRenderer.SetInt("displayField", displayField ? 1 : 0);
        
        signalRenderer.Dispatch(0, tex.width / 8, tex.height / 8, 1);

        bgMaterial.SetTexture("_MainTex", tex);
    }
}
