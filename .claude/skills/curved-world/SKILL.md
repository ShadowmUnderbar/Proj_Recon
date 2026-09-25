---
name: curved-world
description: RECONの「水平線カーブ」（ワールドを頂点シェーダで丸く曲げ、遠くのものが水平線の向こうへ隠れる見た目）に新しいオブジェクト・演出・エフェクトを対応させる。新しい敵・背景・弾・レイ・トレーサー・パーティクル・HUDマーカーを追加したとき、それがカーブに追従せず「地面から浮く」「傾く」「消える」場合は必ずこのスキルを使う。「カーブに対応させて」「曲面に合わせて」「水平線」「CurvedWorld」「地面から浮く」「遠くの敵が消える」などの依頼が該当する。曲率やカメラ高さの調整、実機でのチューニングもここに含む。
---

# curved-world: 水平線カーブ実装ガイド

RECONの**水平線カーブ**は、ワールドを頂点シェーダで丸く沈ませ、遠くのものが水平線の向こうへ隠れるように見せる描画。

**曲げているのは描画だけ**である。コライダー・NavMesh・弾道・当たり判定はすべて平らなまま動く。だから新しい機能を足すとき、ゲームロジック側でカーブを意識する必要はない。意識が要るのは**描画と、ワールド座標を画面へ変換する経路**だけ。

## 中核の原理（これだけは先に理解する）

沈下量を**XZ平面上の距離だけの関数**にしている。

```hlsl
float CurvedWorld_Drop(float2 positionXZ)
{
    float2 delta = positionXZ - _CurvedWorldOrigin.xz;
    return dot(delta, delta) * _CurvedWorldParams.x;
}
```

ここから3つの性質が出る。**対応を判断するときは毎回この3つに当てる。**

1. **垂直に立ったジオメトリは歪まない。** 壁の上端も下端もXZが同じなので、同じ量だけ沈んで剛体のまま降りる。「立体をどうごまかすか」という問題は発生しない
2. **同じXZにあるものは一緒に沈む。** 高さ2mを飛ぶ弾は、その地点の地面が3m沈めば一緒に3m沈み、地面からの高さ2mが保たれる。だから見た目と当たり判定がずれない
3. **歪むのは水平方向に広がったジオメトリだけ。** つまり地面と、横に長い壁。これは歪んでほしい対象そのもの

## 数式（調整と検証で必ず使う）

| 求めたいもの | 式 | 備考 |
|---|---|---|
| 沈下量 | `曲率 × 距離²` | |
| 水平線までの距離 | `sqrt(カメラ高さ / 曲率)` | ここより遠くは折り返して隠れる |
| 目標の水平線からの曲率 | `カメラ高さ / 水平線距離²` | 設定はこちらで入力する |
| **水平線上の沈下量** | **= カメラ高さ** | 曲率によらず一定。バウンズ拡張量の根拠 |
| 直線の中央のたわみ | `曲率 × 線の長さ² / 4` | **距離に依存しない。長さだけで決まる** |

設定は曲率ではなく「水平線までの距離[m]」で入力する。曲率の生の値（0.00129 など）から見え方は想像できない。

## ファイル早見表

| 役割 | パス |
|---|---|
| 変位の共通処理（全シェーダがinclude） | `Assets/App/Material/Shaders/CurvedWorld.hlsl` |
| 陰影ありシェーダ（4パス） | `Assets/App/Material/Shaders/CurvedWorldLit.shader` |
| 陰影なしシェーダ（線・エフェクト向け） | `Assets/App/Material/Shaders/CurvedWorldUnlit.shader` |
| Unlitのデプス／影パス共通部 | `Assets/App/Material/Shaders/CurvedWorldUnlitDepth.hlsl` |
| 設定SO | `Assets/App/Script/Common/Data/CurvedWorldConfig.cs` |
| シェーダと同じ式のC#側の写し | `Assets/App/Script/Common/Data/CurvedWorldDisplacement.cs` |
| パラメータ配布（**カーブの中心**） | `Assets/App/Script/Common/Views/CurvedWorldView.cs` |
| 地面グリッド生成 | `Assets/App/Script/Common/Views/CurvedWorldGroundView.cs` |
| カリング用バウンズ拡張 | `Assets/App/Script/Common/Views/CurvedWorldBoundsView.cs` |
| **線を刻む共通処理** | `Assets/App/Script/Common/Views/CurvedWorldLine.cs` |
| カメラ配置の適用 | `Assets/App/Script/Common/Views/CurvedWorldCameraRigView.cs` |
| 実機での曲率調整 | `Assets/App/Script/Common/Views/CurvedWorldTunerView.cs` |
| 本編用の設定アセット | `Assets/App/Graphics/CurvedWorldConfig_Battle.asset` |
| プロトタイプ用の設定アセット | `Assets/App/Graphics/CurvedWorldConfig.asset` |
| プロトタイプシーン（ビルド対象外） | `Assets/Scenes/CurvedWorldPrototype.unity` |

