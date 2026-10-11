using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace App.Battle.Views
{
    /// <summary>
    /// アップグレードのタグを1行で並べ、表示枠（RectTransform の幅）に収まらないときは時間で横に流す。
    /// 3D の TextMeshPro はマスクが効かないため、文字の頂点をずらし、枠の端に近い文字ほど透明にして見切れを隠す
    /// </summary>
    public class UpgradeTagTickerView : MonoBehaviour
    {
        [SerializeField, Tooltip("タグを表示するテキスト。左寄せ・折り返しなしで使う（幅が表示枠になる）")]
        private TextMeshPro _text;

        [SerializeField, Tooltip("タグ同士の区切り")]
        private string _separator = "  /  ";

        [SerializeField, Tooltip("流れる速さ[テキストのローカル単位/秒]")]
        private float _scrollSpeed = 2f;

        [SerializeField, Tooltip("先頭に戻ったときに止まる時間[秒]")]
        private float _loopPause = 1f;

        [SerializeField, Tooltip("表示枠の端で文字を薄くする幅[テキストのローカル単位]")]
        private float _edgeFadeWidth = 0.8f;

        private TagTickerScroller _scroller;

        // 流す前の頂点・色（メッシュの作り直しのたびに取り直す）
        private Vector3[][] _sourceVertices = Array.Empty<Vector3[]>();
        private Color32[][] _sourceColors = Array.Empty<Color32[]>();

        private bool _isScrolling;

        private void Awake()
        {
            _scroller = new TagTickerScroller(_scrollSpeed, _loopPause);

            if (_text != null)
            {
                // 流すときは1行で左端から並べる前提で幅を測るため、prefab の設定に関わらず固定する
                _text.enableWordWrapping = false;
                _text.overflowMode = TextOverflowModes.Overflow;
                _text.alignment = TextAlignmentOptions.MidlineLeft;
            }
        }

        private void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }

        /// <summary>表示するタグ名を設定する。空なら何も出さない</summary>
        public void SetTags(IReadOnlyList<string> tagNames)
        {
            if (_text == null)
            {
                return;
            }

            _isScrolling = false;

            if (tagNames == null || tagNames.Count == 0)
            {
                _text.text = string.Empty;
                return;
            }

            var joined = string.Join(_separator, tagNames);
            if (_text.GetPreferredValues(joined).x <= _text.rectTransform.rect.width)
            {
                // 収まるなら流さずそのまま出す
                _text.text = joined;
                return;
            }

            // 1周ぶん（末尾にも区切りを付けて次の周との間を空ける）を2回並べ、1周の幅だけずらし続ける
            var cycle = joined + _separator;
            _text.text = cycle + cycle;
            _text.ForceMeshUpdate();

            var textInfo = _text.textInfo;
            if (textInfo.characterCount <= cycle.Length)
            {
                // 文字数が想定と合わない（リッチテキストのタグを含む等）ときは流さず1周ぶんだけ出す
                _text.text = joined;
                return;
            }

            _scroller.Reset(textInfo.characterInfo[cycle.Length].origin - textInfo.characterInfo[0].origin);
            _isScrolling = true;
            CacheSourceMesh();
            ApplyScroll();
        }

        private void Update()
        {
            if (!_isScrolling)
            {
                return;
            }

            // ショップなどポーズ中にも流すため、時間停止の影響を受けない時間で進める
            _scroller.Advance(Time.unscaledDeltaTime);
            ApplyScroll();
        }

        private void OnTextChanged(UnityEngine.Object changed)
        {
            // スケール変更などでメッシュが作り直されたら、ずらす前の頂点を取り直して当て直す
            if (!_isScrolling || changed != _text)
            {
                return;
            }

            CacheSourceMesh();
            ApplyScroll();
        }

        private void CacheSourceMesh()
        {
            var meshInfo = _text.textInfo.meshInfo;
            if (_sourceVertices.Length != meshInfo.Length)
            {
                _sourceVertices = new Vector3[meshInfo.Length][];
                _sourceColors = new Color32[meshInfo.Length][];
            }

            for (var i = 0; i < meshInfo.Length; i++)
            {
                _sourceVertices[i] = (Vector3[])meshInfo[i].vertices.Clone();
                _sourceColors[i] = (Color32[])meshInfo[i].colors32.Clone();
            }
        }

        private void ApplyScroll()
        {
            var textInfo = _text.textInfo;
            var rect = _text.rectTransform.rect;
            var shift = new Vector3(-_scroller.Offset, 0f, 0f);

            for (var i = 0; i < textInfo.characterCount; i++)
            {
                var character = textInfo.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                var materialIndex = character.materialReferenceIndex;
                var vertexIndex = character.vertexIndex;
                if (materialIndex >= _sourceVertices.Length ||
                    vertexIndex + 3 >= _sourceVertices[materialIndex].Length)
                {
                    continue;
                }

                var sourceVertices = _sourceVertices[materialIndex];
                var sourceColors = _sourceColors[materialIndex];
                var vertices = textInfo.meshInfo[materialIndex].vertices;
                var colors = textInfo.meshInfo[materialIndex].colors32;

                // 頂点は左下・左上・右上・右下の順。文字の中心で薄さを決める
                var centerX = (sourceVertices[vertexIndex].x + sourceVertices[vertexIndex + 2].x) * 0.5f + shift.x;
                var alpha = TagTickerScroller.EdgeAlpha(centerX, rect.xMin, rect.xMax, _edgeFadeWidth);

                for (var v = vertexIndex; v < vertexIndex + 4; v++)
                {
                    vertices[v] = sourceVertices[v] + shift;
                    var color = sourceColors[v];
                    color.a = (byte)(color.a * alpha);
                    colors[v] = color;
                }
            }

            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
