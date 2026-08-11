using System;

namespace App.Common.Data
{
    [Serializable]
    public class BulletData
    {
        public ShotType ShotType;
        public AimFocusType FocusType = AimFocusType.NotFocus;
        public float Damage = 1;
        public float Speed = 20f;
        public int Penetration = 1;
        public float Size = 0.2f;
        // 爆風の半径。0なら爆風なし
        public float Explosive = 0;

        // 爆風の範囲内に与えるダメージ。0なら爆風によるダメージは発生しない
        public float ExplosiveDamage = 0;
    }
}