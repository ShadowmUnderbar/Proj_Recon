using App.Battle.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Interface
{
    public interface IHitBoxView
    {
        public HitBoxType HitBoxType { get; }
    }
}