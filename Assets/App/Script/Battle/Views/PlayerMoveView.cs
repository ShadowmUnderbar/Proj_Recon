using System;
using App.Battle.Interface;
using R3;
using UnityEngine;

namespace App.Battle.Views
{
    public class PlayerMoveView : MonoBehaviour, IPlayerMoveView
    {
        public ReactiveProperty<Vector3> OnUpdatePosition { get; } = new();

        private void Update()
        {
            OnUpdatePosition.Value = transform.position;
        }

        public void Move(Vector2 move, float speed)
        {
            transform.position += new Vector3(move.x, 0, move.y) * speed;
        }
    }
}