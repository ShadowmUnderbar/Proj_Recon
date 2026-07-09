function PlaytestScenarioStep {
    Invoke-Uloop -Command 'simulate-keyboard' -Params @{ action = 'Press'; key = 'W'; duration = '1.5' } | Out-Null
    Invoke-Uloop -Command 'simulate-mouse-input' -Params @{ action = 'LongPress'; button = 'Left'; x = '540'; y = '400'; duration = '1.5' } | Out-Null
}
