using System.Collections.Generic;
using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// ポイント粒子の単位定義と漂い・吸い寄せ・回収の調整パラメータ。
    /// </summary>
    [CreateAssetMenu(fileName = "PointParticleConfig", menuName = "Config/PointParticleConfig")]
    public class PointParticleConfig : ScriptableObject
    {
        [Header("単位")]
        [SerializeField, Tooltip("粒子の単位一覧。順不同で登録してよい（分割時に値の大きい順へ並べ替える）")]
        private List<PointUnitData> _units = new();

        [SerializeField, Tooltip("1回の撃破で出す粒子の最大個数。超える場合は小さい単位から繰り上げて丸める")]
        private int _maxParticleCount = 12;

        [Header("生成")]
        [SerializeField, Tooltip("撃破地点を中心に粒子を散らす半径（m）")]
        private float _spawnRadius = 0.6f;

        [SerializeField, Tooltip("生成時の高さの下限（m。撃破地点の足元から測る）。地面へ埋まらないよう持ち上げる")]
        private float _minHeight = 0.5f;

        [SerializeField, Tooltip("生成時の高さの上限（m。撃破地点の足元から測る）")]
        private float _maxHeight = 1.5f;

        [Header("漂い")]
        [SerializeField, Tooltip("上下に揺れる振幅（m）")]
        private float _bobAmplitude = 0.15f;

        [SerializeField, Tooltip("上下に揺れる速さ（1秒あたりの周期数）")]
        private float _bobFrequency = 0.5f;

        [SerializeField, Tooltip("水平方向にゆっくり漂う速さ（m/秒）")]
        private float _driftSpeed = 0.1f;

        [SerializeField, Tooltip("生成後にプレイヤーと同じ高さへ寄っていく速さ（m/秒）。0なら生成時の高さのまま漂う")]
        private float _heightFollowSpeed = 1.5f;

        [Header("吸い寄せ・回収")]
        [SerializeField, Tooltip("この距離まで近づくとプレイヤーへ吸い寄せられる（m）")]
        private float _magnetDistance = 3f;

        [SerializeField, Tooltip("吸い寄せの最高速度（m/秒）")]
        private float _magnetSpeed = 6f;

        [SerializeField, Tooltip("吸い寄せの加速度（m/秒^2）。近づくほど速くなる見た目にする")]
        private float _magnetAcceleration = 12f;

        [SerializeField, Tooltip("この距離まで近づくと回収される（m）。高さは無視し、水平距離で判定する")]
        private float _collectDistance = 0.4f;

        [SerializeField, Tooltip("取得判定の最小の大きさ（直径m）。見た目が小さい単位の粒子でも取りこぼさないよう、弾の通過判定とプレイヤーの接触判定をこの大きさまで広げる")]
        private float _minHitSize = 0.5f;

        [SerializeField, Tooltip("弾が当たった粒子がプレイヤーへ吸い込まれ切るまでの秒数。どれだけ遠くで撃ってもこの秒数で回収される")]
        private float _bulletPullDuration = 0.5f;

        [SerializeField, Tooltip("回収・吸い寄せの基準にするプレイヤーの高さ（m）。プレイヤー座標は足元基準のため胴体あたりを狙う")]
        private float _playerCenterHeight = 1f;

        public IReadOnlyList<PointUnitData> Units => _units;
        public int MaxParticleCount => _maxParticleCount;
        public float SpawnRadius => _spawnRadius;
        public float MinHeight => _minHeight;
        public float MaxHeight => _maxHeight;
        public float BobAmplitude => _bobAmplitude;
        public float BobFrequency => _bobFrequency;
        public float DriftSpeed => _driftSpeed;
        public float HeightFollowSpeed => _heightFollowSpeed;
        public float MagnetDistance => _magnetDistance;
        public float MagnetSpeed => _magnetSpeed;
        public float MagnetAcceleration => _magnetAcceleration;
        public float CollectDistance => _collectDistance;
        public float MinHitSize => _minHitSize;
        public float BulletPullDuration => _bulletPullDuration;
        public float PlayerCenterHeight => _playerCenterHeight;
    }
}
