# MSFS2020_AutoFPS

Based on muumimorko's idea and code in MSFS_AdaptiveLOD, as further developed by Fragtality in DynamicLOD and myself in DynamicLOD_ResetEdition.<br/><br/>

This utility is a new development that is a simplification of, and a slightly different concept to, DynamicLOD_ResetEdition. It aims to improve MSFS performance and smoothness by automatically changing key MSFS settings that impact MSFS performance and smoothness the most. It has an easy to use GUI and provides features such as:<br/>
- Automatically adjusts TLOD to achieve a user-defined target FPS band based on pre or user-defined maximum and minimum TLODs,<br/>
- TLOD minimum on ground/landing option, which prioritises TLOD over FPS during these flight phases and also averts exacerbating existing texture tearing issues with DX12,</br>
- Cloud quality decrease option for when FPS can't be achieved at the lowest desired TLOD,<br/>
- Automatic pause when MSFS loses focus option, particularly useful if using FG due to varying FPS when MSFS gains or loses focus,</br>
- Automatic FPS settling timer on MSFS graphics mode and focus changes to allow FPS to stabilise before being acted upon,</br>
- Simultaneous PC, FG and VR mode compatibilty including correct FG FPS display and separate FPS targets for each mode,<br>
- A greatly simplified GUI option that uses pre-defined settings for an automated experience suited to most user scenarios, and</br>
- Auto restoration of original settings changed by the utility.<br/><br/>

**Really, really important:**<br>
- Do not even mention, let alone try to discuss, this app on the MSFS official forums, even in a personal messages, as they have taken the view that this app modifies licenced data, regardless of how harmless the way in which the app does it, and is therefore a violation of their Terms of Service and Code of Conduct for that website. If you do so, your post/personal message will be flagged by moderators and you may get banned from the MSFS official forums. You have been warned! 

Important:<br/> 
- This utility directly accesses active MSFS memory locations while MSFS is running to read and set TLOD and cloud quality settings on the fly at a maximum rate of one read and, if required, change per setting per second. The utility will first verify that the MSFS memory locations being used are still valid and if not, likely because of an MSFS version change, will attempt to find where they have been relocated. If it does find the new memory locations and they pass validation tests, the utility will update itself automatically and will function as normal. If it can't find or validate MSFS memory locations at any time when starting up, the utility will self-restrict to read only mode to prevent the utility making changes to unknown MSFS memory locations.<br/>
- As such, I believe the app to be robust in its interaction with validated MSFS memory locations and to be responsible in disabling itself if it can't guarantee that. Nonetheless, this utility is offered as is and no responsibility will be taken for unintended negative side effects. Use at your own risk!<br/><br/>

Which app should I use? DynamicLOD_ResetEdition or MSFS2020_AutoFPS?:
- Essentially both apps are intended to give you better overall performance but with different priorities to achieve it that result in a slightly different experience.  They both allow a lower TLOD down low and on the ground, when your viewing distance reduced anyway so the visual impact is minimal, and a higher TLOD when at higher altitude and not in close proximity to complex scenery or traffic. They also adjust OLOD and Cloud Quality but TLOD is usually the most important determiner of performance at these two extremes.
- Where they differ is that DynamicLOD provides user set tables for LOD changes at specific altitudes, giving the user precise control over when and where these changes take place such that they can optimise them to their particular flight activity they normally do, and can set a specific profile for each one. The price of such precise control is that the user must be intimately familiar with LODs to be able to tune a variety of settings in the app for the best outcome and this can be a bit daunting for more casual and non-technical users.
- Alternatively, AutoFPS seeks to automate these changes as much as possible based on a target FPS and a minimum and maximum LOD range within which to automatically adjust. This results in a much simpler and generally similarly acceptable user experience compared to DynamicLOD. Nonetheless, the automation algorithm does require FPS headroom to function correctly, so can conflict in cases where an FPS cap is being used, such as with Vsync or motion reprojection in VR. Additionally, AutoFPS tends to make constant small changes to TLOD, much more than DynamicLOD does, and this can induce stuttering on older hardware as it struggles to manage even small scenery changes. In these cases, the user would be better off using DynamicLOD in a more manually tuned approach.
- Both apps can be installed concurrently, but only one can be running at a time.

Frame Generation (FG) users: 
- The app does detect correct FG FPS when FG is enabled in MSFS, however FG is only active when MSFS is the focused window and becomes inactive when not, through your graphics driver not this app.
- To see correct FG FPS, use the app's "On Top" option to overlay this app over MSFS and give MSFS the focus.
- If FG is being incorrectly reported as enabled by the app, the likely reason is that the FG mod had been installed and removed and the now the now greyed out MSFS FG setting is still set to on. To fix, change the DLSSG line in your UserCfg.opt file to be DLSSG 0.

