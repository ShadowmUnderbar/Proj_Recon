$Global:PlaytestDrillStepCount = 0
$Global:PlaytestMoveKeys = @('W', 'A', 'S', 'D')
$Global:PlaytestFormKeys = @('Digit1', 'Digit2', 'Digit3')

function PlaytestScenarioStep {
    $Global:PlaytestDrillStepCount++
    $step = $Global:PlaytestDrillStepCount

    $moveKey = $Global:PlaytestMoveKeys[$step % $Global:PlaytestMoveKeys.Count]
    $fireX = 200 + (($step * 137) % 680)
    $fireY = 200 + (($step * 211) % 680)

    Invoke-Uloop -Command 'simulate-keyboard' -Params @{ action = 'Press'; key = $moveKey; duration = '1.5' } | Out-Null
    Invoke-Uloop -Command 'simulate-mouse-input' -Params @{ action = 'LongPress'; button = 'Left'; x = "$fireX"; y = "$fireY"; duration = '1.5' } | Out-Null

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
