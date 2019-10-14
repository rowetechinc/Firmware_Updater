;Vault NSIS Installer
;Install Vault
;Written by Rico Castelo

; HISTORY
; -----------------------------------------------------------------
; Date            Initials    Version    Comments
; -----------------------------------------------------------------
; 08/05/2015      RC          0.0.5       Initial
; 08/28/2015      RC          0.5.1       Initial
; 08/28/2015      RC          0.5.2       Initial
; 09/10/2015      RC          0.5.4       Initial
; 09/29/2015      RC          0.5.5       Initial
; 10/29/2015      RC          0.5.5       Changed the sqlite version.
;

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Include Modern UI
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

  !include "MUI2.nsh"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; General
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
	;Name and file
	Name "Rowe Technology Inc. - Vault"
	OutFile "RTI.Firmware.Updater.Installer.v.2.1.0.Full.exe"

	;Default installation folder
	InstallDir "$PROGRAMFILES\Rowe Technology Inc\Fimware Updater"
  
	;Get installation folder from registry if available
	InstallDirRegKey HKCU "Software\Rowe Technology Inc - Fimware Updater" ""

	;Request application privileges for Windows Vista
	RequestExecutionLevel admin


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Variables
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

Var /GLOBAL VERSION_NUM
Var /GLOBAL VERSION_MAJOR
Var /GLOBAL VERSION_MINOR

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Interface Settings
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
	!define MUI_ABORTWARNING
	!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Pages
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
	!insertmacro MUI_PAGE_LICENSE "License.txt"
	!insertmacro MUI_PAGE_COMPONENTS
	!insertmacro MUI_PAGE_DIRECTORY
	!insertmacro MUI_PAGE_INSTFILES
  
	!insertmacro MUI_UNPAGE_CONFIRM
	!insertmacro MUI_UNPAGE_INSTFILES
  
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Languages
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
	!insertmacro MUI_LANGUAGE "English"

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Installer Sections
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Install Main Application and all DLL
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
Section "Core" SecCore

	StrCpy $VERSION_NUM "2.1.0" 
	StrCpy $VERSION_MAJOR "2"
	StrCpy $VERSION_MINOR "1"

	SetOutPath $INSTDIR

	; Add Files
	DetailPrint "Installing RTI Firmware Updater."
	CreateDirectory "$INSTDIR\x64"
	CreateDirectory "$INSTDIR\x86"
	File "..\bin\Release\Firmware Updater.exe"
	File "..\bin\Release\RTI.dll"
	File "/oname=x64\SQLite.Interop.dll" "..\..\packages\System.Data.SQLite.Core.1.0.111.0\build\net451\x64\SQLite.Interop.dll"
	File "/oname=x86\SQLite.Interop.dll" "..\..\packages\System.Data.SQLite.Core.1.0.111.0\build\net451\x86\SQLite.Interop.dll"
	
	; Create shortcut in start menu
	CreateDirectory "$SMPROGRAMS\RTI"
	CreateShortCut "$SMPROGRAMS\RTI\FirmwareUpdater.lnk" "$INSTDIR\Firmware Updater.exe"
	CreateShortCut "$SMPROGRAMS\RTI\Uninstall.lnk" "$INSTDIR\uninstall.exe"
	
	; Store installation folder
	WriteRegStr HKCU "Software\Rowe Technology Inc - Firmware Updater" "" $INSTDIR
	WriteRegStr HKCU "Software\Rowe Technology Inc - Firmware Updater" "Version" "$VERSION_MAJOR.$VERSION_MINOR"

	; Create uninstaller
	WriteUninstaller "$INSTDIR\Uninstall.exe"
	
	; Add to Add/Remove
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\RTI Firmware Updater" \
				 "DisplayName" "Rowe Technology Inc - Firmware Updater"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\RTI Firmware Updater" \
				 "UninstallString" "$\"$INSTDIR\uninstall.exe$\""

	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\RTI Firmware Updater" \
				 "Publisher" "Rowe Technology Inc."

	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\RTI Firmware Updater" \
				 "DisplayVersion" $VERSION_NUM

	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\RTI Firmware Updater" \
				 "VersionMajor" $VERSION_MAJOR

	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\RTI Firmware Updater" \
				 "VersionMinor" $VERSION_MINOR

