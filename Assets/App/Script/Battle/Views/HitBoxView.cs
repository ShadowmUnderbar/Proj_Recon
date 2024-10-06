using App.Battle.Data;
using App.Battle.Interface;
using UnityEngine;

public class HitBoxView : MonoBehaviour, IHitBoxView
{
    [SerializeField]
    private HitBoxType hitBoxType;

    public HitBoxType HitBoxType => hitBoxType;

    public uint Id { get; set; }
}
