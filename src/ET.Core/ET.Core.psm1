# ============================================================================
# Enterprise Toolkit - ET.Core Module
# ============================================================================

# Load classes (Enums first, then other classes)

$classesPath = Join-Path $PSScriptRoot 'Classes'

if (Test-Path $classesPath) {

    # Load all enums first
    Get-ChildItem -Path $classesPath -Filter '*.ps1' |
        Where-Object { $_.Name -like '*Level.ps1' -or $_.Name -like '*Enum.ps1' } |
        Sort-Object Name |
        ForEach-Object {
            . $_.FullName
        }

    # Load remaining classes
    Get-ChildItem -Path $classesPath -Filter '*.ps1' |
        Where-Object { $_.Name -notlike '*Level.ps1' -and $_.Name -notlike '*Enum.ps1' } |
        Sort-Object Name |
        ForEach-Object {
            . $_.FullName
        }
}

# Load private functions

$privatePath = Join-Path $PSScriptRoot 'Private'

if (Test-Path $privatePath) {

    Get-ChildItem -Path $privatePath -Filter '*.ps1' |
        Sort-Object Name |
        ForEach-Object {
            . $_.FullName
        }
}

# Load public functions

$publicPath = Join-Path $PSScriptRoot 'Public'

if (Test-Path $publicPath) {

    Get-ChildItem -Path $publicPath -Filter '*.ps1' |
        Sort-Object Name |
        ForEach-Object {
            . $_.FullName
        }
}

# Export all Enterprise Toolkit public commands

Export-ModuleMember -Function *-ET*
