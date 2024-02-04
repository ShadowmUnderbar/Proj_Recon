using System.Collections;
using UnityEngine;

namespace App.Battle.Interface.View
{
    public interface IPlayerTopDownAimStoreView
    {
        void Aim();
        Vector3 GetAimPosition(bool isLeft);
    }
}