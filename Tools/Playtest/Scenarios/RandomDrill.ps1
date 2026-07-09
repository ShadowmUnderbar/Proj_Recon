$Global:PlaytestDrillStepCount = 0
$Global:PlaytestMoveKeys = @('W', 'A', 'S', 'D')
$Global:PlaytestFormKeys = @('Digit1', 'Digit2', 'Digit3')

function PlaytestScenarioStep {
    $Global:PlaytestDrillStepCount++
    $step = $Global:PlaytestDrillStepCount

    $moveKey = $Global:PlaytestMoveKeys[$step % $Global:PlaytestMoveKeys.Count]

    Invoke-Uloop -Command 'simulate-keyboard' -Params @{ action = 'Press'; key = $moveKey; duration = '1.5' } | Out-Null

    # 攻撃頻度アップ: 1ステップの間に複数回撃つ。狙う座標も毎回変えてエイムを動かし続ける
    $shotCount = 5
    for ($shot = 0; $shot -lt $shotCount; $shot++) {
        $fireX = 200 + (($step * 137 + $shot * 97) % 680)
        $fireY = 200 + (($step * 211 + $shot * 149) % 680)
        Invoke-Uloop -Command 'simulate-mouse-input' -Params @{ action = 'Click'; button = 'Left'; x = "$fireX"; y = "$fireY" } | Out-Null
        Start-Sleep -Milliseconds 250
    }

    if ($step % 3 -eq 0) {
        $formKey = $Global:PlaytestFormKeys[$step % $Global:PlaytestFormKeys.Count]
        Invoke-Uloop -Command 'simulate-keyboard' -Params @{ action = 'Press'; key = $formKey } | Out-Null
    }
    if ($step % 4 -eq 0) {
        Invoke-Uloop -Command 'simulate-mouse-input' -Params @{ action = 'LongPress'; button = 'Right'; x = "$fireX"; y = "$fireY"; duration = '1' } | Out-Null
    }
    if ($step % 5 -eq 0) {
        Invoke-Uloop -Command 'simulate-keyboard' -Params @{ action = 'Press'; key = 'Space' } | Out-Null
    }
}
