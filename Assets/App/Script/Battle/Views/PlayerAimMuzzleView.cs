using App.Battle.Interface;
using App.Common.Views;
using UnityEngine;

namespace App.Battle.Views
{
    public class PlayerAimMuzzleView : MonoBehaviour, IPlayerAimMuzzleView
    {
        [SerializeField] private ForwardRayView _forwardRayView;
        public Transform Transform => transform;

        public void LookAimPosition(Vector3 position)
        {
            if (position == default)
            {
                return;
            }

            position.y = transform.position.y;

            transform.LookAt(position);
        }

        public void SetRayColor(Color color)
        {
            Debug.Log("SetRayColor" + color);
            _forwardRayView.SetRayColor(color);
        }
    }
}