using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBoxView : MonoBehaviour , IHitBoxView
{
    [SerializeField]
    private HitBoxType hitBoxType;

    public HitBoxType HitBoxType => hitBoxType;
}
