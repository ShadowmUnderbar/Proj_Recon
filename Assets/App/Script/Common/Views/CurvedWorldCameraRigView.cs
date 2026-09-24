using App.Common.Data;
using UnityEngine;

namespace App.Common.Views
{
    /// <summary>
    /// カメラリグの高さと引きを設定から適用する。
    /// 水平線までの距離はカメラ高さに依存する（sqrt(カメラ高さ / 曲率)）ため、
    /// 高さと見える範囲は同じ設定アセットで並べて触れたほうが合わせやすい。
    /// VRではHMDの追従分がこのリグ位置に加算される。
    /// </summary>
    [ExecuteAlways]
    public class CurvedWorldCameraRigView : MonoBehaviour
    {
        [SerializeField, Tooltip("カメラ配置を持つ設定")]
        private CurvedWorldConfig _config;

        [SerializeField, Tooltip("位置を動かすカメラリグ。未指定ならこのオブジェクト自身")]
        private Transform _rig;

        private Transform RigTransform => _rig != null ? _rig : transform;

        private void OnEnable()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 高さを振りながらSceneビューで見え方を確かめられるようにする
            // スライダーを動かしている間はOnValidateが連続で飛ぶ。
            // そのたびに登録すると同じ処理が何十回も走ってエディタが固まる
            if (!isActiveAndEnabled || _applyQueued)
            {
                return;
            }

            _applyQueued = true;
            UnityEditor.EditorApplication.delayCall += ApplyIfAlive;
        }

        private bool _applyQueued;

        private void ApplyIfAlive()
        {
            _applyQueued = false;

            if (this != null)
            {
                Apply();
            }
        }
#endif

        /// <summary>設定のカメラ高さと引きをリグのローカル位置へ反映する</summary>
        public void Apply()
        {
            if (_config == null)
            {
                return;
            }

            RigTransform.localPosition = _config.CameraLocalPosition;
        }
    }
}
