using UnityEngine;

namespace App.Framework
{
    /// <summary>
    /// このコンポーネントを持つオブジェクトをデプス基準点として設定する。
    /// Custom/DepthClipLit シェーダーを使用するオブジェクトは、
    /// このオブジェクトよりカメラに近い位置に描画されると非表示になる。
    ///
    /// 使い方:
    ///   1. 基準オブジェクトにこのコンポーネントをアタッチ
    ///   2. 非表示にしたいオブジェクトのシェーダーを Custom/DepthClipLit に変更
    /// </summary>
    [DisallowMultipleComponent]
    public class DepthClipperController : MonoBehaviour
    {
        private static readonly int ClipperDepthId = Shader.PropertyToID("_ClipperDepth");

        [SerializeField, Tooltip("使用するカメラ。未設定時は Camera.main を使用")]
        private Camera _camera;

        private void Start()
        {
            if (_camera == null)
                _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_camera == null) return;

            // カメラからのリニアアイ深度を計算してグローバルに設定
            float depth = _camera.WorldToViewportPoint(transform.position).z;
            Shader.SetGlobalFloat(ClipperDepthId, depth);
        }

        private void OnDisable()
        {
            // 無効化時はクリッピングを解除（0 = 無効）
            Shader.SetGlobalFloat(ClipperDepthId, 0f);
        }

        private void OnDestroy()
        {
            Shader.SetGlobalFloat(ClipperDepthId, 0f);
        }
    }
}
