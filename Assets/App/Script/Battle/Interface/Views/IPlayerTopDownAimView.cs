using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerTopDownAimView
    {
        Vector3 GetAimPosition();
        ReactiveProperty<bool> IsFocus { get; }
    }
}