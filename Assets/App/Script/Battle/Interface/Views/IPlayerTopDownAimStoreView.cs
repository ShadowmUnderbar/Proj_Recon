using System.Collections;
using UnityEngine;

namespace App.Battle.Interface.Views
{
    public interface IPlayerTopDownAimStoreView
    {
        void Aim();
        Vector3 GetAimPosition(bool isLeft);
        void Shot(bool isLeft);
    }
}