using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float Speed = 50;

    public float minPosX = -4.3f;
    public float maxPosX = 8.4f;

    public float minPosY = -7.5f;
    public float maxPosY = 8.5f;
 
     void Update()
     {
        float xAxisValue = Input.GetAxis("Horizontal") * Speed;
        float yAxisValue = Input.GetAxis("Vertical") * Speed;
        var xPos = Mathf.Clamp(transform.position.x + xAxisValue, minPosX, maxPosX);        
        var yPos = Mathf.Clamp(transform.position.y + yAxisValue, minPosY, maxPosY);


 
        transform.position = new Vector3(xPos, yPos, transform.position.z);
     }
}
