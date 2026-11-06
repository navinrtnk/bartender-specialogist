# Copyright (c) 2026 Navin Ratnayake (@navinrtnk). SPDX-License-Identifier: MIT
# Optional Windows development tool; the game plays the committed WAV on every platform.
$ErrorActionPreference = 'Stop'
$output = Join-Path (Split-Path -Parent $PSScriptRoot) 'Assets\Audio\great-drink.wav'
$voice = New-Object -ComObject SAPI.SpVoice
$voices = $voice.GetVoices()
for ($i = 0; $i -lt $voices.Count; $i++) {
    if ($voices.Item($i).GetDescription() -match 'David') { $voice.Voice = $voices.Item($i) }
}
$stream = New-Object -ComObject SAPI.SpFileStream
try {
    $stream.Open($output, 3, $false)
    $voice.AudioOutputStream = $stream
    $voice.Rate = -1
    $voice.Speak('Great drink!') | Out-Null
} finally {
    $stream.Close()
}
