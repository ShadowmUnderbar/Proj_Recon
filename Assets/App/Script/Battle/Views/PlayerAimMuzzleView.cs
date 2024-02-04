using App.Battle.Interface.View;
using UnityEngine;

namespace App.Battle.Views
{
    public class PlayerAimMuzzleView : MonoBehaviour, IPlayerAimMuzzleView
    {
        public void LookAimPosition(Vector3 position)
        {
            transform.LookAt(position);
        }
    }
}