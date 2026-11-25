$ErrorActionPreference = 'Stop'
$runtimeRoot = Join-Path $env:LOCALAPPDATA 'QuizApp/Pronunciation'
$venv = Join-Path $runtimeRoot 'venv'
$python = Join-Path $venv 'Scripts/python.exe'

function Invoke-Checked {
    param([string]$Program, [string[]]$Arguments)
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Setup failed ($Program, exit $LASTEXITCODE)."
    }
}

if (!(Test-Path -LiteralPath $python)) {
    Write-Host 'Creating the Python 3.12 environment for local pronunciation assessment...'
    Invoke-Checked 'py' @('-3.12', '-m', 'venv', $venv)
}

Write-Host 'Installing the CPU runtime...'
Invoke-Checked $python @('-m', 'pip', 'install', '--index-url', 'https://download.pytorch.org/whl/cpu', 'torch==2.7.0+cpu', 'torchaudio==2.7.0+cpu')
Invoke-Checked $python @('-m', 'pip', 'install', 'funasr==1.4.16', 'transformers==4.43.0', 'soundfile==0.13.1', 'pypinyin==0.55.0', 'praat-parselmouth==0.4.6')
Invoke-Checked $python @((Join-Path $PSScriptRoot 'Pronunciation/install_assets.py'), $runtimeRoot)
Write-Host 'Local Mandarin pronunciation assessment is ready.'
