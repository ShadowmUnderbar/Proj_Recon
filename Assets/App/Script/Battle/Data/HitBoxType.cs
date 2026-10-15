using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Data
{

    public enum HitBoxType
    {
        None,
        Player,
        Enemy,
        Object,

        // 召喚した味方の被弾受け
        Ally
    }
}