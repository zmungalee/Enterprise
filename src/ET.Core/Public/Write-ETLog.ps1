function Write-ETLog {

    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [ETLogLevel]$Level = [ETLogLevel]::Information,

        [string]$Component = 'General'
    )

    $entry = [ETLogEntry]::new()

    $entry.Level     = $Level
    $entry.Component = $Component
    $entry.Message   = $Message

    $formatted = Format-ETLogEntry -LogEntry $entry

    Write-Host $formatted
}