using UnityEngine;

namespace App.Battle.Data
{
    /// <summary>
    /// ボスウェーブの設定。指定ウェーブの開始時に、プレイヤーを決まった位置へ移し、ボスグループを出現させる。
    /// ボスウェーブ中は通常の敵が湧かず、ボスを全員倒すまでウェーブが進まない。
    /// </summary>
    [CreateAssetMenu(fileName = "BossWaveConfig", menuName = "Config/BossWaveConfig")]
    public class BossWaveConfig : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("ボスが出現するウェーブ番号（1始まり）")]
        private int _bossWave = 5;

        [SerializeField, Tooltip("出現させるボスグループ。未設定ならボスウェーブを行わない")]
        private BossGroupConfig _bossGroup;

        [SerializeField, Tooltip("ボスウェーブ開始時にプレイヤーを移す位置")]
        private Vector3 _playerPosition = Vector3.zero;

        [SerializeField, Tooltip("ボスグループの出現位置の基準点。メンバーはここからのずれで配置する")]
        private Vector3 _bossOriginPosition = new(0f, 0f, 20f);

        [SerializeField, Tooltip("ボスグループの基準の向き（オイラー角）。メンバーのずれもこの向きで回す")]
        private Vector3 _bossOriginEulerAngles = new(0f, 180f, 0f);

        public BossGroupConfig BossGroup => _bossGroup;
        public Vector3 PlayerPosition => _playerPosition;
        public Pose BossOrigin => new(_bossOriginPosition, Quaternion.Euler(_bossOriginEulerAngles));

        public bool IsBossWave(int wave)
        {
            return _bossGroup != null && wave == _bossWave;
        }
    }
}
