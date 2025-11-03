using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerTopDownAimView
    {
        Vector3 GetAimPosition();
        Observable<int> OnFocus { get; }
        bool IsFocus { get; set; }
    }
}