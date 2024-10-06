using App.Battle.Interface;
using UnityEngine;

namespace App.Battle.Views
{
    public class PlayerAimMuzzleView : MonoBehaviour, IPlayerAimMuzzleView
    {
        public Transform Transform => transform;

        public void LookAimPosition(Vector3 position)
        {
            if(position == default)
            {
                return;
            }

            position.y = transform.position.y;

            transform.LookAt(position);
        }
    }
}