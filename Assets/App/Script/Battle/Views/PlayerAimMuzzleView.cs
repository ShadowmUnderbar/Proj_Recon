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

            if (position.y <= transform.position.y)
            {
                position.y = transform.position.y;
            }

            transform.LookAt(position);
        }

        public void SetRayColor(Color color)
        {
            _forwardRayView.SetRayColor(color);
        }

        public void SetEnableRay(bool enable)
        {
            _forwardRayView.SetEnable(enable);
        }
    }
}