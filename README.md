# MSFS2020_AutoFPS

Based on muumimorko's idea and code in MSFS_AdaptiveLOD, as further developed by Fragtality in DynamicLOD and myself in DynamicLOD_ResetEdition.<br/><br/>

This utility is a new development that is a simplification of, and a slightly different concept to, DynamicLOD_ResetEdition. It aims to improve MSFS performance and smoothness by automatically changing the two settings that impact MSFS performance the most, namely TLOD and cloud quality settings, based on the current AGL and with an easy to use GUI. It provides features such as:<br/>
- Automatically adjusts TLOD to achieve a user-defined target FPS band based on pre or user-defined maximum and minimum TLODs,<br/>
- TLOD minimum on ground/landing option, which prioritises TLOD over FPS during these flight phases and also averts exacerbating existing texture tearing issues with DX12,</br>
- Cloud quality decrease option for when FPS can't be achieved at the lowest desired TLOD,<br/>
- Automatic pause when MSFS loses focus option, particularly useful if using FG due to varying FPS when MSFS gains or loses focus,</br>
- Automatic FPS settling timer on MSFS graphics mode and focus changes to allow FPS to stabilise before being acted upon,</br>
- Simultaneous PC, FG and VR mode compatibilty including correct FG FPS display and separate FPS targets for each mode,<br>
- A greatly simplified GUI option that uses pre-defined settings for an automated experience suited to most user scenarios, and</br>
- Auto restoration of original settings changed by the utility.<br/><br/>

Important:<br/> 
- This utility directly accesses active MSFS memory locations while MSFS is running to read and set TLOD and cloud quality settings on the fly at a maximum rate of one read and, if required, change per setting per second. The utility will first verify that the MSFS memory locations being used are still valid and if not, likely because of an MSFS version change, will attempt to find where they have been relocated. If it does find the new memory locations and they pass validation tests, the utility will update itself automatically and will function as normal. If it can't find or validate MSFS memory locations at any time when starting up, the utility will self-restrict to read only mode to prevent the utility making changes to unknown MSFS memory locations.<br/>
- As such, I believe the app to be robust in its interaction with validated MSFS memory locations and to be responsible in disabling itself if it can't guarantee that. Nonetheless, this utility is offered as is and no responsibility will be taken for unintended negative side effects. Use at your own risk!<br/><br/>

If you are not familiar with what MSFS graphics settings do, specifically TLOD and cloud quality, and don't understand the consequences of changing them, it is highly recommended you do not use this utility.
<br/><br/>

This utility is unsigned because I am a hobbyist and the cost of obtaining certification is prohibitive to me. As a result, you may get a warning message of a potentially dangerous app when you download it in a web browser like Chrome. You can either trust this download, based on feedback you can easily find on Avsim and Youtube, and run a virus scan and malware scan before you install just be sure, otherwise choose not to and not have this utility version.<br/><br/>

## Requirements

The Installer will install the following Software:
- .NET 7 Desktop Runtime (x64)
- MobiFlight Event/WASM Module

<br/>

