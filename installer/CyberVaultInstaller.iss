#define MyAppName "CyberVault"
#define MyAppVersion "4.3.0"
#define MyAppPublisher "CyberVault"
#define MyAppURL "https://github.com/CyberNilsen/CyberVault"
#define MyAppExeName "CyberVault.exe"
#define MyAppSourcePath "C:\Users\ander\Downloads\CyberVault"
#define MyAppCopyright "Copyright © 2025 CyberVault"
#define MyAppContact "cyberbrothershq@gmail.com"

[Setup]
; NEW AppId for version 4.3.0 - SECURITY ENHANCEMENT UPDATE
AppId={{B8C9D0E1-F2A3-4567-B8C9-D0E1F2A34567}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion} - Enhanced Security
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppCopyright={#MyAppCopyright}
AppContact={#MyAppContact}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=C:\Users\ander\Desktop\
OutputBaseFilename=CyberVault_Installer
; Specify 64-bit architecture
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
; Enable version overwrite and upgrade handling
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Security Enhancement Installer
VersionInfoCopyright={#MyAppCopyright}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

; Compression settings for smaller install package
Compression=lzma2/ultra64
SolidCompression=yes

; Appearance settings
WizardStyle=modern
WizardResizable=yes
SetupIconFile={#MyAppSourcePath}\Images\CyberVaultLogo.ico
UninstallDisplayIcon={app}\Images\CyberVaultLogo.ico
WizardImageStretch=no
WizardSizePercent=120

; System requirements
PrivilegesRequired=admin
MinVersion=10.0

; CRITICAL: Allow upgrades and handle existing installations
AllowNoIcons=yes
DisableProgramGroupPage=auto
DisableWelcomePage=no
DisableDirPage=no
DisableReadyPage=no
DisableFinishedPage=no
ShowLanguageDialog=yes
ShowComponentSizes=yes
SetupLogging=yes
CloseApplications=force
RestartApplications=yes
UninstallDisplayName={#MyAppName} {#MyAppVersion} - Enhanced Security
UninstallRestartComputer=no
UsePreviousAppDir=yes
UsePreviousSetupType=yes
UsePreviousTasks=yes
; Enable upgrade mode
CreateUninstallRegKey=yes
UninstallFilesDir={app}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"

[Messages]
; Clean installer messages
english.WelcomeLabel1=Welcome to {#MyAppName} v{#MyAppVersion} Setup
english.WelcomeLabel2=This will install {#MyAppName} version {#MyAppVersion} on your computer.%n%nIt is recommended that you close all other applications before continuing.
english.FinishedHeadingLabel={#MyAppName} Setup Complete
english.FinishedLabel=Setup has finished installing {#MyAppName} on your computer. The application may be launched by selecting the installed shortcuts.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Components]
Name: "main"; Description: "Main Application"; Types: full compact custom; Flags: fixed

[Types]
Name: "full"; Description: "Full installation"
Name: "compact"; Description: "Compact installation"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Dirs]
Name: "{app}\Data"; Permissions: everyone-full
Name: "{app}\Logs"; Permissions: everyone-full
Name: "{app}\Temp"; Permissions: everyone-full
Name: "{app}\Plugins"; Permissions: everyone-modify
Name: "{app}\Images"; Permissions: everyone-modify
Name: "{app}\Documentation"; Permissions: everyone-modify
Name: "{localappdata}\{#MyAppName}"; Flags: uninsneveruninstall

[Files]
; Main executable - CRITICAL: Use ignoreversion and replacesameversion
Source: "{#MyAppSourcePath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion replacesameversion; Components: main

; PDB file (debug symbols)
Source: "{#MyAppSourcePath}\CyberVault.pdb"; DestDir: "{app}"; Flags: ignoreversion replacesameversion; Components: main

; All DLL files - specify each one explicitly to ensure they're included
Source: "{#MyAppSourcePath}\D3DCompiler_47_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion replacesameversion; Components: main
Source: "{#MyAppSourcePath}\PenImc_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion replacesameversion; Components: main
Source: "{#MyAppSourcePath}\PresentationNative_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion replacesameversion; Components: main
Source: "{#MyAppSourcePath}\vcruntime140_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion replacesameversion; Components: main
Source: "{#MyAppSourcePath}\wpfgfx_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion replacesameversion; Components: main

; Images folder and all contents
Source: "{#MyAppSourcePath}\Images\*"; DestDir: "{app}\Images"; Flags: ignoreversion replacesameversion recursesubdirs createallsubdirs; Components: main

; Documentation files (optional)
; Source: "{#MyAppSourcePath}\Documentation\*"; DestDir: "{app}\Documentation"; Flags: ignoreversion replacesameversion recursesubdirs createallsubdirs; Components: docs

; Include ALL other files without exclusions to capture everything in the zip
Source: "{#MyAppSourcePath}\*"; DestDir: "{app}"; Flags: ignoreversion replacesameversion recursesubdirs createallsubdirs; Excludes: "*.tmp,*.bak,*.log,*.obj,*.tlog"; Components: main

[Icons]
; Create desktop shortcut (optional)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Images\CyberVaultLogo.ico"; Tasks: desktopicon; Comment: "Launch {#MyAppName}"

; Create Quick Launch shortcut (optional)
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Images\CyberVaultLogo.ico"; Tasks: quicklaunchicon; Comment: "Launch {#MyAppName} quickly"

; Start Menu shortcuts
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Images\CyberVaultLogo.ico"; Comment: "Launch {#MyAppName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"; Comment: "Uninstall {#MyAppName}"

[Registry]
; Add registry entries with proper version tracking for v4.3.0
Root: HKLM; Subkey: "Software\{#MyAppName}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppName}"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppName}"; ValueType: string; ValueName: "PreviousVersion"; ValueData: "{code:GetPreviousVersion}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppName}"; ValueType: dword; ValueName: "Installed"; ValueData: 1; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppName}"; ValueType: string; ValueName: "Publisher"; ValueData: "{#MyAppPublisher}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppName}"; ValueType: string; ValueName: "InstallDate"; ValueData: "{code:GetCurrentDate}"; Flags: uninsdeletekey



; Add license info to registry
Root: HKLM; Subkey: "Software\{#MyAppName}\License"; ValueType: string; ValueName: "LicenseType"; ValueData: "Standard"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppName}\License"; ValueType: string; ValueName: "LicenseExpiry"; ValueData: "Perpetual"; Flags: uninsdeletekey

; Add application to the Windows Firewall exceptions
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules"; ValueType: string; ValueName: "CyberVault Application v4.3.0"; ValueData: "v2.10|Action=Allow|Active=TRUE|Dir=In|Protocol=6|Profile=Private|App={app}\{#MyAppExeName}|Name={#MyAppName} Application v4.3.0|Desc=Allow {#MyAppName} Enhanced Security to access the network"; Flags: uninsdeletevalue

[Run]
; Kill existing processes before installation
Filename: "{cmd}"; Parameters: "/c taskkill /f /im {#MyAppExeName}"; Flags: runhidden skipifdoesntexist; StatusMsg: "Closing running application..."

; Install Visual C++ Redistributables if needed
Filename: "{tmp}\vcredist_x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing Visual C++ Redistributables..."; Flags: waituntilterminated; Check: VCRedistNeedsInstall

; Run post-install configuration
Filename: "{app}\post_install.bat"; Flags: runhidden; StatusMsg: "Configuring enhanced security..."; Check: FileExists('{app}\post_install.bat')

; Show readme after installation (optional)
; Filename: "{app}\Documentation\Migration_Guide.txt"; Description: "View Setup Guide"; Flags: postinstall shellexec skipifsilent; Components: docs

; Offer to run the application after installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Run cleanup routines during uninstallation
Filename: "{cmd}"; Parameters: "/c taskkill /f /im {#MyAppExeName}"; Flags: runhidden skipifdoesntexist
Filename: "{app}\uninstall_cleanup.bat"; Flags: runhidden skipifdoesntexist

[InstallDelete]
; Clean old files that are no longer needed during upgrade
Type: files; Name: "{app}\old_version.dll"
Type: filesandordirs; Name: "{app}\Temp\*"
Type: files; Name: "{app}\CyberTools_v*.dll"; Check: IsUpgrade
Type: files; Name: "{app}\*.old"; Check: IsUpgrade

[UninstallDelete]
; Remove installed files during uninstallation
Type: files; Name: "{app}\*.log"
Type: files; Name: "{app}\*.tmp"
Type: filesandordirs; Name: "{app}\Temp"
Type: filesandordirs; Name: "{app}"
Type: filesandordirs; Name: "{group}"

[INI]
; Create or update INI file settings
Filename: "{app}\config.ini"; Section: "Setup"; Key: "InstallPath"; String: "{app}"
Filename: "{app}\config.ini"; Section: "Setup"; Key: "Version"; String: "{#MyAppVersion}"
Filename: "{app}\config.ini"; Section: "Setup"; Key: "Language"; String: "{language}"
Filename: "{app}\config.ini"; Section: "Setup"; Key: "InstallDate"; String: "{code:GetCurrentDate}"
Filename: "{app}\config.ini"; Section: "Setup"; Key: "UpgradeFrom"; String: "{code:GetPreviousVersion}"

; Security-specific configuration (optional)
; Filename: "{app}\config.ini"; Section: "Security"; Key: "EncryptionLevel"; String: "Enhanced"
; Filename: "{app}\config.ini"; Section: "Security"; Key: "PBKDF2Iterations"; String: "1000000"
; Filename: "{app}\config.ini"; Section: "Security"; Key: "SecurityUpdateVersion"; String: "{#MyAppVersion}"

[Code]
var
  IsUpgradeInstall: Boolean;
  PreviousVersion: String;
  UpgradePage: TOutputMsgWizardPage;
  SecurityWarningPage: TOutputMsgWizardPage;

// Get the previous version if upgrading
function GetPreviousVersion(Param: String): String;
begin
  if IsUpgradeInstall then
    Result := PreviousVersion
  else
    Result := 'New Installation';
end;

// Check if this is a release build (to determine whether to include PDB files)
function IsReleaseBuild: Boolean;
begin
  Result := True; // Set to False for debug builds
end;

// Check if this is an upgrade from previous version
function IsUpgrade: Boolean;
begin
  Result := IsUpgradeInstall;
end;

// Get current date in ISO format
function GetCurrentDate(Param: String): String;
begin
  Result := GetDateTimeString('yyyy-mm-dd', '-', '-');
end;

// Check if Visual C++ Redistributable needs to be installed
function VCRedistNeedsInstall(): Boolean;
var
  RegKey: String;
  RegValue: Cardinal;
begin
  // Check for Visual C++ 2019/2022 Redistributable
  RegKey := 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64';
  if RegQueryDWordValue(HKLM, RegKey, 'Installed', RegValue) then
  begin
    Result := (RegValue <> 1);
  end
  else
  begin
    Result := True;
  end;
end;

// Compare version strings
function CompareVersions(V1, V2: String): Integer;
var
  P, N1, N2: Integer;
begin
  Result := 0;
  while (Result = 0) and ((V1 <> '') or (V2 <> '')) do
  begin
    P := Pos('.', V1);
    if P > 0 then
    begin
      N1 := StrToIntDef(Copy(V1, 1, P - 1), 0);
      Delete(V1, 1, P);
    end
    else if V1 <> '' then
    begin
      N1 := StrToIntDef(V1, 0);
      V1 := '';
    end
    else
    begin
      N1 := 0;
    end;

    P := Pos('.', V2);
    if P > 0 then
    begin
      N2 := StrToIntDef(Copy(V2, 1, P - 1), 0);
      Delete(V2, 1, P);
    end
    else if V2 <> '' then
    begin
      N2 := StrToIntDef(V2, 0);
      V2 := '';
    end
    else
    begin
      N2 := 0;
    end;

    if N1 < N2 then
      Result := -1
    else if N1 > N2 then
      Result := 1;
  end;
end;

// Force close application
procedure ForceCloseApplication();
var
  ResultCode: Integer;
begin
  Exec('taskkill', '/f /im {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

// Add custom wizard pages
procedure InitializeWizard;
begin
  // Create standard upgrade notification page if this is an upgrade
  if IsUpgradeInstall then
  begin
    UpgradePage := CreateOutputMsgPage(wpSelectDir, 
      'Upgrade Installation Detected', 
      'Existing installation found',
      'An existing installation of {#MyAppName} (version ' + PreviousVersion + ') was found.' + #13#10 + #13#10 +
      'This installer will upgrade your installation to version {#MyAppVersion}.' + #13#10 + #13#10 +
      'Your settings and data will be preserved.' + #13#10 + #13#10 +
      'Click Next to continue with the upgrade.');
  end;
end;

// Check for prerequisites and handle existing installations
function InitializeSetup(): Boolean;
var
  UninstallKey: String;
  UninstallString: String;
  ResultCode: Integer;
  OldVersion: String;
  NewVersion: String;
  CompareResult: Integer;
  MsgResult: Integer;
begin
  Result := True;
  IsUpgradeInstall := False;
  PreviousVersion := '';

  // Force close any running instances
  ForceCloseApplication();

  // Check for existing installations
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}_is1';
  if RegQueryStringValue(HKLM, UninstallKey, 'DisplayVersion', OldVersion) then
  begin
    NewVersion := '{#MyAppVersion}';
    PreviousVersion := OldVersion;
    CompareResult := CompareVersions(OldVersion, NewVersion);
    
    if CompareResult = 0 then
    begin
      // Same version - offer to repair/reinstall
      MsgResult := MsgBox('The same version of {#MyAppName} (v' + OldVersion + ') is already installed.' + #13#10 + #13#10 +
        'Would you like to repair the installation?', 
        mbConfirmation, MB_YESNO);
      if MsgResult = IDNO then
      begin
        Result := False;
        Exit;
      end;
      IsUpgradeInstall := True;
    end
    else if CompareResult < 0 then
    begin
      // Older version - security upgrade
      IsUpgradeInstall := True;
      // Continue with installation
    end
    else
    begin
      // Newer version installed - warn user
      MsgResult := MsgBox('A newer version of {#MyAppName} (v' + OldVersion + ') is already installed.' + #13#10 + #13#10 +
        'You are trying to install version ' + NewVersion + '.' + #13#10 + #13#10 +
        'Do you want to downgrade to the older version?', 
        mbConfirmation, MB_YESNO);
      if MsgResult = IDNO then
      begin
        Result := False;
        Exit;
      end;
      IsUpgradeInstall := True;
    end;
  end;

  // Additional check in Program Files
  if DirExists(ExpandConstant('{pf}\{#MyAppName}')) then
  begin
    if not IsUpgradeInstall then
    begin
      IsUpgradeInstall := True;
      PreviousVersion := 'Unknown';
    end;
  end;
end;

// Handle installation steps
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    // Force close application before installing
    ForceCloseApplication();
    
    // Wait a moment for processes to close
    Sleep(1000);
  end;
  
  if CurStep = ssPostInstall then
  begin
    // Update registry with new version info
    RegWriteStringValue(HKLM, 'Software\{#MyAppName}', 'Version', '{#MyAppVersion}');
    RegWriteStringValue(HKLM, 'Software\{#MyAppName}', 'InstallDate', GetCurrentDate(''));
    
    if IsUpgradeInstall then
    begin
      RegWriteStringValue(HKLM, 'Software\{#MyAppName}', 'PreviousVersion', PreviousVersion);
      RegWriteStringValue(HKLM, 'Software\{#MyAppName}', 'UpgradeDate', GetCurrentDate(''));
    end;
  end;
end;

// Handle uninstallation
function InitializeUninstall(): Boolean;
begin
  Result := MsgBox('Do you want to completely remove ' + '{#MyAppName}' + ' and all of its components?' + #13#10 + #13#10 +
    'Note: Your personal vault files and settings will be preserved unless you choose otherwise.', 
    mbConfirmation, MB_YESNO) = IDYES;
end;

// Handle uninstallation events
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  KeepDataStr: String;
  KeepData: Boolean;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Force close application
    ForceCloseApplication();
    
    // Ask if user wants to keep vault files
    if MsgBox('Would you like to keep your encrypted vault files and configuration?' + #13#10 + #13#10 +
      'If you keep them, you can restore your passwords if you reinstall CyberVault.', 
      mbConfirmation, MB_YESNO) = IDYES then
    begin
      RegWriteStringValue(HKCU, 'Software\{#MyAppName}', 'KeepDataOnUninstall', 'yes');
    end
    else
    begin
      RegWriteStringValue(HKCU, 'Software\{#MyAppName}', 'KeepDataOnUninstall', 'no');
    end;
  end;
  
  if CurUninstallStep = usPostUninstall then
  begin
    // Check the flag and delete data if requested
    if RegQueryStringValue(HKCU, 'Software\{#MyAppName}', 'KeepDataOnUninstall', KeepDataStr) then
    begin
      KeepData := KeepDataStr = 'yes';
      if not KeepData then
      begin
        DelTree(ExpandConstant('{localappdata}\{#MyAppName}'), True, True, True);
        // Also delete AppData\Roaming\CyberVault if user chose to remove data
        DelTree(ExpandConstant('{userappdata}\{#MyAppName}'), True, True, True);
      end;
    end;
    
    // Clean up registry
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\{#MyAppName}');
  end;
end;