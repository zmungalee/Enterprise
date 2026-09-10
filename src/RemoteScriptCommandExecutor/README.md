# Enterprise Remote Script & Command Executor

Windows WPF administrative utility for executing PowerShell `.ps1` scripts or CMD commands on a remote Windows computer through authenticated PowerShell Remoting.

## Security requirements

- Administrator credentials are entered at runtime and are never persisted.
- Passwords are never written to logs or command-line arguments.
- Kerberos is the default authentication mechanism.
- The application does not modify the registry.
- The application does not change permanent PowerShell execution policy.
- The application does not disable UAC, Defender, AMSI, firewall protections, or other security controls.
- Remote sessions are closed after execution.
- Script files are restricted to `.ps1`.
- Execution timeouts are enforced.
- ExecutionPolicy Bypass is an explicit, per-execution advanced option only. It does not change the machine policy.
- Bypass requires a confirmation dialog and is recorded in the local execution output/audit section.
- CMD execution verifies that the remote identity has an administrator token before running the command.

## Prerequisites

- Windows 10/11 or Windows Server with .NET 8 Desktop Runtime/SDK as appropriate.
- PowerShell Remoting/WinRM enabled on the target.
- Network access to WinRM (HTTP 5985 in the current implementation).
- Appropriate administrative rights on the target.
- Domain/Kerberos authentication is recommended.

## Important operational note

The application intentionally does **not** set `TrustedHosts`, alter WinRM configuration, change UAC, or edit registry values to make remoting work. If the target is not correctly configured for secure remoting, the connection should be fixed through the organization's normal Windows/GPO configuration rather than weakened by the application.

## Build

```powershell
dotnet restore src/RemoteScriptCommandExecutor/RemoteScriptCommandExecutor.csproj
dotnet build src/RemoteScriptCommandExecutor/RemoteScriptCommandExecutor.csproj -c Release
```

## Usage

1. Enter the target computer name.
2. Enter the administrator username and password.
3. Test the connection.
4. Select **PowerShell Script** or **Remote Elevated CMD**.
5. For a script, browse to a `.ps1` file and optionally provide arguments.
6. For a command, enter the intended CMD command.
7. Execute and review output, errors, identity and audit information.

## ExecutionPolicy Bypass

Normal execution uses the target machine's existing policy. If a script is blocked specifically by execution policy, the operator can explicitly select the advanced bypass option. Bypass is applied only to the child PowerShell process used for that execution. No registry or permanent execution-policy change is performed.
