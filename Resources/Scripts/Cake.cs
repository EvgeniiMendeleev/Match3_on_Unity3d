using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cake : MonoBehaviour
{
    /*[SerializeField][Range(0,1)] private float speed;
    private Point targetPoint;
    private Vector3 firstCell;
    bool isMovable = false;

    void Start()
    {
        firstCell = GameObject.FindGameObjectWithTag("Board").transform.GetChild(0).position;
    }

    void FixedUpdate()
    {
        if(isMovable)
        {
            float posX = targetPoint.GetX * 0.438f + firstCell.x;
            float posY = targetPoint.GetY * 0.438f + firstCell.y;

            Vector3 position = new Vector3(posX, posY, -0.01f);

            if (transform.position == position)
            {
                isMovable = false;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, position, speed);
            }
        }
    }

    public Point SetTarget 
    { 
        set
        {
            value.SetY = -value.GetY;

            targetPoint = value;
            this.isMovable = true;
        } 
    }*/
}
