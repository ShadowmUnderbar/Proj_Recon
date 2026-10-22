namespace App.Framework.Utilities
{
    /// <summary>
    /// <see cref="ViewPool{TView}"/> で使い回す View。
    /// 貸出のたびに <see cref="OnRent"/> で前回の状態を捨て、返却時に <see cref="OnReturn"/> で進行中の処理を止める。
    /// 自分の役目を終えたら <see cref="IViewReturner.Return"/> で自分をプールへ戻す（Destroy しない）
    /// </summary>
    public interface IPoolableView
    {
        /// <summary>プールから貸し出された直後に呼ばれる。状態の初期化と、返却先の保持を行う</summary>
        void OnRent(IViewReturner returner);

        /// <summary>プールへ戻る直前に呼ばれる。待機中の非同期処理の打ち切りや、見た目の後始末を行う</summary>
        void OnReturn();
    }

    /// <summary>貸し出した View を受け取ってプールへ戻す窓口</summary>
    public interface IViewReturner
    {
        void Return(IPoolableView view);
    }
}