カーブの中心は `Player.prefab` のルート（`PlayerMoveView` と同じオブジェクト）に付いた `CurvedWorldView` が配る。**中心はカメラではなくプレイヤーの論理位置**にする。HMDを中心にすると首を振るたびに地形が波打つ。

## 新しいものを追加するときの判断表

| 追加するもの | シェーダ | モード | 追加作業 |
|---|---|---|---|
| コンパクトなメッシュ（敵・プレイヤー・小物） | `CurvedWorldLit` | **ピボット** | `CurvedWorldBoundsView` をプレハブのルートへ |
| スキンメッシュのキャラクタ | `CurvedWorldLit` | **ピボット** | 同上 |
| 横に長い壁・床・背景 | `CurvedWorldLit` | 頂点ごと | 親に `CurvedWorldBoundsView` を1つ |
| `LineRenderer` の線 | `CurvedWorldUnlit` | 頂点ごと | **`CurvedWorldLine.SetLine` で刻む（必須）** |
| `TrailRenderer` の軌跡 | `CurvedWorldUnlit` | 頂点ごと | 不要（点が毎フレーム打たれるので元から細かい） |
| 半透明エフェクト・粒子 | `CurvedWorldUnlit` | 頂点ごと | 不要 |
| 頭に追従するUI・キャンバス | **そのまま** | — | 不要（中心からの距離が0なので沈まない） |
| UIを指すレイ（`VrUiRayView`） | **そのまま** | — | **曲げない**。指す先のUIが曲がらないため外れる |
| ワールド座標から画面へ出すHUDマーカー | — | — | `CurvedWorldDisplacement.Apply()` を通す |

### ピボットか頂点ごとかの決め方

マテリアルの `_CurvedWorldPerObject` トグルで切り替える。

幅 w のオブジェクトが距離 d にあるとき、手前側と奥側の沈下量の差は `曲率 × 2 × d × w`。
幅2mの敵が40m先にいて曲率0.0035なら0.55mの落差、つまり15度傾く。**コンパクトな立体はピボット基準にする。**

逆に横に長い壁をピボット基準にすると、壁全体が同じ量だけ沈んで地面から浮く。**長いものは頂点ごと。**

## LineRenderer の刻み（最も間違えやすい）

`LineRenderer` は指定した点にしか頂点が生まれない。2点の線にカーブシェーダを当てても、**両端が沈むだけで間は弦のまま残り、放物面から浮く**。シェーダを当てるだけでは直らない。

```csharp
// 悪い例（両端しか沈まない）
lineRenderer.positionCount = 2;
lineRenderer.SetPosition(0, start);
lineRenderer.SetPosition(1, end);

// 良い例（曲率に応じて刻む）
CurvedWorldLine.SetLine(lineRenderer, start, end);
```

刻み間隔は `sqrt(4 × 許容たわみ / 曲率)` で自動計算される。曲率は**シェーダへ配られているグローバル値**から読むので、実機で曲率を振っても追随する。設定アセットから読んではいけない。

長さが毎フレーム変わる収縮アニメーションでは `SetLinePositionsKeepingCount` を使う。点の数を変えると `LineRenderer` の内部バッファが作り直される。

カーブ未使用のシーンではグローバル曲率が0のままなので、刻み間隔が無限大になり点は2つ、つまり従来と同じ描画に落ちる。**この性質のおかげで、共通コードに刻みを入れても他の機能に影響しない。**

## 落とし穴（すべて実際に踏んだもの）

### シェーダ

- **全パスに変位を通す。** ForwardLit だけ対応して ShadowCaster / DepthOnly / DepthNormals を忘れると、そのパスだけ元の位置に残る。影が本体から離れた場所に出る
- **不透明マテリアルはデプス・影のパスが要る。** 半透明だけを想定してUnlitに1パスしか持たせないと、不透明設定のマテリアル（`EnemyBullet` など）がデプスプリパスやSSAO有効時に破綻し、影も出なくなる
- **沈下量に上限を付けない。** 途中で頭打ちにすると遠景が平らな面になり、その面が水平線の上へ折り返して「2枚目の床」として空に浮く
- **`color.a *= fogFactor` を書かない。** URPの `ComputeFogFactor` はフォグのキーワードが立っていないとき0を返す。フォグ無効時にすべて透明になる
- **法線も倒す。** 位置だけ沈めると地面の陰影が平らなまま。`CurvedWorld_ApplyNormal` がヤコビアンの逆転置で処理する

