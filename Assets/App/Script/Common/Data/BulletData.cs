using System;

namespace App.Common.Data
{
    [Serializable]
    public class BulletData
    {
        public string Id;
        public ShotType Type;
        public int Damage = 1;
        public float Speed = 20f;
        public int Penetration = 1;
        public float Explosive = 0;
        public bool IsFocus = false;
    }
}