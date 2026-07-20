using App.Battle.Interface;
using R3;
using VContainer;

namespace App.Battle.Presenters
{
    public class RunStartPresenter : IRunStartPresenter
    {
        private readonly IRunStartView _runStartView;

        public Observable<int> OnSlotSelected => _runStartView.OnSlotSelected;
        public Observable<Unit> OnStartWithoutLoad => _runStartView.OnStartWithoutLoad;

        [Inject]
        public RunStartPresenter(IRunStartView runStartView)
        {
            _runStartView = runStartView;
        }

        public void Show(string headline) => _runStartView.Show(headline);
        public void SetSlot(int index, string label, bool interactable) => _runStartView.SetSlot(index, label, interactable);
        public void Hide() => _runStartView.Hide();
    }
}
