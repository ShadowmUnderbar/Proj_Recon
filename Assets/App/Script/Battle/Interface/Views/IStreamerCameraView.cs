using App.Battle.Data;
using R3;

namespace App.Battle.Interface
{
    /// <summary>
    /// 配信用（ディスプレイ側）カメラ。HMDへ出る映像には一切干渉しない。
    /// 通常はプレイヤーカメラの視点を複製し、演出中だけ独自の位置・画角へ移動して復帰する。
    /// </summary>
    public interface IStreamerCameraView
    {
        /// <summary>演出の再生が完了し、プレイヤー視点へ戻り切ったタイミング</summary>
        Observable<Unit> OnShotFinished { get; }

        /// <summary>演出ショットを再生する。再生中に呼ぶと現在のショットを差し替える</summary>
        void PlayShot(StreamerCameraShotRequest request);

        /// <summary>演出を中断し、プレイヤー視点へ戻す</summary>
        void CancelShot();
    }
}
