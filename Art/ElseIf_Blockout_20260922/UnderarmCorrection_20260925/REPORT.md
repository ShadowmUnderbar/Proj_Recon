# 脇下の追加補正

最適化固有の問題ではありません。
CurrentとOptimizedで、身頃の脇下（高さ1.15〜1.20m、中心から左右0.08〜0.20m）の頂点数と腕ウェイトが一致していました。
前後身頃のUpperArmとShoulderの合計は、この範囲で平均約57〜62%、最大約85〜96%でした。
既存のShoulderが鎖骨相当の骨で、Clavicleという別名の骨はありません。
Bone位置と範囲別の詳細はDiagnosis.jsonです。

## 採用した補正

腕ウェイトを直接減衰する案を2通り試しましたが、通常姿勢と45度で接触が増えたため採用していません。
元のウェイトを復元し、試験した減衰を基に、左右独立のDLHN_Underarm_Drape_L/Rへ変換しました。
高く腕を開く方向でのみ、身頃が下へ残る成分を主体に加えています。
肩上面と袖外周は補正対象から外し、脇を完全に固定する形にはしていません。
脇を削ったり、人体を変更したりしていません。

Base、自然立ち、45度、Asymmetric Aim、One Arm Backでは新しい補正値は0です。
90度とWideでは1になります。
補正は角度に応じて連続的に変化しますが、任意の全エイム遷移を検証したものではありません。
2種類の補正を衣装と追従するデザイン面へ合計20個配置しています。

## 結果

Wideで脇下身頃の測定領域は、補正前より平均47.18mm下へ戻りました。
肩上部と袖外周の差は0.001mm未満、腹部側と裾の差は0mmでした。
基準姿勢と通常角度は数値誤差の範囲で維持しています。

148,131頂点、288,752三角形、10 Material、74 Boneを維持しています。
頂点追加、Edge Loop追加、トポロジー復元は0です。
Optimizedとの基準座標と面接続の差分は0件、Masterの編集前指紋との差分も0件でした。

Base、自然立ち、45度、90度、Wide、Asymmetric Aim、One Arm Backを含む全12姿勢を検証しました。
対象ジャケットと内部人体の三角形交差は0件です。
左右それぞれ764点の手指検査点も全姿勢で袖内に収まっています。
検証記録は08_stress_audit.jsonとMotion_Measurement.jsonです。

## 比較画像

Current Wide、Optimized Wide、Corrected Wideを同じカメラ、照明、48サンプルでレンダーしました。
[Wideの3モデル比較](Wide_Comparison.jpg)
[256px相当の比較](Wide_Small_256.jpg)
[指定6姿勢](Pose_Review.jpg)

256px画像高の比較では、身頃から袖下面へ一枚に張る大きな三角形が弱まり、肩と袖の外周を維持できています。
この縮小表示は前回と同じ参考条件であり、Unityの実ゲーム画面での検証ではありません。

## 保存

ElseIf_Underarm_Corrected.blendに保存しています。
補正版のCollectionはElseIf_Game_Optimized_Corrected、SceneはElseIf_Underarm_Reviewです。
Current、Optimizedの補正前モデルとMasterもファイル内に保持しています。
編集前バックアップはBefore_Underarm_Correction.blendです。
基準姿勢のフレーム1で停止しています。
