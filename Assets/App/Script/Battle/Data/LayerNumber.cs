using UnityEngine;

namespace App.Battle.Data
{
    public static class LayerMasks
    {
        public static int FieldLayer => 1 << LayerMask.NameToLayer("Default");
        public static int EnemyLayer => 1 << LayerMask.NameToLayer("Enemy");
    }
}