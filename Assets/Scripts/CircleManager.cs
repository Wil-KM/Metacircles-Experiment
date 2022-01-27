using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleManager : MonoBehaviour
{
    public Camera cam;
    private Vector2 worldDimensions;
    // Resolution of blocks used in marching cubes, higher res = better picture
    public int resolutionY;
    private int prevResY;
    private int resolutionX;
    // Size of blocks used in marching cubes, calculated with resolution
    private float blockSize;
    // All metacircles in the simulation
    private Metacircle[]  circles;
    // Stores the summation of all circle signal values for marching cubes
    private ValuePoint[] signalMap;
    private RenderTexture signalTexture;
    public ComputeShader signalProcessor;
    public Transform background;
    public Material bgMaterial;
    public ComputeShader signalRenderer;

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
        Debug.Log("Recalculating Grid");
        prevResY = resolutionY;
        
        resolutionX = (Screen.width * resolutionY) / Screen.height;
        blockSize = (worldDimensions.y * 2) / resolutionY;
        worldDimensions.x = (resolutionX * blockSize) / 2;

        background.localScale = new Vector3(worldDimensions.x * 2, worldDimensions.y * 2, 1);

        signalMap = new ValuePoint[(resolutionX + 1) * (resolutionY + 1)];

        for (int i = 0; i < resolutionX + 1; i++)
            for (int j = 0; j < resolutionY + 1; j++)
                signalMap[i * (resolutionY + 1) + j].pos = new Vector2(i * blockSize - worldDimensions.x, j * blockSize - worldDimensions.y);
    }

    private void runSimulation()
    {
        for (int i = 0; i < resolutionX + 1; i++)
            for (int j = 0; j < resolutionY + 1; j++)
                signalMap[i * (resolutionY + 1) + j].val = 0;

        ComputeBuffer valuePointBuffer = new ComputeBuffer(signalMap.Length, sizeof(float) * 3);
        valuePointBuffer.SetData(signalMap);

        calculateValues(valuePointBuffer);
        
        valuePointBuffer.GetData(signalMap);

        drawField(valuePointBuffer);

        valuePointBuffer.Dispose();
    }

    private void calculateValues(ComputeBuffer vals)
    {
        signalProcessor.SetBuffer(0, "valuePoints", vals);

        foreach (Metacircle circle in circles)
        {
            signalProcessor.SetFloats("sourceCoords", new float[] { circle.pos.x, circle.pos.y });
            signalProcessor.SetFloat("sourceStrength", circle.strength);
            signalProcessor.Dispatch(0, signalMap.Length / 8, 1, 1);
        }
    }

    private void drawField(ComputeBuffer vals)
    {
        signalRenderer.SetTexture(0, "Result", signalTexture);
        signalRenderer.SetBuffer(0, "valuePoints", vals);
        signalRenderer.SetInts("pixelResolution", new int[] { signalTexture.width, signalTexture.height });
        signalRenderer.SetInts("blockResolution", new int[] { resolutionX, resolutionY });
        
        signalRenderer.Dispatch(0, signalTexture.width / 8, signalTexture.height / 8, 1);

        bgMaterial.SetTexture("_MainTex", signalTexture);
    }
}
