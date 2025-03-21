[Setup] 
AppName=CyberVault 
AppVersion=1.0.0 
DefaultDirName={pf}\CyberVault 
DefaultGroupName=CyberVault 
OutputDir=C:\Users\ander\Desktop\ 
OutputBaseFilename=CyberVaultInstaller 
Compression=lzma 
SolidCompression=yes 
ArchitecturesInstallIn64BitMode=x64 
SetupIconFile=C:\Users\ander\Desktop\CyberVault\Images\CyberVaultLogo.ico   
UninstallDisplayIcon={app}\CyberVaultLogo.ico
 
[Files] 
; Main executable 
Source: "C:\Users\ander\Desktop\CyberVault\CyberVault.exe"; DestDir: "{app}"; Flags: ignoreversion 
 
; PDB file 
Source: "C:\Users\ander\Desktop\CyberVault\CyberVault.pdb"; DestDir: "{app}"; Flags: ignoreversion 
 
; DLL files 
Source: "C:\Users\ander\Desktop\CyberVault\D3DCompiler_47_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "C:\Users\ander\Desktop\CyberVault\PenImc_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "C:\Users\ander\Desktop\CyberVault\PresentationNative_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "C:\Users\ander\Desktop\CyberVault\vcruntime140_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "C:\Users\ander\Desktop\CyberVault\wpfgfx_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion 
 
; Icon file (required for Control Panel & shortcuts) 
Source: "C:\Users\ander\Desktop\CyberVault\Images\CyberVaultLogo.ico"; DestDir: "{app}"; DestName: "CyberVaultLogo.ico"; Flags: ignoreversion 
 
[Icons] 
; Create a shortcut on the desktop 
Name: "{userdesktop}\CyberVault"; Filename: "{app}\CyberVault.exe"; IconFilename: "{app}\CyberVaultLogo.ico" 
 
; Create a shortcut in the Start Menu 
Name: "{group}\CyberVault"; Filename: "{app}\CyberVault.exe"; IconFilename: "{app}\CyberVaultLogo.ico" 
 
[Registry] 
; Add registry entry to mark installation 
Root: HKCU; Subkey: "Software\CyberVault"; ValueType: dword; ValueName: "Installed"; ValueData: 1 
 
; Control Panel Icon - Main registry entry
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1"; ValueType: string; ValueName: "DisplayIcon"; ValueData: "{app}\CyberVaultLogo.ico"; Flags: uninsdeletevalue
 
; Additional entries for more compatibility
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\CyberVault"; ValueType: string; ValueName: "DisplayIcon"; ValueData: "{app}\CyberVaultLogo.ico"; Flags: uninsdeletevalue 
 
; 32-bit Compatibility on 64-bit Windows 
Root: HKLM; Subkey: "Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\CyberVault"; ValueType: string; ValueName: "DisplayIcon"; ValueData: "{app}\CyberVaultLogo.ico"; Flags: uninsdeletevalue 
 
[UninstallDelete] 
; Remove installed files during uninstallation 
Name: "{app}\CyberVault.exe"; Type: files 
Name: "{app}\CyberVault.pdb"; Type: files 
Name: "{app}\D3DCompiler_47_cor3.dll"; Type: files 
Name: "{app}\PenImc_cor3.dll"; Type: files 
Name: "{app}\PresentationNative_cor3.dll"; Type: files 
Name: "{app}\vcruntime140_cor3.dll"; Type: files 
Name: "{app}\wpfgfx_cor3.dll"; Type: files 
Name: "{app}\CyberVaultLogo.ico"; Type: files 
 
; Remove installation directory after uninstall 
Name: "{app}"; Type: dirifempty 
 
[Run] 
; Run the application after installation 
Filename: "{app}\CyberVault.exe"; Description: "Launch CyberVault"; Flags: postinstall nowait