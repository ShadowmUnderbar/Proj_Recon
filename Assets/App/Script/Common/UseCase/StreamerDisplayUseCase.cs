using App.Common.Data;
using UnityEngine;
using UnityEngine.XR;
using VContainer;
using VContainer.Unity;

namespace App.Common.UseCase
{
    /// <summary>
    /// ストリーマーモード時のPCディスプレイ出力を切り替える。
    /// XRのミラー表示（HMD映像のPC画面転送）を止め、配信用カメラの描画をPC画面へ出すための下準備を行う。
    /// カメラ本体の生成・制御は行わない（Battle層のStreamerCameraViewが担当）。
    /// </summary>
    public class StreamerDisplayUseCase : IInitializable
    {
        private readonly StreamerModeConfig _streamerModeConfig;

        [Inject]
        public StreamerDisplayUseCase(StreamerModeConfig streamerModeConfig)
        {
            _streamerModeConfig = streamerModeConfig;
        }

        public void Initialize()
        {
            if (!_streamerModeConfig.IsEnabled)
            {
                return;
            }

            if (_streamerModeConfig.VROnly && !DebugConfig.IsVRMode)
            {
                return;
            }

            if (!_streamerModeConfig.SuppressMirrorView)
            {
                return;
            }

            // ミラー表示を止めて、PCメインウィンドウを配信用カメラの描画先として空ける。
            // XRプラグイン（Oculus / OpenXR）によって効き方が異なるため、実機で確認が必要
            XRSettings.showDeviceView = false;
            Debug.Log($"[StreamerMode] ミラー表示を抑制しました (showDeviceView={XRSettings.showDeviceView})");
        }
    }
}
