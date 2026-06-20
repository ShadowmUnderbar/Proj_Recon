using App.Battle.Interface;
using UnityEngine;

namespace App.Battle.Views
{
    // 両腕を手続き的にエイム対象へ向けるView。
    // 2つのエイム対象を「画面上で肩に近い方の腕」へ動的に割り当てる（左入力→左腕の固定ではない）。
    // ※ OnAnimatorIKはAnimatorと同じGameObjectでのみ発火するため、このコンポーネントはモデルへ付与する。
    public class PlayerAimIKView : MonoBehaviour, IPlayerAimIKView
    {
        [SerializeField] private Animator _animator;

        // エイム時の手IKの目標ウェイト（0で無効、1で完全追従）
        [SerializeField, Range(0f, 1f)] private float _aimWeight = 1f;

        // ウェイトの追従速度（秒あたり）。急なON/OFFを避けて滑らかに切り替える
        [SerializeField] private float _weightLerpSpeed = 8f;

        // 肩からの最大到達距離。遠い対象でも腕が破綻しないようクランプする
        [SerializeField] private float _armReach = 0.6f;

        // 手の回転も対象方向へ合わせるか
        [SerializeField] private bool _alignHandRotation = true;

        private Camera _camera;
        private Transform _leftUpperArm;
        private Transform _rightUpperArm;
        private bool _hasBones;

        private Vector3 _leftTarget;
        private Vector3 _rightTarget;
        private bool _hasTargets;

        // 現在のIKウェイト（手ごとに補間）
        private float _leftWeight;
        private float _rightWeight;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            CacheBones();
        }

        private void CacheBones()
        {
            if (_animator == null)
            {
                return;
            }

            _leftUpperArm = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _rightUpperArm = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _hasBones = _leftUpperArm != null && _rightUpperArm != null;
        }

        public void SetAimTargets(Vector3 leftTarget, Vector3 rightTarget)
        {
            _leftTarget = leftTarget;
            _rightTarget = rightTarget;
            _hasTargets = true;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || !_hasTargets)
            {
                return;
            }

            if (!_hasBones)
            {
                CacheBones();
                if (!_hasBones)
                {
                    return;
                }
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            // 画面上で近い腕へ各対象を割り当てる
            ResolveAssignment(out var leftArmTarget, out var rightArmTarget);

            // 各手をエイム対象へ。肩から遠い場合は到達距離でクランプ
            ApplyHandIK(AvatarIKGoal.LeftHand, _leftUpperArm, leftArmTarget, ref _leftWeight);
            ApplyHandIK(AvatarIKGoal.RightHand, _rightUpperArm, rightArmTarget, ref _rightWeight);
        }

        // 2×2の割当のうちスクリーン距離合計が小さい方を採用する
        private void ResolveAssignment(out Vector3 leftArmTarget, out Vector3 rightArmTarget)
        {
            // カメラが無い場合はスクリーン判定不可。左腕→左対象の素直な割当にフォールバック
            if (_camera == null)
            {
                leftArmTarget = _leftTarget;
                rightArmTarget = _rightTarget;
                return;
            }

            var leftShoulder = ToScreen(_leftUpperArm.position);
            var rightShoulder = ToScreen(_rightUpperArm.position);
            var targetA = ToScreen(_leftTarget);
            var targetB = ToScreen(_rightTarget);

            // (左腕→A, 右腕→B)
            var straight = Vector2.Distance(leftShoulder, targetA) + Vector2.Distance(rightShoulder, targetB);
            // (左腕→B, 右腕→A)
            var swapped = Vector2.Distance(leftShoulder, targetB) + Vector2.Distance(rightShoulder, targetA);

            if (swapped < straight)
            {
                leftArmTarget = _rightTarget;
                rightArmTarget = _leftTarget;
            }
            else
            {
                leftArmTarget = _leftTarget;
                rightArmTarget = _rightTarget;
            }
        }

        private Vector2 ToScreen(Vector3 worldPosition)
        {
            var screen = _camera.WorldToScreenPoint(worldPosition);
            return new Vector2(screen.x, screen.y);
        }

        private void ApplyHandIK(AvatarIKGoal goal, Transform upperArm, Vector3 target, ref float weight)
        {
            weight = Mathf.MoveTowards(weight, _aimWeight, _weightLerpSpeed * Time.deltaTime);

            var shoulder = upperArm.position;
            var toTarget = target - shoulder;
            var distance = toTarget.magnitude;

            // 肩から到達距離内へクランプした到達点
            var reachPoint = distance > _armReach
                ? shoulder + toTarget / distance * _armReach
                : target;

            _animator.SetIKPositionWeight(goal, weight);
            _animator.SetIKPosition(goal, reachPoint);

            if (_alignHandRotation && distance > Mathf.Epsilon)
            {
                _animator.SetIKRotationWeight(goal, weight);
                _animator.SetIKRotation(goal, Quaternion.LookRotation(toTarget / distance, Vector3.up));
            }
        }
    }
}
