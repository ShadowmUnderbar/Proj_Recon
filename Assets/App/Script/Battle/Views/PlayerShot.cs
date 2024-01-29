using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShot : MonoBehaviour, IPlayerShot
{
    [SerializeField]
    private GameObject _bullet;


    public void ShotLeft(Vector3 pos)
    {
        var inst = Instantiate(_bullet);
        inst.transform.LookAt(pos);
    }

    public void ShotRight(Vector3 pos)
    {
        var inst = Instantiate(_bullet);
        inst.transform.LookAt(pos);
    }
}
