using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleManager : MonoBehaviour
{
    public Camera cam;
    private Vector2 worldDimensions;
    // Resolution of blocks used in marching cubes, higher res = better picture
    public int resolutionY;
    private int resolutionX;
    // Size of blocks used in marching cubes, calculated with resolution
    private float blockSize;
    private float halfBlock;
    // All metacircles in the simulation
    private Metacircle[]  circles;
    // Stores the summation of all circle signal values for marching cubes
    private float[,] signalMap;
    
    void Start()
    {
        circles = GetComponentsInChildren<Metacircle>();
        worldDimensions = cam.ScreenToWorldPoint(new Vector2(0, cam.pixelHeight));
    }

    private void OnDrawGizmos() 
    {
        Gizmos.color = Color.white;

        for (int i = 0; i < resolutionX + 1; i++)
        {
            Gizmos.DrawLine(new Vector2(i * blockSize - worldDimensions.x, -worldDimensions.y), new Vector2(i * blockSize - worldDimensions.x, worldDimensions.y));

            for (int j = 0; j < resolutionY + 1; j++)
                Gizmos.DrawLine(new Vector2(-worldDimensions.x, j * blockSize - worldDimensions.y), new Vector2(worldDimensions.x, j * blockSize - worldDimensions.y));
        }

        for (int i = 0; i < resolutionX; i++)
            for (int j = 0; j < resolutionY; j++)
            {
                Gizmos.color = new Color(1f - (1f / Mathf.Max(1, signalMap[i, j])), 1f - (1f / Mathf.Max(1, signalMap[i, j])), 1f - (1f / Mathf.Max(1, signalMap[i, j])), .5f);
                Gizmos.DrawCube(new Vector2(i * blockSize - worldDimensions.x + halfBlock, j * blockSize - worldDimensions.y + halfBlock), Vector2.one * blockSize);
            }
    }

    void Update()
    {
        resolutionX = (Screen.width * resolutionY) / Screen.height;
        blockSize = (worldDimensions.y * 2) / resolutionY;
        halfBlock = blockSize / 2;
        worldDimensions.x = (resolutionX * blockSize) / 2;
        
        signalMap = new float[resolutionX + 1, resolutionY + 1];

        for (int i = 0; i < resolutionX + 1; i++)
            for (int j = 0; j < resolutionY + 1; j++)
                foreach (Metacircle circle in circles)
                    signalMap[i, j] += circle.getStrength(new Vector2(i * blockSize - worldDimensions.x, j * blockSize - worldDimensions.y));
    }
}
