# ET.Core Module

$Public = Join-Path $PSScriptRoot 'Public'
$Private = Join-Path $PSScriptRoot 'Private'

if (Test-Path $Private) {
    Get-ChildItem $Private -Filter *.ps1 | ForEach-Object { . $_.FullName }
}

if (Test-Path $Public) {
    Get-ChildItem $Public -Filter *.ps1 | ForEach-Object { . $_.FullName }
}

function Get-ETPlatform {
    [CmdletBinding()]
    param()

    [PSCustomObject]@{
        Name='Enterprise Toolkit'
        Version='0.1.0'
        Status='Initialized'
    }
}

function Start-ETPlatform { Write-Verbose 'Enterprise Toolkit started.' }
function Stop-ETPlatform { Write-Verbose 'Enterprise Toolkit stopped.' }

Export-ModuleMember -Function Get-ETPlatform,Start-ETPlatform,Stop-ETPlatform