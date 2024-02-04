using UnityEngine;

namespace App.Battle.Interface.View
{
    public interface IPlayerShot
    {
        public void ShotLeft(Vector3 pos);
        public void ShotRight(Vector3 pos);
    }
}