### カリング

- **バウンズを広げる。** カリングはCPU側のバウンズで行われ、シェーダが頂点を沈めたことを知らない。広げないと、まだ見えている壁が消える
- **広げる量はカメラ高さの数倍。** 水平線上の沈下量は曲率によらずカメラ高さと等しいので、それを基準にする。ステージの広さから決める必要はない
- **ワールドの下方向へ広げる。** `localBounds` はローカル軸に沿うので、傾いた対象ではローカルの-Yが真下ではない。`InverseTransformVector(Vector3.down * margin)` で変換してから包む
- **実行中の曲率に追随させる。** 生成時に焼き込むと、実機で曲率を上げたとき地面がカリングで消える

### アセット・エディタ

- **生成メッシュには `HideFlags.DontSave`。** 付けないと分割数ぶんの頂点がシーンファイルへ丸ごと焼き込まれる（一辺120分割で2.4MB）
- **マテリアルのシェーダを差し替えるとカスタムレンダーキューがリセットされる。** 不透明マテリアルを差し替えたら `renderQueue` を戻す
- **Unityのコンポーネントに `??` は使えない。** `GetComponent<T>() ?? AddComponent<T>()` は fake-null を素通りする。`if (x == null)` で書く
- **`OnValidate` の `delayCall` は多重登録される。** スライダーを動かしている間に何十回も走ってエディタが固まる。登録済みフラグで防ぐ

## 検証のしかた

見た目の印象ではなく**数値で測る**。`uloop-execute-dynamic-code` でPlay中に実行する。

線が曲面をなぞっているかは、密にサンプリングして折れ線と実際の曲面の距離を取る。

```csharp
var view = UnityEngine.Object.FindFirstObjectByType<App.Common.Views.CurvedWorldView>();
var d = view.Displacement;

float MaxError(Vector3 start, Vector3 end, int positionCount)
{
    var verts = new Vector3[positionCount];
    for (var i = 0; i < positionCount; i++)
        verts[i] = d.Apply(Vector3.Lerp(start, end, (float)i / (positionCount - 1)));

    var max = 0f;
    for (var s = 0; s <= 600; s++)
    {
        var t = s / 600f;
        var truePos = d.Apply(Vector3.Lerp(start, end, t));
        var scaled = t * (positionCount - 1);
        var seg = Mathf.Min(Mathf.FloorToInt(scaled), positionCount - 2);
        max = Mathf.Max(max, Vector3.Distance(truePos, Vector3.Lerp(verts[seg], verts[seg + 1], scaled - seg)));
    }
    return max;
}
```

バウンズが広がったかは `renderer.localBounds` を拡張の前後で比べる。沈下量は `view.Displacement.DropAt(pos)` で出る。

見た目の確認は `uloop-screenshot`。ただし本編のカメラは俯角75度（PCデバッグ時）でほぼ真下を向いているため、カーブは見えにくい。Play中に `Camera.main.transform.parent.localRotation` を35〜40度に変えると水平線が視界に入る。

## 曲率の調整

`CurvedWorldConfig_Battle` の「水平線までの距離」を変える。実機では `CurvedWorldTunerView` が両グリップ＋右スティックで振れる（設定アセットは書き換わらないので、良い値をログから拾ってInspectorへ戻す）。

**強くするほど遠くの敵が隠れる。** 水平線をスポーン範囲（25〜40m）まで寄せると、その距離の敵は必ず隠れる。撃ってくる敵が見えなくなるのはゲームプレイの変更なので、勝手に強くしない。

カメラ高さを上げると、同じ水平線距離でも沈下量が増えて効果が出る。ただし敵との距離感が変わるため、**カメラ高さの変更は必ず相談してから行う**。

## 現時点で未対応のもの

- **Metaパス（ライトマップのベイク）とMotionVectors** は実装していない。GIのベイクやTAAを使う場合、曲げる前のジオメトリで計算される
- **VRでの快適さが未評価。** 両眼とも同じワールド空間変形なので立体視としては破綻しないが、遠方が沈む動きが頭の並進移動に対してどう感じるかは実機で見ないとわからない
- **実機でのfps計測が未実施**
- **地面はワールドに固定**でプレイヤーを追従しない。見える範囲の倍以上の大きさにして端が見切れないようにしている

## 関連

- 実装の経緯と実測値: PR #96
- 実機での挙動確認: [`playtest-run`](../playtest-run/SKILL.md)