SectionEnd

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Descriptions
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Language strings
LangString DESC_SecCore ${LANG_ENGLISH} "Core files."

; Assign language strings to sections
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
	!insertmacro MUI_DESCRIPTION_TEXT ${SecCore} $(DESC_SecCore)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Uninstaller Section
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
Section "Uninstall"

	Delete "$INSTDIR\Firmware Updater.exe"
	Delete "$INSTDIR\RTI.dll"
	Delete "$INSTDIR\x64\SQLite.Interop.dll"
	Delete "$INSTDIR\x86\SQLite.Interop.dll"

	; Old installations
	Delete "$INSTDIR\x64\SQLite.Interop.dll"
	Delete "$INSTDIR\x86\SQLite.Interop.dll"
	Delete "$INSTDIR\log4net.dll"
	Delete "$INSTDIR\OxyPlot.dll"
	Delete "$INSTDIR\OxyPlot.Wpf.dll"
	Delete "$INSTDIR\System.Data.SQLite.dll"
	;Delete "$INSTDIR\System.Data.SQLite.Linq.dll"
	Delete "$INSTDIR\Xceed.Wpf.Toolkit.dll"
	Delete "$INSTDIR\HelixToolkit.Wpf.dll"
	Delete "$INSTDIR\Microsoft.Expression.Drawing.dll"
	Delete "$INSTDIR\Newtonsoft.Json.dll"
	Delete "$INSTDIR\AutoUpdater.NET.dll"
	Delete "$INSTDIR\ReactiveUI.Blend.dll"
	Delete "$INSTDIR\ReactiveUI.dll"
	Delete "$INSTDIR\ReactiveUI.Routing.dll"
	Delete "$INSTDIR\ReactiveUI.Xaml.dll"
	Delete "$INSTDIR\System.Reactive.Core.dll"
	Delete "$INSTDIR\System.Reactive.Interfaces.dll"
	Delete "$INSTDIR\System.Reactive.Linq.dll"
	Delete "$INSTDIR\System.Reactive.PlatformServices.dll"
	Delete "$INSTDIR\System.Reactive.Windows.Threading.dll"
	Delete "$INSTDIR\System.Windows.Interactivity.dll"
	Delete "$INSTDIR\Caliburn.Micro.dll"
	Delete "$INSTDIR\WPFLocalizeExtension.dll"
	Delete "$INSTDIR\XAMLMarkupExtensions.dll"
	Delete "$INSTDIR\MahApps.Metro.dll"
	Delete "$INSTDIR\WriteableBitmapEx.Wpf.dll"
    Delete "$INSTDIR\Licenses.txt"
	Delete "$INSTDIR\EndUserRights.txt"
	Delete "$INSTDIR\RTI - Pulse User Guide.pdf"

	Delete "$INSTDIR\Uninstall.exe"

	; Remove the install directory
	RMDir "$INSTDIR\x64"
	RMDir "$INSTDIR\x86"
	RMDir "$INSTDIR"
	RMDir "$PROGRAMFILES\Rowe Technology Inc\Firmware Updater"
	RMDir "$PROGRAMFILES\Rowe Technology Inc"

	; Remove the program data
	Delete "C:\ProgramData\RTI\Pulse\FirmwareUpdaterErrorLog.log" 
	RMDir "C:\ProgramData\RTI\Firmware Updater"
	RMDir "C:\ProgramData\RTI"

	; Remove registry key for SQLite
	DeleteRegKey HKCU "Software\System.Data.SQLite"

	; Remove registery key
	DeleteRegKey /ifempty HKCU "Software\Rowe Technology Inc - Firmware Updater"
	
	; Remove Uninstall 
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Firmware Updater"

	; Remove menu short cuts
	Delete "$SMPROGRAMS\RTI\FirmwareUpdater.lnk"
	Delete "$SMPROGRAMS\RTI\Uninstall.lnk"
	RMDir "$SMPROGRAMS\RTI"

SectionEnd