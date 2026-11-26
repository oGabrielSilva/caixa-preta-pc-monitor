; Caixa-Preta installer (Inno Setup 6). Built by scripts/Build-Release.ps1, which passes:
;   /DAppVersion=<version>   /DStaging=<folder with Collector, Dashboard, Tray and Scripts>
; The install scripts do the real work (services, ACLs, shortcut); this file only copies the files and runs them,
; so a manual install with the scripts and this installer behave the same.

#ifndef AppVersion
  #error Pass /DAppVersion=<version> (scripts/Build-Release.ps1 does).
#endif
#ifndef Staging
  #error Pass /DStaging=<folder> (scripts/Build-Release.ps1 does).
#endif

[Setup]
AppId={{3FFC108C-83DA-413A-9AE9-1B9EEEE6D38E}
AppName=Caixa-Preta
AppVersion={#AppVersion}
AppVerName=Caixa-Preta {#AppVersion}
AppPublisher=Caixa-Preta contributors
VersionInfoVersion={#AppVersion}
; The scripts, the services and the tray app all expect %ProgramFiles%\PcMonitor: the folder is not a choice.
DefaultDirName={commonpf64}\PcMonitor
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Windows 11 always has the WebView2 runtime the tray app needs; Windows 10 may not (LTSC, offline images).
MinVersion=10.0.22000
LicenseFile=..\LICENSE
SetupIconFile=..\assets\brand\caixa-preta.ico
UninstallDisplayIcon={app}\Tray\CaixaPreta.exe
UninstallDisplayName=Caixa-Preta
OutputBaseFilename=CaixaPreta-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Services and the tray app are stopped in PrepareToInstall; the Restart Manager would only ask about them again.
CloseApplications=no

[Languages]
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[CustomMessages]
ptbr.OpenApp=Abrir o Caixa-Preta
ptbr.ConfiguringComponent=Configurando %1...
ptbr.ScriptFailed=Não foi possível configurar %1. Os detalhes estão em:%n%2
ptbr.RemoveData=Apagar também os dados gravados (leituras e quedas)?%n%nEles ficam em %1. Isso não pode ser desfeito.

[InstallDelete]
; Leftovers of an older version (for example a framework-dependent build) must not mix with the new files.
Type: filesandordirs; Name: "{app}\Collector"
Type: filesandordirs; Name: "{app}\Dashboard"
Type: filesandordirs; Name: "{app}\Tray"
Type: filesandordirs; Name: "{app}\Scripts"

[Files]
Source: "{#Staging}\Collector\*"; DestDir: "{app}\Collector"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#Staging}\Dashboard\*"; DestDir: "{app}\Dashboard"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#Staging}\Tray\*"; DestDir: "{app}\Tray"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#Staging}\Scripts\*"; DestDir: "{app}\Scripts"; Flags: ignoreversion

[Run]
Filename: "{app}\Tray\CaixaPreta.exe"; Description: "{cm:OpenApp}"; Flags: postinstall nowait runasoriginaluser skipifsilent

[Code]
var
  RemoveData: Boolean;

{ Runs one install script with Windows PowerShell 5.1; its output goes to LogFile. True when it succeeded. }
function RunScript(const Script, Arguments, LogFile: String): Boolean;
var
  Command: String;
  ResultCode: Integer;
begin
  Command := '-NoProfile -ExecutionPolicy Bypass -Command "Start-Transcript -Path ''' + LogFile + ''' -Append | Out-Null; ' +
    'try { & ''' + ExpandConstant('{app}\Scripts\') + Script + ''' ' + Arguments + '; $code = 0 } ' +
    'catch { $_ | Out-String | Write-Host; $code = 1 } finally { Stop-Transcript | Out-Null }; exit $code"';
  Result := Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'), Command, '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
    and (ResultCode = 0);
end;

procedure StopProgram(const FileName, Arguments: String);
var
  ResultCode: Integer;
begin
  Exec(FileName, Arguments, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

{ An update replaces files the services and the tray app hold open. net stop waits until each service has stopped. }
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopProgram(ExpandConstant('{sys}\net.exe'), 'stop PcMonitor.Dashboard');
  StopProgram(ExpandConstant('{sys}\net.exe'), 'stop PcMonitor.Collector');
  StopProgram(ExpandConstant('{sys}\taskkill.exe'), '/F /IM CaixaPreta.exe');
  Sleep(2000);
  Result := '';
end;

procedure Configure(const Script, Component: String);
var
  LogFile: String;
begin
  LogFile := ExpandConstant('{app}\install.log');
  WizardForm.StatusLabel.Caption := FmtMessage(CustomMessage('ConfiguringComponent'), [Component]);
  if not RunScript(Script, '-InPlace', LogFile) then
    MsgBox(FmtMessage(CustomMessage('ScriptFailed'), [Component, LogFile]), mbCriticalError, MB_OK);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    Configure('Install-Collector.ps1', 'o coletor');
    Configure('Install-Dashboard.ps1', 'o painel');
    Configure('Install-Tray.ps1', 'o app da bandeja');
  end;
end;

function InitializeUninstall(): Boolean;
begin
  RemoveData := (not UninstallSilent) and
    (MsgBox(FmtMessage(CustomMessage('RemoveData'), [ExpandConstant('{commonappdata}\PcMonitor')]), mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES);
  Result := True;
end;

{ Before Inno Setup deletes the files: the scripts remove the services, ACL entries and shortcut (and the data if asked). }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  LogFile, CollectorArguments: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    LogFile := ExpandConstant('{tmp}\caixa-preta-uninstall.log');
    CollectorArguments := '-Confirm:$false';
    if RemoveData then
      CollectorArguments := CollectorArguments + ' -RemoveData';
    RunScript('Uninstall-Tray.ps1', '-Confirm:$false', LogFile);
    RunScript('Uninstall-Dashboard.ps1', '-Confirm:$false', LogFile);
    RunScript('Uninstall-Collector.ps1', CollectorArguments, LogFile);
  end;
end;
