using System;

namespace App.Common.Data
{
    [Serializable]
    public class BulletData
    {
        public ShotType Shotype;
        public AimFocusType FocusType = AimFocusType.NotFocus;
        public float CoolDownSecound = 1f;
        public int Damage = 1;
        public float Speed = 20f;
        public int Penetration = 1;
        public float Explosive = 0;
    }
}