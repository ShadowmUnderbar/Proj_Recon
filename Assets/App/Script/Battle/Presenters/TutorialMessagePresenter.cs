using App.Battle.Data;
using App.Battle.Interface;
using VContainer;

namespace App.Battle.Presenters
{
    public class TutorialMessagePresenter : ITutorialMessagePresenter
    {
        private readonly ITutorialMessageView _tutorialMessageView;

        public TutorialMessagePhase Phase => _tutorialMessageView.Phase;

        [Inject]
        public TutorialMessagePresenter(ITutorialMessageView tutorialMessageView)
        {
            _tutorialMessageView = tutorialMessageView;
        }

        public void Show(string text) => _tutorialMessageView.Show(text);
        public void SetText(string text) => _tutorialMessageView.SetText(text);
        public void UpdateAnchor(TutorialMessageAnchor anchor) => _tutorialMessageView.UpdateAnchor(anchor);
        public void Hide() => _tutorialMessageView.Hide();
    }
}
