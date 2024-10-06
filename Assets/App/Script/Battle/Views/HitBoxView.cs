using App.Battle.Data;
using App.Battle.Interface;
using UnityEngine;

public class HitBoxView : MonoBehaviour, IHitBoxView
{
    [SerializeField]
    private HitBoxType hitBoxType;

    [SerializeField]
    private int _id;

    public HitBoxType HitBoxType => hitBoxType;

    public int Id => _id;
}
