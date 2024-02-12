using System.Collections;
using UnityEngine;

namespace App.Battle.Interface.Views
{
    public interface IPlayerAimMuzzleView
    {
        Transform Transform { get; }
        void LookAimPosition(Vector3 position);
    }
}