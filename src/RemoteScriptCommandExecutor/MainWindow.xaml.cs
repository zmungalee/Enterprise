using Microsoft.Win32;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Enterprise.RemoteExecutor;

public partial class MainWindow : Window
{
    public MainWindow() { InitializeComponent(); }

    private void ModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ScriptPanel == null) return;
        bool script = ModeBox.SelectedIndex == 0;
        ScriptPanel.Visibility = script ? Visibility.Visible : Visibility.Collapsed;
        CommandPanel.Visibility = script ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BrowseScript_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PowerShell scripts (*.ps1)|*.ps1", CheckFileExists = true };
        if (dialog.ShowDialog() == true) ScriptBox.Text = dialog.FileName;
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ValidateCommonInput();
            SetBusy(true);
            OutputBox.Text = await Task.Run(() => ExecuteRemote("$env:COMPUTERNAME; whoami; [Security.Principal.WindowsIdentity]::GetCurrent().IsSystem", false, null));
            ConnectionStatus.Text = "Connected";
        }
        catch (Exception ex) { ConnectionStatus.Text = "Failed"; OutputBox.Text = FormatError(ex); }
        finally { SetBusy(false); }
    }

    private async void Execute_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ValidateCommonInput();
            int timeout = ParseTimeout();
            bool script = ModeBox.SelectedIndex == 0;
            string? scriptText = null;

            if (script)
            {
                if (!File.Exists(ScriptBox.Text)) throw new InvalidOperationException("The selected .ps1 file does not exist.");
                if (!string.Equals(Path.GetExtension(ScriptBox.Text), ".ps1", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Only .ps1 files are permitted.");
                scriptText = await File.ReadAllTextAsync(ScriptBox.Text, Encoding.UTF8);
                if (BypassBox.IsChecked == true)
                {
                    var confirm = MessageBox.Show("This will use ExecutionPolicy Bypass for this execution only. No registry or permanent policy change will be made. Continue?", "Security Override", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (confirm != MessageBoxResult.Yes) return;
                }
            }
            else if (string.IsNullOrWhiteSpace(CommandBox.Text)) throw new InvalidOperationException("Enter a CMD command.");

            SetBusy(true);
            string output = await Task.Run(() => ExecuteRemote(script ? BuildScriptRunner(scriptText!, BypassBox.IsChecked == true, ArgumentsBox.Text) : BuildCmdRunner(), script, script ? null : CommandBox.Text, timeout));
            OutputBox.Text = output;
        }
        catch (Exception ex) { OutputBox.Text = FormatError(ex); }
        finally { SetBusy(false); }
    }

    private string ExecuteRemote(string remoteScript, bool isScript, string? command, int timeout = 300)
    {
        using SecureString securePassword = PasswordBox.SecurePassword.Copy();
        var credential = new PSCredential(UsernameBox.Text.Trim(), securePassword);
        var uri = new Uri($"http://{TargetBox.Text.Trim()}:5985/wsman");
        var connection = new WSManConnectionInfo(uri, "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", credential)
        {
            AuthenticationMechanism = AuthenticationMechanism.Kerberos,
            OperationTimeout = timeout * 1000,
            OpenTimeout = Math.Min(timeout * 1000, 30000),
            IdleTimeout = Math.Max(timeout * 1000, 60000)
        };

        using Runspace runspace = RunspaceFactory.CreateRunspace(connection);
        try
        {
            runspace.Open();
            using PowerShell ps = PowerShell.Create();
            ps.Runspace = runspace;
            ps.AddScript(remoteScript);
            if (command != null) ps.AddParameter("Command", command);
            var results = ps.Invoke();
            var sb = new StringBuilder();
            foreach (var result in results) sb.AppendLine(result?.ToString());
            foreach (var error in ps.Streams.Error) sb.AppendLine("ERROR: " + error);
            sb.AppendLine();
            sb.AppendLine("----- AUDIT -----");
            sb.AppendLine($"Target: {TargetBox.Text.Trim()}");
            sb.AppendLine($"Account: {UsernameBox.Text.Trim()}");
            sb.AppendLine($"Mode: {(isScript ? "PowerShell Script" : "Remote Elevated CMD")}");
            if (isScript) sb.AppendLine($"ExecutionPolicy override: {BypassBox.IsChecked == true}");
            sb.AppendLine($"Timestamp (UTC): {DateTime.UtcNow:O}");
            sb.AppendLine($"Errors: {ps.HadErrors}");
            return sb.ToString();
        }
        finally { runspace.Close(); }
    }

    private static string BuildScriptRunner(string script, bool bypass, string args)
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(script));
        string policy = bypass ? " -ExecutionPolicy Bypass" : "";
        string safeArgs = Convert.ToBase64String(Encoding.UTF8.GetBytes(args ?? string.Empty));
        return $@"
$ErrorActionPreference = 'Stop'
$temp = Join-Path ([IO.Path]::GetTempPath()) ('EnterpriseRemote_' + [guid]::NewGuid().ToString('N') + '.ps1')
try {{
  [IO.File]::WriteAllBytes($temp, [Convert]::FromBase64String('{encoded}'))
  $argText = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{safeArgs}'))
  $argList = @()
  if ($argText.Trim()) {{ $argList = [regex]::Matches($argText, '""([^""\\]*(?:\\.[^""\\]*)*)""|([^\s]+)') | ForEach-Object {{ if ($_.Groups[1].Success) {{ $_.Groups[1].Value }} else {{ $_.Groups[2].Value }} }} }}
  & powershell.exe -NoLogo -NoProfile{policy} -File $temp @argList 2>&1
  if ($LASTEXITCODE -ne 0) {{ exit $LASTEXITCODE }}
}} finally {{ Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue }}";
    }

    private static string BuildCmdRunner() => @"
$ErrorActionPreference = 'Stop'
Write-Output ('Remote identity: ' + [Security.Principal.WindowsIdentity]::GetCurrent().Name)
$admin = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Write-Output ('Administrator token: ' + $admin)
if (-not $admin) { throw 'The remote session does not have an elevated administrator token. No UAC or registry changes are attempted.' }
cmd.exe /d /c $Command
exit $LASTEXITCODE";

    private void ValidateCommonInput()
    {
        if (string.IsNullOrWhiteSpace(TargetBox.Text)) throw new InvalidOperationException("Enter a target computer.");
        if (TargetBox.Text.Contains(' ')) throw new InvalidOperationException("Target computer name must not contain spaces.");
        if (string.IsNullOrWhiteSpace(UsernameBox.Text)) throw new InvalidOperationException("Enter an administrator username.");
        if (PasswordBox.SecurePassword.Length == 0) throw new InvalidOperationException("Enter the administrator password.");
    }

    private int ParseTimeout()
    {
        if (!int.TryParse(TimeoutBox.Text, out int value) || value < 5 || value > 3600) throw new InvalidOperationException("Timeout must be between 5 and 3600 seconds.");
        return value;
    }

    private void SetBusy(bool busy) { ExecuteButton.IsEnabled = !busy; }
    private static string FormatError(Exception ex) => $"FAILED: {ex.Message}{Environment.NewLine}{Environment.NewLine}No registry or permanent security-policy changes were made.";
    private void Clear_Click(object sender, RoutedEventArgs e) { OutputBox.Clear(); ConnectionStatus.Text = "Not tested"; }
}
