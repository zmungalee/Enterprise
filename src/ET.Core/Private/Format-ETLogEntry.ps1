function Format-ETLogEntry {
    <#
    .SYNOPSIS
        Formats an ETLogEntry object into a log string.
    #>

    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ETLogEntry]$LogEntry
    )

    return '{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [{2}] {3}' -f `
        $LogEntry.Timestamp,
        $LogEntry.Level,
        $LogEntry.Component,
        $LogEntry.Message
}