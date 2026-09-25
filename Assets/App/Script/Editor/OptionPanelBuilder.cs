using App.Common.Data;
using App.MainMenu.Views;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace App.Editor
{
    /// <summary>
    /// オプションパネル（利き手・VRの移動設定）のプレースホルダUIを組み立てるエディタ拡張。
    /// <see cref="MainMenuRoomBuilder"/>から呼ばれ、見た目は仮のまま操作だけ通るようにしている。
    /// 本番のUIに差し替えるときは、生成された階層の見た目を置き換えたうえで、
    /// <see cref="OptionPanelView"/>のボタン・スライダー参照を繋ぎ直すこと。
    /// </summary>
    public static class OptionPanelBuilder
    {
        // パネルの大きさ[px]。WorldSpace Canvasのスケール（1px＝1mm）を掛けた値がメートルになる
        private const float PanelWidth = 1400f;
        private const float PanelHeight = 900f;

        private const float RowLabelWidth = 420f;
        private const float RowHeight = 90f;
        private const float ButtonWidth = 280f;
        private const float SliderWidth = 520f;
        private const float ValueLabelWidth = 260f;

        private const float TitleFontSize = 64f;
        private const float RowFontSize = 40f;

        // 各行の縦位置[px]。パネル中心が原点
        private const float TitlePositionY = 340f;
        private const float DominantHandPositionY = 180f;
        private const float LocomotionPositionY = 40f;
        private const float MoveSpeedPositionY = -100f;
        private const float SnapTurnPositionY = -240f;

        // 各列の横位置[px]
        private const float LabelPositionX = -450f;
        private const float FirstColumnPositionX = 60f;
        private const float SecondColumnPositionX = 380f;
        private const float SliderPositionX = 130f;
        private const float ValueLabelPositionX = 500f;

        /// <summary>日本語を表示できるフォント。TMPの既定フォントには日本語のグリフが無い</summary>
        private const string FontAssetPath = "Assets/App/Font/nicokaku_v2-5 SDF.asset";

        private static readonly Color PanelColor = new(0.05f, 0.07f, 0.1f, 0.9f);
        private static readonly Color ButtonColor = new(0.2f, 0.28f, 0.38f, 1f);
        private static readonly Color SliderBackgroundColor = new(0.15f, 0.18f, 0.22f, 1f);
        private static readonly Color SliderFillColor = new(0.35f, 0.6f, 0.9f, 1f);

        /// <summary>オプションパネル一式を生成し、参照を繋いだルートを返す</summary>
        public static GameObject CreateOptionPanel()
        {
            var root = new GameObject("OptionPanelView", typeof(RectTransform), typeof(Canvas));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            CreateBackground(rootRect);
            CreateText("Title", rootRect, new Vector2(PanelWidth, 100f), new Vector2(0f, TitlePositionY),
                "OPTION", TitleFontSize, TextAlignmentOptions.Center);

            var dominantHandLeft = CreateRow(rootRect, "DominantHand", DominantHandPositionY, "利き手",
                "左", "右", out var dominantHandRight);

            var smoothLocomotion = CreateRow(rootRect, "Locomotion", LocomotionPositionY, "移動方式",
                "スムーズ", "テレポート", out var teleportLocomotion);

            var moveSpeedSlider = CreateSliderRow(rootRect, "MoveSpeed", MoveSpeedPositionY, "移動速度",
                out var moveSpeedLabel);
            var snapTurnSlider = CreateSliderRow(rootRect, "SnapTurnAngle", SnapTurnPositionY, "ターン角度",
                out var snapTurnLabel);

            // スライダーの範囲はプレハブにも保存しておく。実行時にも組み立て直すが、
            // 既定の0〜1のまま保存されていると、値の反映が先に走ったときにクランプされてしまう
            moveSpeedSlider.minValue = PlayerSettingRange.MinMoveSpeed;
            moveSpeedSlider.maxValue = PlayerSettingRange.MaxMoveSpeed;
            moveSpeedSlider.wholeNumbers = false;
            moveSpeedSlider.value = PlayerSettingRange.DefaultMoveSpeed;

            snapTurnSlider.minValue = PlayerSettingRange.MinSnapTurnAngle;
            snapTurnSlider.maxValue = PlayerSettingRange.MaxSnapTurnAngle;
            snapTurnSlider.wholeNumbers = true;
            snapTurnSlider.value = PlayerSettingRange.DefaultSnapTurnAngle;

            var view = root.AddComponent<OptionPanelView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_dominantHandLeftButton").objectReferenceValue = dominantHandLeft;
            serialized.FindProperty("_dominantHandRightButton").objectReferenceValue = dominantHandRight;
            serialized.FindProperty("_smoothLocomotionButton").objectReferenceValue = smoothLocomotion;
            serialized.FindProperty("_teleportLocomotionButton").objectReferenceValue = teleportLocomotion;
            serialized.FindProperty("_moveSpeedSlider").objectReferenceValue = moveSpeedSlider;
            serialized.FindProperty("_moveSpeedLabel").objectReferenceValue = moveSpeedLabel;
            serialized.FindProperty("_snapTurnAngleSlider").objectReferenceValue = snapTurnSlider;
            serialized.FindProperty("_snapTurnAngleLabel").objectReferenceValue = snapTurnLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static void CreateBackground(RectTransform parent)
        {
            var background = CreateRect("Background", parent, Vector2.zero, Vector2.zero);
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;

            var image = background.gameObject.AddComponent<Image>();
            image.color = PanelColor;

            // 背景がレイを吸うとボタンの手前で当たり判定が止まるため、素通しにする
            image.raycastTarget = false;
        }

        /// <summary>「ラベル＋選択ボタン2つ」の1行を作る</summary>
        private static Button CreateRow(
            RectTransform parent,
            string name,
            float positionY,
            string label,
            string firstLabel,
            string secondLabel,
            out Button secondButton)
        {
            CreateText($"{name}Label", parent, new Vector2(RowLabelWidth, RowHeight),
                new Vector2(LabelPositionX, positionY), label, RowFontSize, TextAlignmentOptions.Left);

            var firstButton = CreateButton($"{name}First", parent,
                new Vector2(ButtonWidth, RowHeight), new Vector2(FirstColumnPositionX, positionY), firstLabel);

            secondButton = CreateButton($"{name}Second", parent,
                new Vector2(ButtonWidth, RowHeight), new Vector2(SecondColumnPositionX, positionY), secondLabel);

            return firstButton;
        }

        /// <summary>「ラベル＋スライダー＋数値表示」の1行を作る</summary>
        private static Slider CreateSliderRow(
            RectTransform parent,
            string name,
            float positionY,
            string label,
            out TMP_Text valueLabel)
        {
            CreateText($"{name}Label", parent, new Vector2(RowLabelWidth, RowHeight),
                new Vector2(LabelPositionX, positionY), label, RowFontSize, TextAlignmentOptions.Left);

            var slider = CreateSlider($"{name}Slider", parent,
                new Vector2(SliderWidth, RowHeight), new Vector2(SliderPositionX, positionY));

            valueLabel = CreateText($"{name}Value", parent, new Vector2(ValueLabelWidth, RowHeight),
                new Vector2(ValueLabelPositionX, positionY), "-", RowFontSize, TextAlignmentOptions.Right);

            return slider;
        }

        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            return rect;
        }

        private static TMP_Text CreateText(
            string name,
            RectTransform parent,
            Vector2 size,
            Vector2 position,
            string content,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent, size, position);

            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font != null)
            {
                text.font = font;
            }
            else
            {
                Debug.LogWarning($"{FontAssetPath} が見つかりませんでした。日本語が表示されない可能性があります");
            }

            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.raycastTarget = false;

            return text;
        }

        private static Button CreateButton(
            string name,
            RectTransform parent,
            Vector2 size,
            Vector2 position,
            string label)
        {
            var rect = CreateRect(name, parent, size, position);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = ButtonColor;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText($"{name}Label", rect, size, Vector2.zero, label, RowFontSize,
                TextAlignmentOptions.Center);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        private static Slider CreateSlider(string name, RectTransform parent, Vector2 size, Vector2 position)
        {
            var rect = CreateRect(name, parent, size, position);
            var slider = rect.gameObject.AddComponent<Slider>();

            var background = CreateStretchedRect("Background", rect);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = SliderBackgroundColor;

            var fillArea = CreateStretchedRect("Fill Area", rect);
            var fill = CreateStretchedRect("Fill", fillArea);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = SliderFillColor;

            var handleArea = CreateStretchedRect("Handle Slide Area", rect);
            var handle = CreateRect("Handle", handleArea, new Vector2(RowHeight, RowHeight), Vector2.zero);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.direction = Slider.Direction.LeftToRight;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;

            return slider;
        }

        private static RectTransform CreateStretchedRect(string name, RectTransform parent)
        {
            var rect = CreateRect(name, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }
    }
}