Dangerous/Unsafe program warnings:
- This utility is unsigned because I am a hobbyist and the cost of obtaining certification is prohibitive to me, so you may get a warning message of a potentially dangerous app when you download it in a web browser like Chrome or from your antivirus program, including Windows Defender.
- You can either trust this download, based on feedback you can easily find on Avsim and Youtube, make an exception in your browser and/or antivirus program for the download then run a virus scan and malware scan before you install just be sure, or just not install and use this app.<br/><br/>

## Requirements

The Installer will install the following Software:
- .NET 7 Desktop Runtime (x64)
- MobiFlight Event/WASM Module

<br/>

[Download here](https://github.com/ResetXPDR/MSFS2020_AutoFPS/releases/latest)

(Under Assests, the MSFS2020_AutoFPS-Installer-vXYZ.exe File)

<br/><br/>

## Installation / Update / Uninstall
Basically: Just run the Installer to do all three of these.<br/>

Some Notes:
- MSFS2020_AutoFPS has to be stopped before installing.
- If the MobiFlight Module is not installed or outdated, MSFS also has to be stopped.
- If you have duplicate MobiFlight Modules installed, in either your official or community folders, the utility may display 0 value Sim Values and otherwise not function. Remove the duplicate versions, rerun the utility installer and it should now work.
- Do not run the Installer as Admin!
- If the installer will not run at all, Windows SmartScreen is potentially blocking it because the app is so new. The solution to try is:
  - Right-click on the Installer and select properties
  - Check the option "Unblock"
  - Click on Apply and Ok to save the change
  - Then try to install it again
- If you wish to retain your settings for an update version, do NOT uninstall first, as that deletes all app files, including the config file. Just run the installer, select update and your settings will be retained.
- For Auto-Start either your FSUIPC7.ini or EXE.xml (MSFS) is modified. The Installer does not create a Backup.
- The utility may be blocked by Windows Security or your AV-Scanner, try if unblocking and/or setting an Exception helps (for the whole Folder)
- The Installation-Location is fixed to %appdata%\MSFS2020_AutoFPS (your Users AppData\Roaming Folder) and can't be changed.
  - Binary in %appdata%\MSFS2020_AutoFPS\bin
  - Logs in %appdata%\MSFS2020_AutoFPS\log
  - Config: %appdata%\MSFS2020_AutoFPS\MSFS2020_AutoFPS.config
- If after installing and running the app your simconnect always stays red, try downloading and installing a Microsoft official version of “Microsoft Visual C++ 2015 - 2022 Redistributable”, which may be missing from your Windows installation.
- If you get an "MSFS compatibility test failed - Read Only mode" message there are three possible causes:
  - There is an issue with permissions and you may need to run the app as Administrator. This is by far the most likely cause and resolution.
  - You may have changed MSFS settings in your usercfg.opt file beyond what is possible to set in the MSFS settings menu. To rectify, go into MSFS settings at the main menu and reset to default (F12) the graphics settings for both PC and VR mode, then make all changes to MSFS within the MSFS settings menu.
  - A new version of MSFS has come out that has a different memory map to what the app expects, which has happened only once since MSFS 2020 was released, and the app can't auto adjust to the new memory location for MSFS settings. If so, I will likely be already aware of it and working on a solution, but if you may be one of the first to encounter it (eg. on an MSFS beta) then please do let me know.
- If you get an exception 'System.IO.DirectoryNotFoundException' during AutoStartExe when trying to install with the autostart option for MSFS, it usually means that your MSFS installation is missing the required EXE.xml file in which to place the autostart entry. To resolve, you need to  go to your MSFS root user directory (MS Store Version: "C:\Users\YOUR_USERNAME\AppData\Local\Packages\Microsoft. FlightSimulator_8wekyb3d8bbwe\LocalCache\ or Steam Version: "C:\Users\YOUR_USERNAME\AppData\Roaming\Microsoft Flight Simulator\") and manually create an EXE.xml file and save it there. You can use the following EXE.xml template, inserting your Windows username where shown:
```
<?xml version="1.0" encoding="Windows-1252"?>
<SimBase.Document Type="Launch" version="1,0">
  <Descr>Launch</Descr>
  <Filename>EXE.xml</Filename>
  <Disabled>False</Disabled>
  <Launch.ManualLoad>False</Launch.ManualLoad>
  <Launch.Addon>
    <Disabled>False</Disabled>
    <ManualLoad>False</ManualLoad>
    <Name>MSFS2020_AutoFPS</Name>
    <Path>C:\Users\<username>\AppData\Roaming\MSFS2020_AutoFPS\bin\MSFS2020_AutoFPS.exe</Path>
  </Launch.Addon>
</SimBase.Document>
```
- To uninstall, run the installer and select remove on the first window. This will remove all traces of the app, including the desktop icon, MSFS or FSUIPC autostart entries if you used them, and the entire app folder, including your configuration file.

<br/><br/>

## Usage / Configuration

- General
  - Starting manually: anytime, but preferably before MSFS or in the Main Menu. The utility will stop itself when MSFS closes. 
  - Closing the Window does not close the utility, use the Context Menu of the SysTray Icon.
  - Clicking on the SysTray Icon opens the Window (again).
  - Runnning as Admin NOT required (BUT: It is required to be run under the same User/Elevation as MSFS).
  - Do not change TLOD, OLOD and Cloud Quality MSFS settings manually while in a flight with this app running as it will conflict with what the app is managing and they will not restore to what you set when you exit your flight. If you wish to change the defaults for these MSFS settings, you must do so either without this app running or, if it is, only while you are in the MSFS main menu (ie not in a flight). 
- Connection Status
  - Red values indicate not connected, green is connected.
- Sim Values
  - Will not show valid values unless all three connections are green. n/a means not available right now.
  - Green means the sim value is at or better than target value being sought, red means at lowest level or worse than target value being sought, orange means TLOD is auto adjusting, black is shown otherwise.
  - FPS shows the FPS for the current graphics mode averaged over 5 seconds which will smooth out any transient FPS spikes experienced when panning or loading new scenery or objects so that automated MSFS setting changes are minimised.
  - Priority will show whether FPS or TLOD Min are the current automation priority, with the latter only being shown if the TLOD min for ground/landing is enabled and conditions are such that, because of your flight phase (on or near the ground), working towards TLOD Min now has priority over maintaining FPS. 
- General
  - Target FPS - The most important setting in this app.
    - Set it to what FPS you want the app to target while running, noting that this value should be at the mid to lower end of what your system is capable of otherwise the app will be unlikely to achieve it.
    - There is a setting for each graphics mode (PC, FG and VR) which you can only change while in that mode and on the ground or in a flight. This is particularly useful if regularly switching between FG mode and VR mode in your flights as the FG FPS target can be significantly higher than the one for VR. If using FG, the target FPS you set is your desired FG Active FPS, not the FG Inactive FPS you see when this app has the focus instead of MSFS. 
    - If you use an FPS cap, or Vsync for the same purpose, you will need to set your target FPS to be around 10% lower than that cap. This allows the automated TLOD increase logic to function properly because it needs FPS to get above the target FPS plus the FPS tolerance to activate an increase in TLOD. If doing so causes unacceptable tearing of the image on your monitor, or breaks motion reprojection if you use it with VR, then this app isn't suitable for you.
  - Use Expert Options - When disabled allows the app to use default settings in conjuction with your chosen target FPS that should produce good automated FPS tracking, provided you have set a realistic FPS target within your system's performance capability. When enabled, the UI expands to show additional MSFS settings to adjust. If you do not understand these settings and their impact on MSFS performance and graphics quality, it is strongly recommended that you do not use these expert options and you should uncheck this option. When Use Expert Setting is unchecked, the following internal settings are used by the app:
    - FPS Tolerance - 5%
    - TLOD Minimum on Ground/Landing - enabled
    - Alt TLOD Base (coming up in the next version) - 1000 ft
    - Avg Descent Rate (coming up in the next version)  - 2000 fpm
    - TLOD Minimum - 50% of your current MSFS TLOD setting
    - TLOD Maximum - 200% of your current MSFS TLOD setting
    - Decrease Cloud Quality - enabled
    - Cloud Recovery TLOD - 2/5 between TLOD Minimum and TLOD Maximum
    - Auto OLOD (coming up in the next version) - disabled
    - Pause when MSFS loses focus - enabled in current version but will be disabled in the next version
  - On Top (v0.4.1) - allows the app to overlay your MSFS session if desired, with MSFS having the focus. This is mainly useful for adjusting settings and seeing the outcome over the top of your flight as it progresses. It should also satisfy single monitor users utilising the FG capability of MSFS as they now see the true FG FPS the app is reading when MSFS has the focus.
  - Open window on app start - determines the app window's startup state.
  - Status Message - On app startup indicates key system messages, such as:
    - Before loading a flight - whether a newer version of the app is available to download and install
    - Loading in to a flight  - whether MSFS memory integrity test have failed, and
    - Flight is loaded - showing detected DX version, Graphics Mode (PC, FG, or VR), and app pause or FPS settling time status as applicable. The FPS settling timer runs for 6 seconds to allow FPS to settle between pausing/unpausing and VR/PC/FG mode transitions. This allows the FPS to stabilise before engaging automatic functions and should lead to much smaller TLOD changes when seeking the target FPS on such transitions.
- MSFS Settings
  -  FPS Tolerance - Determines how much variance from your target FPS must occur before the app will adjust MSFS settings to achieve the target FPS and what nominal magnitude those changes will be. The lower the setting, the more reactive the app will be, the more MSFS settings changes will occur and the changes will be smaller. Vice versa for higher settings. When expert settings are disabled, the default value of 5% should provide the most balanced experience.
  -  TLOD Min on Ground/Landing (v0.4.0) - When enabled, your TLOD will immediately change to TLOD minimum when your flight is loaded and will stay that way any time you are on the ground. Once you take off and start climbing, auto TLOD will kick in gradually on a sliding scale up to 1000 ft AGL, then normal range auto operation above that. On descent to landing, when you cross 2000 ft AGL, the app will progressively adjust your TLOD down so that as you cross 1000 ft AGL it will be at TLOD minimum and there it will stay locked all the way to the ground, taxi and shutdown at the gate. If you are level below 1000 ft AGL, then commence descending, TLOD will rapidly switch to TLOD minimum with no stepping as it thinks you are landing imminently. If you are using cloud quality reduction options, even though you are at TLOD minimum on the ground which would normally cause cloud quality to reduce straight away, it will only reduce if you are below achieving your FPS target FPS and recover again as normal. It is generally recommended to leave this setting enabled, as will happen with expert settings disabled, if you want your TLOD on the ground or when landing to always be TLOD min, otherwise with it disabled you may experience texture corruption with DX12 on the ground, especially at airports.
  -  TLOD Min on Ground/Landing option (v0.4.1)
     -  Enabled when either Use Expert Settings is not checked, ie by default, or TLOD Min on Ground/Landing is checked.
     -  On the ground your TLOD will immediately change to TLOD minimum when your flight is loaded and will stay that way until you are in the air. 
     -  Once you take off and start climbing, your TLOD will remain unchanged until you reach the Alt TLOD Base, which defaults to 1000 ft but is user changeable under Expert Settings. The app will calculate an Alt TLOD Top, based on an Average Decent Rate, which defaults to 2000 fpm and again is user adjustable, at which TLOD will be allowed to go up to TLOD Max, should conditions warrant an auto adjustment to that level. In between these two altitudes, the app choses the lowest of either an  interpolated TLOD from this altitude band or a TLOD that allows the target FPS to be maintained.
     -  Once over the calculated Alt TLOD top, FPS priority will take over, using TLOD Min to TLOD Max as the working TLOD range.
     -  It is generally recommended to leave this setting enabled, as will happen with expert settings disabled, if you want your TLOD on the ground or when landing to always be TLOD min, otherwise with it disabled you may experience texture corruption with DX12 on the ground, especially at airports.
  -  TLOD Min - Sets the minimum TLOD the automation algorithm will use. When expert settings are disabled, the app will use 50% of your existing MSFS TLOD setting for this parameter.
  -  TLOD Max - Sets the maximum TLOD the automation algorithm will use. When expert settings are disabled, the app will use 200% of your existing MSFS TLOD setting for this parameter.
  -  Decrease Cloud Quality - When enabled, will reduce cloud quality by one level if TLOD has already auto reduced to TLOD Min and FPS is still below target FPS by more than the FPS tolerance. When expert settings are disabled, the app will enable this setting.
  -  Cloud Recovery TLOD - The TLOD level required to cancel an active cloud quality reduction state and restore cloud quality back to its initial higher quality level. This provides a TLOD buffer to account for the increased TLOD achieved by reducing cloud quality to stop FPS adaption constantly toggling on and off. This parameter is ideally set to 50 TLOD above TLOD Min provided that the aforementioned conditions can be met. When expert settings are disabled, the app will use your existing MSFS TLOD setting for this parameter.
  -  Auto OLOD (v0.4.1)
     -  This option is disabled by default. When enabled, four user definable parameters relating to this feature will be revealed on the UI.
     -  Rather than the automation being FPS based, which would cause contention with TLOD changes at the same time, OLOD will adjust based on an altitude band with a base and top level and with OLOD values defined for each of these altitudes.
     -  The app will set OLOD @ Base at or below the Alt OLOD Base, set the OLOD @ Top at or above Alt OLOD Top and interpolate in between. Note that OLOD @ Base can be higher, lower or the same value as the OLOD @ Top, depending on whether you want OLOD to decrease, increase or stay the same respectively as you ascend. 
  -  Pause when MSFS loses focus - This will stop TLOD and, if applicable, cloud quality from changing while you are focused on another app and not MSFS. It is particularly useful for when using FG as the FG active and inactive frame rate can vary quite considerably and because FG is not always an exact doubling of non-FG FPS. In v0.4.0, when expert settings are disabled, the app will enable this setting. In v0.4.1, when expert settings are disabled, the app will disable this setting.
<br/><br/>
