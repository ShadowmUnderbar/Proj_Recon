using App.Battle.Interface;
using R3;
using VContainer;

namespace App.Battle.Presenters
{
    public class GameOverPresenter : IGameOverPresenter
    {
        private readonly IGameOverView _gameOverView;

        public Observable<int> OnSaveSlotSelected => _gameOverView.OnSaveSlotSelected;
        public Observable<Unit> OnExitWithoutSave => _gameOverView.OnExitWithoutSave;

        [Inject]
        public GameOverPresenter(IGameOverView gameOverView)
        {
            _gameOverView = gameOverView;
        }

        public void Show(string headline) => _gameOverView.Show(headline);
        public void SetSlotLabel(int index, string label) => _gameOverView.SetSlotLabel(index, label);
        public void SetStatus(string status) => _gameOverView.SetStatus(status);
        public void Hide() => _gameOverView.Hide();
    }
}
