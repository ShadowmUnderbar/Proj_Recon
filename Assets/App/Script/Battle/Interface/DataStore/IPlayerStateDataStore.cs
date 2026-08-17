using App.Common.Data;
using R3;
using UnityEngine;

namespace App.Battle.Interface.DataStore
{
    public interface IPlayerStateDataStore
    {
        ReactiveProperty<Vector3> Position { get; }
        ReactiveProperty<Quaternion> Rotate { get; }
        Pose Pose { get; }
        Transform PlayerTransform { get; set; }

        ReactiveProperty<float> Health { get; }
        ReactiveProperty<float> MaxHealth { get; }

        /// <summary>被弾したダメージ量を流す（HP減少と同時。被弾条件バフの駆動に使う）</summary>
        Observable<float> OnDamaged { get; }

        float MoveSpeed { get; }
        UnlockCoreSkillType UnlockCoreSkillType { get; }

        void Move(Vector2 moveV2, float speed);

        /// <summary>プレイヤーにダメージを与える（HPを減らし OnDamaged を発火）</summary>
        void TakeDamage(float damage);

        /// <summary>
        /// 所持中のHP最大値アップを反映して MaxHealth を再計算する。
        /// 増えた分は現在HPにも加算する。アップグレード獲得時・セット読込時に呼ぶ。
        /// </summary>
        void RefreshMaxHealth();

        /// <summary>プレイヤーのHPを回復する（MaxHealth を上限にクランプ）</summary>
        void Heal(float amount);

        void SetUnlockCoreSkillType(UnlockCoreSkillType unlockCoreSkillType);
    }
}