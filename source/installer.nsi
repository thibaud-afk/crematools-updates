Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"
!define PRODUCT "CrémaTools"
!define VERSION "1.0.0"
Name "${PRODUCT} ${VERSION}"
OutFile "release\CremaTools Setup ${VERSION}.exe"
InstallDir "$LOCALAPPDATA\Programs\CremaTools"
InstallDirRegKey HKCU "Software\CremaTools" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma
SetCompressorDictSize 32
Icon "assets\crematools.ico"
UninstallIcon "assets\crematools.ico"
BrandingText "CremaLab — Offert avec plaisir par Thibaud CREMADES"
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\CremaTools.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Ouvrir CrémaTools"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "French"
Var TestInstall
Function .onInit
  StrCpy $TestInstall "0"
  ${GetParameters} $R0
  ${GetOptions} $R0 "/TESTMODE" $R1
  ${IfNot} ${Errors}
    StrCpy $TestInstall "1"
  ${EndIf}
FunctionEnd
Section "CrémaTools"
  SetShellVarContext current
  SetOutPath "$INSTDIR"
  Sleep 2000
  SetOverwrite on
  File /r /x "test-results" /x "*.pdb" /x "self-test-error.txt" "publish\*.*"
  WriteUninstaller "$INSTDIR\Uninstall CremaTools.exe"
  ${If} $TestInstall == "1"
    FileOpen $0 "$INSTDIR\.crematools-test-install" w
    FileClose $0
    Goto installDone
  ${EndIf}
  CreateDirectory "$SMPROGRAMS\CrémaTools"
  CreateShortcut "$SMPROGRAMS\CrémaTools\CrémaTools.lnk" "$INSTDIR\CremaTools.exe" "" "$INSTDIR\CremaTools.exe"
  CreateShortcut "$SMPROGRAMS\CrémaTools\Désinstaller CrémaTools.lnk" "$INSTDIR\Uninstall CremaTools.exe"
  CreateShortcut "$DESKTOP\CrémaTools.lnk" "$INSTDIR\CremaTools.exe"
  WriteRegStr HKCU "Software\CremaTools" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CremaTools" "DisplayName" "CrémaTools"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CremaTools" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CremaTools" "Publisher" "CremaLab — Thibaud CREMADES"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CremaTools" "UninstallString" '"$INSTDIR\Uninstall CremaTools.exe"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CremaTools" "QuietUninstallString" '"$INSTDIR\Uninstall CremaTools.exe" /S'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CremaTools" "DisplayIcon" "$INSTDIR\CremaTools.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CremaTools" "InstallLocation" "$INSTDIR"
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CremaTools" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CremaTools" "NoRepair" 1
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CremaTools" "EstimatedSize" $0
  installDone:
SectionEnd
Section "Uninstall"
  SetShellVarContext current
  IfFileExists "$INSTDIR\.crematools-test-install" removeFiles
  Delete "$DESKTOP\CrémaTools.lnk"
  Delete "$SMPROGRAMS\CrémaTools\CrémaTools.lnk"
  Delete "$SMPROGRAMS\CrémaTools\Désinstaller CrémaTools.lnk"
  RMDir "$SMPROGRAMS\CrémaTools"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CremaTools"
  DeleteRegKey HKCU "Software\CremaTools"
  removeFiles:
  !include "uninstall-files.nsh"
  Delete "$INSTDIR\.crematools-test-install"
  Delete "$INSTDIR\Uninstall CremaTools.exe"
  RMDir "$INSTDIR"
  ; User data and encrypted vaults deliberately remain in LocalAppData\CremaTools.
SectionEnd
