# MSFS2020_AutoFPS

Based on muumimorko's idea and code in MSFS_AdaptiveLOD, as further developed by Fragtality in DynamicLOD and myself in DynamicLOD_ResetEdition.<br/><br/>

This utility is a new development that is a simplification of, and a slightly different concept to, DynamicLOD_ResetEdition. It aims to improve MSFS performance and smoothness by automatically changing the two settings that impact MSFS performance the most, namely TLOD and cloud quality settings, based on the current AGL and an easy to use GUI. It provides features such as:<br/>
- Automatically adjusts TLOD automatically to achieve a user-defined target FPS band based on pre or user-defined maximum and minimum LODs,<br/>
- TLOD minimum on ground/landing option, which prioritises TLOD over FPS during these flight phases and also averts exacerbating existing texture tearing issues with DX12,</br>
- Cloud quality decrease option for when FPS can't be achieved at the lowest desired TLOD,<br/>
- Automatic pause when MSFS loses focus option, particularly useful if using FG due to varying FPS when MSFS gains or loses focus,</br>
- Automatic FPS settling timer on MSFS graphics mode and focus changes to allow FPS to stabilise before acting upon,</br>
- Simultaneous PC, FG and VR mode compatibilty including correct FG FPS display and separate FPS targets for each mode,<br>
- A greatly simplified GUI, when expert options are not enabled, that uses pre-defined settings for an automated experience suited to most user scenarios, and</br>
- Auto restoration of original settings changed by the utility.<br/><br/>

Important:<br/> 
- This utility directly accesses active MSFS memory locations while MSFS is running to read and set OLOD, TLOD and cloud quality settings on the fly at a maximum rate of one read and, if required, change per setting per second. The utility will first verify that the MSFS memory locations being used are still valid and if not, likely because of an MSFS version change, will attempt to find where they have been relocated. If it does find the new memory locations and they pass validation tests, the utility will update itself automatically and will function as normal. If it can't find or validate MSFS memory locations at any time when starting up, the utility will self-restrict to read only mode to prevent the utility making changes to unknown MSFS memory locations.<br/>
- As such, I believe the app to be robust in its interaction with validated MSFS memory locations and to be responsible in disabling itself if it can't guarantee that. Nonetheless, this utility is offered as is and no responsibility will be taken for unintended negative side effects. Use at your own risk!<br/><br/>

If you are not familiar with what MSFS graphics settings do, specifically TLOD, OLOD and cloud quality, and don't understand the consequences of changing them, it is highly recommended you do not use this utility.
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
- If you experience an "MSFS compatibility test failed - Read Only mode" it is either because you have changed MSFS settings in your usercfg.opt file beyond what is possible to set in the MSFS settings menu or a new version of MSFS has come out that has a different memory map to what the app expects. If the former, go into MSFS settings at the main menu and reset to default for both PC and VR mode, then make all changes to MSFS within the MSFS settings menu. If the latter, I will likely be already aware of it and working on a solution, but if you are likely one of the first (eg. on an MSFS beta) then do let me know.

<br/><br/>

## Usage / Configuration

This section is currently TBD

- Starting manually: anytime, but preferably before MSFS or in the Main Menu. The utility will stop itself when MSFS closes. 
- Closing the Window does not close the utiltiy, use the Context Menu of the SysTray Icon.
- Clicking on the SysTray Icon opens the Window (again).
- Runnning as Admin NOT required (BUT: It is required to be run under the same User/Elevation as MSFS).
- Connection Status
  - Red values indicate not connected, green is connected.
- Sim Values
  - Will not show valid values unless all three connections are green. n/a means not available right now.
  - Green means the sim value is at or better than target value being sought, red means at lowest level or worse than target value being sought, orange means TLOD is auto adjusting, black is shown otherwise.
  - Priority will show whether FPS or TLOD Min are the current automation priority, with the latter only being shown if the TLOD min for ground/landing is enabled and conditions are such that working towards TLOD Min because of your flight phase (on or near the ground) now has priority over maintaining FPS. 
- General
  - Target FPS - The most important setting in this app. Set it to what FPS you want the app to target while running, noting that this value should be at the mid to lower end of what your system is capable of otherwise the app will be unlikely to achieve it. There is a setting for each graphics mode (VR, PC and FG) which you can only change while in that mode and on the ground or in a flight.
  - Use Expert Options - When disabled allows the app to use default settings in conjuction with your chosen target FPS that should produce good automated FPS tracking, provided you have set a realisting FPS target in the first place. When enabled, the UI expands to show additional MSFS settings to adjust. If you do not understand these settings and their impact on MSFS performance and graphics quality, it is strongly recommended that you do not use these expert options and you should uncheck this option.
- MSFS Settings
  -  FPS Tolerance - Determines how much variance from your target FPS must occur before the app will adjust MSFS settings to achieve the target FPS and what nominal magnitude those changes will be. The lower the setting, the more reactive the app will be, the more MSFS settings changes will occur and the changes will be smaller. Vice versa for higher settings. The default value of 5% should provide the most balanced experience.                                      
- **Less is more**:
  - Fewer Increments/Decrements are better of reasonable Step-Size (roughly in the Range of 25-75) or use Step LOD Max to spread LOD changes out over time.
  - Don't overdo it with extreme low or high xLOD Values. A xLOD of 100 is reasonable fine on Ground, 200-ish is reasonable fine in the air. 400 if you have a super computer.
  - Tune your AGL/LOD Pairs to the desired Performance (which is more than just FPS).
  - FPS Adaption is just *one temporary* Adjustment on the current AGL/xLOD Pair to fight some special/rare Situations.
  - Forcing the Sim to (un)load Objects in rapid Succession defeats the Goal to reduce Stutters. It is *not* about FPS.
  - Smooth Transitions lead to smoother experiences.  
<br/><br/>