Currently in development, but when available [Download here](https://github.com/ResetXPDR/MSFS2020_AutoFPS/releases/latest)

(Under Assests, the MSFS2020_AutoFPS-Installer-vXYZ.exe File)

<br/><br/>

## Installation / Update / Uninstall
Basically: Just run the Installer.<br/>

Some Notes:
- MSFS2020_AutoFPS has to be stopped before installing.
- If the MobiFlight Module is not installed or outdated, MSFS also has to be stopped.
- If you have duplicate MobiFlight Modules installed, in either your official or community folders, the utility may display 0 value Sim Values and otherwise not function. Remove the duplicate versions, rerun the utility installer and it should now work.
- Do not run the Installer as Admin!
- If you wish to retain your settings for an update version, do NOT uninstall first, as that deletes all app files, including the config file. Just run the installer, select update and your settings will be retained.
- For Auto-Start either your FSUIPC7.ini or EXE.xml (MSFS) is modified. The Installer does not create a Backup.
- The utility may be blocked by Windows Security or your AV-Scanner, try if unblocking and/or setting an Exception helps (for the whole Folder)
- The Installation-Location is fixed to %appdata%\MSFS2020_AutoFPS (your Users AppData\Roaming Folder) and can't be changed.
  - Binary in %appdata%\MSFS2020_AutoFPS\bin
  - Logs in %appdata%\MSFS2020_AutoFPS\log
  - Config: %appdata%\MSFS2020_AutoFPS\MSFS2020_AutoFPS.config
- If after installing and running the app your simconnect always stays red, try downloading and installing a Microsoft official version of “Microsoft Visual C++ 2015 - 2022 Redistributable”, which may be missing from your Windows installation.
- If you get an "MSFS compatibility test failed - Read Only mode" message it is either because you have changed MSFS settings in your usercfg.opt file beyond what is possible to set in the MSFS settings menu or a new version of MSFS has come out that has a different memory map to what the app expects. If the former, go into MSFS settings at the main menu and reset to default (F12) the graphics settings for both PC and VR mode, then make all changes to MSFS within the MSFS settings menu. If the latter, I will likely be already aware of it and working on a solution, but if you may be one of the first to encounter it (eg. on an MSFS beta) then please do let me know.

<br/><br/>

## Usage / Configuration

- Starting manually: anytime, but preferably before MSFS or in the Main Menu. The utility will stop itself when MSFS closes. 
- Closing the Window does not close the utiltiy, use the Context Menu of the SysTray Icon.
- Clicking on the SysTray Icon opens the Window (again).
- Runnning as Admin NOT required (BUT: It is required to be run under the same User/Elevation as MSFS).
- Connection Status
  - Red values indicate not connected, green is connected.
- Sim Values
  - Will not show valid values unless all three connections are green. n/a means not available right now.
  - Green means the sim value is at or better than target value being sought, red means at lowest level or worse than target value being sought, orange means TLOD is auto adjusting, black is shown otherwise.
  - FPS shows the FPS averaged over 5 seconds, to smooth out any transient FPS spikes experienced when panning or loading new scenery or objects so that automated MSFS setting changes are minimised.
  - Priority will show whether FPS or TLOD Min are the current automation priority, with the latter only being shown if the TLOD min for ground/landing is enabled and conditions are such that working towards TLOD Min because of your flight phase (on or near the ground) now has priority over maintaining FPS. 
- General
  - Target FPS - The most important setting in this app. Set it to what FPS you want the app to target while running, noting that this value should be at the mid to lower end of what your system is capable of otherwise the app will be unlikely to achieve it. There is a setting for each graphics mode (PC, FG and VR) which you can only change while in that mode and on the ground or in a flight. This is particularly useful if regularly switching between FG mode and VR mode in your flights as the FG FPS target can be significantly higher than the one for VR.
  - Use Expert Options - When disabled allows the app to use default settings in conjuction with your chosen target FPS that should produce good automated FPS tracking, provided you have set a realisting FPS target in the first place. When enabled, the UI expands to show additional MSFS settings to adjust. If you do not understand these settings and their impact on MSFS performance and graphics quality, it is strongly recommended that you do not use these expert options and you should uncheck this option.
  - Open window on app start - determines the app window's startup state.
  - Status Message - On app startup indicates key system messages, such as:
    - Before loading a flight - whether a newer version of the app is available to download and install
    - Loading in to a flight  - whether MSFS memory integrity test have failed, and
    - Flight is loaded - showing detected DX version, Graphics Mode (PC, FG, or VR), and app pause or FPS settling time status as applicable. The FPS settling timer runs for 6 seconds to allow FPS to settle between pausing/unpausing and VR/PC/FG mode transitions. This allows the FPS to stabilise before engaging automatic functions and should lead to much smaller TLOD changes when seeking the target FPS on such transitions.
- MSFS Settings
  -  FPS Tolerance - Determines how much variance from your target FPS must occur before the app will adjust MSFS settings to achieve the target FPS and what nominal magnitude those changes will be. The lower the setting, the more reactive the app will be, the more MSFS settings changes will occur and the changes will be smaller. Vice versa for higher settings. When expert settings are disabled, the default value of 5% should provide the most balanced experience.
  -  TLOD Min on Ground/Landing - When enabled, your TLOD will immediately change to TLOD minimum when your flight is loaded and will stay that way any time you are on the ground. Once you take off and start climbing, auto TLOD will kick in gradually on a sliding scale up to 1000 ft AGL, then normal range auto operation above that. On descent to landing, when you cross 2000 ft AGL, the app will progressively adjust your TLOD down so that as you cross 1000 ft AGL it will be at TLOD minimum and there it will stay locked all the way to the ground, taxi and shutdown at the gate. If you are level below 1000 ft AGL, then commence descending, TLOD will rapidly switch to TLOD minimum with no stepping as it thinks you are landing imminently. If you are using cloud quality reduction options, even though you are at TLOD minimum on the ground which would normally cause cloud quality to reduce straight away, it will only reduce if you are below achieving your FPS target FPS and recover again as normal. It is generally recommended to leave this setting disabled, as will happen with expert settings disabled, but enable it if you are getting texture corruption with DX12 on the ground, especially at airports, or want your TLOD on the ground or when landing to always be TLOD min.
  -  TLOD Min - Sets the minimum TLOD the automation algorithm will use. When expert settings are disabled, the app will use 50% of your existing MSFS TLOD setting.
  -  TLOD Max - Sets the maximum TLOD the automation algorithm will use. When expert settings are disabled, the app will use 200% of your existing MSFS TLOD setting.
  -  Decrease Cloud Quality - When enabled, will reduce cloud quality by one level if TLOD has already auto reduced to TLOD Min and FPS is still below target FPS by more than the FPS tolerance.
  -  Cloud Recovery TLOD - The TLOD level required to cancel an active cloud quality reduction state and restore cloud quality back to its initial higher quality level. This provides a TLOD buffer to account for the increased TLOD achieved by reducing cloud quality to stop FPS adaption constantly toggling on and off. This parameter must be at least 5 TLOD more than TLOD Min and 5 TLOD below TLOD Max and ideally is set to 50 TLOD above TLOD Min provided that the aforementioned conditions can be met.
  -  Pause when MSFS loses focus - Defaults to on when not using expert options and is user selectable via expert options. This will stop TLOD and, if applicable, cloud quality from changing while you are focused on another app and not MSFS. It is particularly useful for when using FG as the FG active and inactive frame rate can vary quite considerably and because FG is not always an exact doubling of non-FG FPS.  
<br/><br/>
