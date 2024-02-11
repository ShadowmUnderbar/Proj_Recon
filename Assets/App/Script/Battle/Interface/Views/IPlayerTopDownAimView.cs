using UnityEngine;

namespace App.Battle.Interface.Views
{
    public interface IPlayerTopDownAimView
    {
        Vector3 GetAimPosition();
        bool IsFocus();
    }
}