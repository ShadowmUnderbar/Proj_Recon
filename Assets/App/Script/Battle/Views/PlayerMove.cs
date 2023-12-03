using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour, IPlayerMove
{
    public void Move(Vector2 move)
    {
        transform.position += new Vector3(move.x,0, move.y);
    }
}
