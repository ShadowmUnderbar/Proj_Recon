using R3;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IPlayerTopDownAimView
    {
        Vector3 GetAimPosition();
        Observable<Unit> OnFocus { get; }
        Observable<Unit> OnUnFocus { get; }
    }
}