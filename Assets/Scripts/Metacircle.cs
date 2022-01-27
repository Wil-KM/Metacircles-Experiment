using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metacircle : MonoBehaviour
{
    public Vector2 pos;
    public float strength;

    public float getStrength(Vector2 target)
    {
        return strength / (Mathf.Pow((target.x - pos.x), 2) + Mathf.Pow((target.y - pos.y), 2));
    }

    private void OnDrawGizmos() 
    {
        Gizmos.color = Color.red;

        Gizmos.DrawSphere(pos, .2f);
    }
}
