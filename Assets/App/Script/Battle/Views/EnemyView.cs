using App.Battle.Interface;
using UnityEngine;

namespace App.Script.Battle.Views
{
    public class EnemyView : MonoBehaviour, IEnemyView
    {
        public uint Id { get; private set; }

        public void Init(uint id)
        {
            Id = id;

            var hitboxes = GetComponentsInChildren<HitBoxView>();

            foreach (var hitbox in hitboxes)
            {
                hitbox.Id = Id;
            }
        }
    }
}