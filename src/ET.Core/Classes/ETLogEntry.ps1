class ETLogEntry {

    [datetime]$Timestamp
    [ETLogLevel]$Level
    [string]$Component
    [string]$Message
    [string]$ComputerName
    [string]$UserName
    [guid]$CorrelationId

    ETLogEntry() {
        $this.Timestamp     = Get-Date
        $this.Level         = [ETLogLevel]::Information
        $this.Component     = "General"
        $this.Message       = ""
        $this.ComputerName  = $env:COMPUTERNAME
        $this.UserName      = $env:USERNAME
        $this.CorrelationId = [guid]::NewGuid()
    }

    ETLogEntry(
        [ETLogLevel]$Level,
        [string]$Component,
        [string]$Message
    ) {
        $this.Timestamp     = Get-Date
        $this.Level         = $Level
        $this.Component     = $Component
        $this.Message       = $Message
        $this.ComputerName  = $env:COMPUTERNAME
        $this.UserName      = $env:USERNAME
        $this.CorrelationId = [guid]::NewGuid()
    }

    [string] ToString() {
        return "{0:yyyy-MM-dd HH:mm:ss} [{1}] [{2}] {3}" -f `
            $this.Timestamp,
            $this.Level,
            $this.Component,
            $this.Message
    }
}