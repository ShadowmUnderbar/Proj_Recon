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
        public float Explosive = 0;
    }
}