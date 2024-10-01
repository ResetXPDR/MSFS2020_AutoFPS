# MSFS2020_AutoFPS

## Notice
Following MSFS2020_AutoFPS version 0.4.3.0 formal release, my development efforts on this app will be limited to maintenance of existing functionalty only until the release of MSFS 2024 in Nov 24. When MSFS 2024 is released I hope that much of the functionality of this app becomes native to MSFS and hence this app will no longer be required. If this is not largely the case, I will attempt to update this app to work with MSFS 2024, but since MSFS 2024 is currently an unknown entity, I make no promises that this will be successful or even possible.

## Summary
Based on muumimorko's idea and code in MSFS_AdaptiveLOD, as further developed by Fragtality in DynamicLOD and myself in DynamicLOD_ResetEdition.<br/><br/>

This app aims to improve the MSFS user experience by automatically changing key MSFS settings that impact MSFS performance and smoothness the most. It has an easy to use UI and provides features such as:<br/>
- Automatic TLOD adjustment when in the air to either achieve and maintain a target FPS or to an altitude schedule, the latter as an expert option,
- Improved target FPS tracking for all modes by having much smaller TLOD changes the closer you are to your target FPS, giving more consistent FPS for a better flight experience.    
- A choice between VFR (GA) and IFR (Airliner) flight types, which defaults to settings suitable to each flight type and is fully customisable in Expert mode. 
- Auto raising and lowering of the minimum or base TLOD option, depending on low altitude performance being either very favourable or poor respectively,
- Auto lowering of the maximum or top TLOD at night option, reducing system workload by not having to draw distant scenery that can't be seen in the dark anyway,
- Auto target FPS option, which is useful if you don't know what target FPS to choose or if your flight types are so varied that a single target FPS value is not always appropriate,
- Cloud quality decrease option for when either FPS can't be achieved at the lowest desired TLOD or when the GPU load is too high,
- Automatic OLOD adjustment option based on an automatic or user-definable OLOD range and altitude band (AGL),
- Simultaneous PC, FG (native nVidia, FG mod and Lossless Scaling) and VR mode compatibilty, including correct FG FPS display, and separate FPS targets for each mode,
- A greatly simplified non-expert default UI option that uses pre-defined settings for an automated experience suited to most user scenarios,
- Auto detection and protection from known similar apps already running or incompatibilities with newer MSFS versions, and
- Auto restoration of original MSFS settings changed by the app, recently enhanced to withstand MSFS CTDs.<br><br>

**Really, really important:**
- Do not even mention, let alone try to discuss, this app on the MSFS official forums, even in personal messages, as they have taken the view that this app modifies licenced data, regardless of how harmless the way in which the app does it, and is therefore a violation of their Terms of Service and Code of Conduct for that website. If you do so, your post/personal message will be flagged by moderators and you may get banned from the MSFS official forums. You have been warned!
- Notwithstanding, there is a new MSFS wishlist item requesting simconnect variables access to MSFS settings, which would allow me to make this app legitimate in MS/Abobo's eyes and expand the range of possibilities of what this app could do in future. Please vote for it [here](https://forums.flightsimulator.com/t/expose-tlod-olod-clouds-etc-via-simconnect-l-vars/634075). 

Important:<br/> 
- This app directly accesses active MSFS memory locations while MSFS is running to read and set TLOD, OLOD and cloud quality settings on the fly at a maximum rate of one read and change per setting per second, normally much less. The app will first verify that the MSFS memory locations being used are still valid and if not, likely because of an MSFS version change, will attempt to find where they have been relocated. If it does find the new memory locations and they pass validation tests, the app will update itself automatically and will function as normal. If it can't find or validate MSFS memory locations at any time when starting up, the app will self-restrict to read only mode to prevent the app making changes to unknown MSFS memory locations.
- As such, I believe the app to be robust in its interaction with validated MSFS memory locations and to be responsible in disabling itself if it can't guarantee that. Nonetheless, this app is offered as is and no responsibility will be taken for unintended negative side effects. Use at your own risk!<br/><br/>


## FAQ

I am new to this app/MSFS, or I don't care for all this technical jargon. What is the simplest way to use this app to make my MSFS experience better?
- Start the app before you load your flight,
- Leave Use Expert Settings unchecked,
- Pick what type of flight you are doing via the radio buttons ie. either VFR (GA aircraft) or IFR (airliners),
- Enter a realistic target FPS (or click on auto target FPS for the app to pick it for you),
- Click back on MSFS and wait until any FPS settle or TLOD seek events have finished (60 seconds max), then
- Go fly!

I am getting major stuttering, freezes or CTDs in MSFS using this app. What can I do to stop them?
- By far the most common reason is users have enabled expert settings and have modified the default settings to be way beyond what their system is capable of, even without running the app.
- As such, the first step to resolve is to restore the app's default settings, which you can do by using the installer to uninstall (remove option) and reinstall, which will recreate your config file.
- Rerun the app and try non-expert mode with IFR flight type and Auto Target FPS checked.
- If this doesn't resolve it, try enabling expert options and reducing the FPS Sensitivity setting to 2, to allow smaller TLOD changes.
- If still not resolved, try the FPS Tolerance mode, which was the automation method in the original release version that had larger TLOD changes but they occurred less often, with a setting of 5.
- Finally, if still not resolved, raise an issue here on github and I will do my best to help you, provided you have completed all of the aforementioned steps first.

My default MSFS TLOD, OLOD and/or cloud settings are messed up and each time I try to change them back they get messed up again. How do I fix this?
- You are likely trying to change these default MSFS settings while the app is still running and you are in an active flight, where the app will override any such changes you try to make.
- Either exit the app completely from the System Tray or be in the MSFS main menu (ie. NOT in a flight), then you can go to the MSFS settings screen and change your default MSFS settings to what you want and the app will restore these upon exiting.

Which app should I use? DynamicLOD_ResetEdition or MSFS2020_AutoFPS?:
- Essentially both apps are intended to give you better overall performance but with different priorities to achieve it that result in a slightly different experience.  They both allow a lower TLOD down low and on the ground, when your viewing distance reduced anyway so the visual impact is minimal, and a higher TLOD when at higher altitude and not in close proximity to complex scenery or traffic. They also adjust OLOD and Cloud Quality but TLOD is usually the most important determiner of performance at these two extremes.
- Where they differ is that DynamicLOD provides user set tables for LOD changes at specific altitudes, giving the user precise control over when and where these changes take place such that they can optimise them to their particular flight activity they normally do, and can set a specific profile for each one. The price of such precise control is that the user must be intimately familiar with LODs to be able to tune a variety of settings in the app for the best outcome and this can be a bit daunting for more casual and non-technical users.
- Alternatively, AutoFPS seeks to automate these changes as much as possible based on a target FPS and a minimum and maximum LOD range within which to automatically adjust. This results in a much simpler and generally similarly acceptable user experience compared to DynamicLOD. Nonetheless, the automation algorithm does require FPS headroom to function correctly, so can conflict in cases where an FPS cap is being used, such as with Vsync or motion reprojection in VR, however the new Auto TLOD automation method now available in AutoFPS is FPS-independent and works well in such instances. Additionally, AutoFPS tends to make constant small changes to TLOD, much more than DynamicLOD does, and this can induce stuttering on older hardware as it struggles to manage even small scenery changes. In these cases, the user would be better off using DynamicLOD in a more manually tuned approach.
- Both apps can be installed concurrently, but only one can be running at a time.

How does this app work for Frame Generation (FG) users?
- The app does detect correct FG FPS when FG (native nVidia or FG mod) is enabled in MSFS, however FG is only active when MSFS is the focused window and becomes inactive when not, through your graphics driver not this app.
- To see correct MSFS FG FPS, use the app's "On Top" option to overlay this app over MSFS and give MSFS the focus.
- If MSFS FG is being incorrectly reported as enabled by the app, the likely reason is that either the FG mod had been installed and removed or you have disabled Hardware Accelerated Graphics Scheduling under Windows settings and the now the now greyed out MSFS FG setting may show that it is off but it is still set to on internally to MSFS. To fix, change the DLSSG line in your UserCfg.opt file to be DLSSG 0.
- Lossless Scaling (LS) FG, including the scaling muliplier used, is also detected and the correct LSFG multiplied FPS is displayed. The app will first try to use an LS profile with the specific name MSFS2020 to obtain these settings. If an MSFS2020 profile does not exist then the settings in the Default profile will be used.
- Detection of FG is normally only performed upon starting a flight. If FG is enabled or LS is started after this detection is normally performed, press the Reset button for it to be detected.

Why am I getting a dangerous/Unsafe program warning when trying to download or install?
- This app is unsigned because I am a hobbyist and the cost of obtaining certification is prohibitive to me, so you may get a warning message of a potentially dangerous app when you download it in a web browser like Chrome or from your antivirus program, including Windows Defender.
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
Basically: Just run the Installer to either install, update or uninstall the app.<br/>

Some Notes:
- Your current MSFS2020_AutoFPS has to be exited before updating.
- If the MobiFlight Module is not installed or outdated, MSFS also has to be stopped.
- If you have duplicate MobiFlight Modules installed, in either your official or community folders, the app may display 0 value Sim Values and otherwise not function. Remove the duplicate versions, rerun the app installer and it should now work.
- Do not run the Installer as Admin unless it will not install otherwise due to a permissions issue!
- If the installer will not run at all, Windows SmartScreen is potentially blocking it because the app is so new. The solution to try is:
  - Right-click on the Installer and select properties
  - Check the option "Unblock"
  - Click on Apply and Ok to save the change
  - Then try to install it again
- If you wish to retain your settings for an update version, do NOT uninstall first, as that deletes all app files, including the config file. Just run the installer, select update and your settings will be retained.
- For Auto-Start either your FSUIPC7.ini or EXE.xml (MSFS) is modified. The Installer does not create a Backup.
- If you wish to remove an Auto-Start option from a previous installation, rerun the installer and select Remove Auto-Start and the click Update.
- The app may be blocked by Windows Security or your AV-Scanner, if so try to unblock or set an exception (for the whole Folder)
- The Installation-Location is fixed to %appdata%\MSFS2020_AutoFPS (your Users AppData\Roaming Folder) and can't be changed.
  - Binary in %appdata%\MSFS2020_AutoFPS\bin
  - Logs in %appdata%\MSFS2020_AutoFPS\log
  - Config: %appdata%\MSFS2020_AutoFPS\MSFS2020_AutoFPS.config
- If after installing and running the app your simconnect always stays red, try downloading and installing a Microsoft official version of “Microsoft Visual C++ 2015 - 2022 Redistributable”, which may be missing from your Windows installation.
- If you get an "MSFS compatibility test failed - app disabled." message there are three possible causes:
  - There is an issue with permissions and you may need to run the app as Administrator. This is by far the most likely cause and resolution.
  - You may have changed MSFS settings in your usercfg.opt file beyond what is possible to set in the MSFS settings menu. To rectify, go into MSFS settings at the main menu and reset to default (F12) the graphics settings for both PC and VR mode, then make all changes to MSFS within the MSFS settings menu.
  - A new version of MSFS has come out that has a different memory map to what the app expects, which has happened only once since MSFS 2020 was released, and the app can't auto adjust to the new memory location for MSFS settings. If so, I will likely be already aware of it and working on a solution, but if you may be one of the first to encounter it (eg. on an MSFS beta) then please do let me know.
- If you get a message when starting the app that you need to install .NET desktop runtime, manually download it from [here](https://dotnet.microsoft.com/en-us/download/dotnet/7.0).
- If you get an error message saying "Required EXE.xml file not found for AutoStartExe" when trying to install with the autostart option for MSFS, it usually means that your MSFS installation is missing the required EXE.xml file in which to place the autostart entry. To resolve, you need to  go to your MSFS root user directory (MS Store Version: "C:\Users\YOUR_USERNAME\AppData\Local\Packages\Microsoft. FlightSimulator_8wekyb3d8bbwe\LocalCache\ or Steam Version: "C:\Users\YOUR_USERNAME\AppData\Roaming\Microsoft Flight Simulator\") and manually create an EXE.xml file and save it there. You can use the following EXE.xml template, inserting your Windows username where shown:
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
- To uninstall, ensure you have completely exited the app (ie. it is not hiding still running in your SysTray), run the installer and select remove on the first window. This will remove all traces of the app, including the desktop icon, MSFS or FSUIPC autostart entries if you used them, and the entire app folder, including your configuration file.

<br/><br/>

## Usage / Configuration

- General
  - The app can be started anytime, but preferably before MSFS or in the Main Menu to minimise sudden MSFS settings changes when the app is initialising. The app will exit itself when MSFS closes. 
  - Closing the Window does not close the app, use the Context Menu of the SysTray Icon.
  - Clicking on the SysTray Icon opens the Window (again).
  - If you wish to have the app window always open to the SysTray, close the app and manually change the openWindow key state in the config file to false.
  - The app's window position will be remembered between sessions, except movements to it made while in VR due to window restoration issues. If there are issues with the window not displaying correctly on startup, as can happen when auto-starting the app through MSFS of FSUIPC, either don't use auto-start, restart the app within 10 seconds of last closing it to auto reset the window position, or manually permanently disable this feature in the config file by setting the RememberWindowPos line to be false.
  - The user can progressively hide parts of the UI when the app window is double clicked anywhere that is not a control. The first double click will hide the expert settings section (if applicable), the second will hide the general settings section and a third double click will restore all hidden settings sections. The last state in use will be restored when next starting the app. 
  - Runnning as Admin NOT usually required (BUT: It is required to be run under the same User/Elevation as MSFS).
  - Do not change TLOD, OLOD and Cloud Quality MSFS settings manually while in a flight with this app running as it will conflict with what the app is managing and they will not restore to what you set when you exit your flight. If you wish to change the defaults for these MSFS settings, you must do so either without this app running or, if it is, only while you are in the MSFS main menu (ie not in a flight).
  - If you wish to activate additional logging of settings changes and sim values, as currently happens automatically in test versions, you need to manually edit your config file and add a LogSimValues key, if it doesn't already exist, and set its value to true ie.  ```<add key="LogSimValues" value="true" />```
- Connection Status
  - Red values indicate not connected, green is connected.
- Sim Values
  - Will not show valid values unless all three connections are green. n/a means not available right now.
  - Green means the sim value is at or better than target value being sought, red means at lowest level or worse than target value being sought, orange means TLOD or OLOD is auto adjusting, black is shown otherwise.
  - FPS shows the FPS for the current graphics mode averaged over 5 seconds which will smooth out any transient FPS spikes experienced when panning or loading new scenery or objects so that automated MSFS setting changes are minimised.
- General
  - Status Message - Displays key system messages, such as:
    - Before loading a flight - whether a newer version of the app is available to download and install,
    - Loading in to a flight  - whether MSFS memory integrity test have failed, and
    - Flight is loaded
      - Shows detected Graphics Mode (PC, FG, LSFG or VR) and DX version, app pause, FPS settle, TLOD+ seek, Mtn+, app priority mode and/or TLOD range as applicable.
      - The FPS settle timer runs for up to 20 seconds to allow FPS to settle between pausing/unpausing, auto target FPS calibration, TLOD Min + transitions and VR/PC/FG/LSFG mode transitions. This allows the FPS to stabilise before engaging automatic functions and should lead to much smaller TLOD changes when seeking the target FPS on such transitions.
      - App priority shows whether FPS or TLOD are the current automation priority. A + next to TLOD indicates that TLOD Min + has been activated and that a higher TLOD Min should be expected. Similarly, a + next to ATLOD indicates that TLOD Base + has been activated and that a higher TLOD offset across the entire altitude schedule should be expected. 
      - Bonus GPU load display if the optional GPU-Z companion app, downloadable separately [here](https://www.techpowerup.com/download/techpowerup-gpu-z/), is installed and detected running when starting any flight session. Note, the GPU-Z companion app is required to be running if the Decrease Cloud Quality option is selected in conjunction with the GPU Load activation method, as GPU-Z provides the necessary GPU load information to the app for this method to function.
      - Auto pause will activate if in flight and either MSFS is in active pause or the MSFS settings menu is being accessed.
  - Target FPS - The most important setting in this app. (10 - 200 allowable)
    - Set it to what FPS you want the app to target while running, noting that this value should be at the mid to lower end of what your system is capable of otherwise the app will be unlikely to achieve it.
    - There is a setting for each graphics mode (PC, FG, LSFG and VR) and each flight mode (VFR and IFR), which you can only change while in those mode pairs. This is particularly useful if regularly switching between FG mode and VR mode in your flights as the FG FPS target can be significantly higher than the one for VR.
    - If using MSFS FG, the target FPS you set is your desired FG Active FPS, not the FG Inactive FPS you see when this app has the focus instead of MSFS. 
    - If using an FPS cap, or Vsync for the same purpose, it is strongly recommended you use the Auto TLOD automation method, available under expert settings, which can be used either FPS-independently or, when TLOD Base + is used, with an FPS target matching your FPS cap and works well in such instances.
    - If using such an FPS cap with either FPS Sensitivity or Tolerance automation methods you will need to set your target FPS to be a few FPS lower than that cap to allow the automation logic to function correctly. This potentially introduces screen tearing, or breaks motion reprojection in VR, hence why Auto TLOD is preferred.
  - Auto Target FPS
    - Cannot be enabled at the same time as TLOD Min + due to automation control ambiguity. Selecting both will result in the most recent selection being enabled and the other disabled, with a dialog box to advise this.
    - When checked, a target FPS will automatically be calculated, following any initial FPS settling, when stationary on the ground or any time you are in the air.
    - Automatically recalulated if performance conditions are too low for the calculated target FPS, on the ground after arriving at a new destination, if you change graphics mode or if you uncheck then check the option again for a quick recalibration.
    - With IFR it will range from 95% of your current average FPS on the ground to 85% at or above 3000 ft, the latter being lower to give head room for Max TLOD.
    - With VFR it will be 5% less than each of the IFR percentages respectively to better suit the greater performance expectation with VFR flights.
  - On Top
    - Allows the app to overlay your MSFS session if desired, with MSFS having the focus.
    - Mainly useful for adjusting settings and seeing the outcome over the top of your flight as it progresses.
    - Should also satisfy single monitor users utilising the FG capability of MSFS as they now see the true FG FPS the app is reading when MSFS has the focus.
  - Reset button
    - Resets TLOD, Clouds, Auto Target FPS and graphics mode detection to initial state.
    - Useful to reintialise and recommence the seek process for TLOD Min/Top + should conditions change significantly from what they were on initial startup.
    - Can be activated by pressing ALT-R while the app has the focus, making it suitable to be assigned as a VR-friendly voice command with an app like VoiceAttack.
  - Flight type - VFR or IFR
    - In non-expert mode, VFR will use higher minimum and maximum TLODs and a lower TLOD base altitude than IFR to account for the greater performance expectation that GA flights in rural areas will have.
    - Expert mode will default to similar settings differences, however the settings for each flight type are fully customisable and will save to and restore from separate profiles for VFR and IFR.
    - On the ground, TLOD will be locked to either a pre-determined (non-expert) or user-selectable (expert) TLOD Min.
    - Once in the air and above either a pre-determined (non-expert) or user-selectable (expert) TLOD base altitude, TLOD will be allowed to change to the lower of either the schedule based on your TLODs, FPS sensitivity/tolerance and average descent rate settings or what your current performance dictates.
    - Once above a calculated altitude band above the the TLOD base altitude, the app priority will change from TLOD to FPS.
    - On descent your TLOD will progressively work its way down to TLOD Min by the TLOD base altitude. 
  - Use Expert Options - When disabled allows the app to use default settings in conjuction with your chosen target FPS that should produce good automated FPS tracking, provided you have set a realistic FPS target within your system's performance capability. When enabled, the UI expands to show additional MSFS settings to adjust. If you do not understand these settings and their impact on MSFS performance and graphics quality, it is strongly recommended that you do not use these expert options and you should uncheck this option. When Use Expert Setting is unchecked, the following internal settings are used by the app:
    - Auto Target FPS - user selectable. Enabling automatically disables TLOD Min + due to automation control ambiguity
    - FPS Sensitivity - 5%
    - VFR or IFR flight type - user selectable
    - Alt TLOD Base - VFR 100 ft, IFR 1000 ft
    - Avg Descent Rate - VFR 1000 fpm, IFR 2000 fpm
    - TLOD Minimum - VFR 100% of your current MSFS TLOD setting, IFR 50%
    - TLOD Maximum - VFR 300% of your current MSFS TLOD setting, IFR 200%
    - TLOD Min + - enabled, unless Auto Target FPS is enabled then disabled
    - TLOD Max + - disabled
    - TLOD Max - - enabled
    - Decrease Cloud Quality
      - enabled by default and uses the GPU load activation method if GPU-Z is found to be running, otherwise the TLOD activation method is used.
      - can be disabled by setting DecCloudQNonExpert to false in the app config file.
      - GPU load activation method decreases cloud quality with greater than 98% GPU load and recovers with less than 80% GPU load.
      - TLOD activation activation method uses a Cloud Recovery TLOD 2/5 between TLOD Minimum and TLOD Maximum or + 50 over TLOD Min, whichever is lower. If excessive changing of cloud quality levels are detected, the app will automatically increase its calculated cloud recovery TLOD.
    - Auto OLOD - enabled and VFR 150% of your current MSFS OLOD setting, IFR 100%
    - Pause when MSFS loses focus - disabled, unless using MSFS FG then enabled
- Expert Settings
  - Auto Method - FPS Sensitivity generally gives the best results for most users and hence is the default. Use FPS Tolerance if you experience stuttering issues. Use Auto TLOD if you want a DynamicLOD-like experience or are using an FPS cap.
    - FPS Sensitivity (v0.4.2 and later) - smaller changes more often.
      - Determines how sensitive the app will be to the variance between your current and target FPS.
      - Also determines the largest TLOD step size you will see, being double the FPS sensitivity number.
      - The lower the setting the smaller the changes will be, which is useful if you are experiencing stuttering with the default value of 5. Vice versa for higher settings. (1 - 20 allowable)
    - FPS Tolerance (all versions except 0.4.2) - larger changes less often.
      - Determines how much variance from your target FPS must occur before the app will adjust MSFS settings to achieve the target FPS and what nominal magnitude those changes will be.
      - The lower the setting, the more reactive the app will be, the more MSFS settings changes will occur and the changes will be smaller. (1% - 20% allowable)
    - Auto TLOD - functions similar to Auto OLOD by using an altitude schedule and is best for when using system FPS caps.
      - TLOD will adjust based on an altitude band with a base and top level and with TLOD values defined for each of these altitudes.
      - The app will set TLOD Base at or below the Alt TLOD Base (AGL), set the TLOD Top at or above Alt TLOD Top (AGL) and interpolate in between.
      - The nominal LOD Step Size can be set to allow users experiencing stuttering issues to try different LOD step sizes to help resolve the issue. The default value is 5. (1 - 20 allowable)
      - When TLOD Base + is unchecked, this method completely ignores FPS hence all FPS-related settings are removed from the UI.
      - TLOD Base + - additional TLOD with favourable performance conditions.
        - When enabled, a target FPS will be required for the logic to work, which you should preferably set to your FPS cap if you use one or, if not, slightly lower than your normally achievable FPS.
        - The TLOD Base + seek process will automatically start when commencing a flight, regardless of your aircraft's position, and at the conclusion of a flight when on the ground and stopped.
        - This seek process can be manually restarted by pressing the Reset button, should flight conditions change such that the original TLOD Base + is no longer valid.
        - When seeking, TLOD Base + will increase in steps of the original TLOD Base until either TLOD Top is achieved or the FPS cannot consistently achieve the target FPS. If the the latter, TLOD Base + will backtrack to the previous TLOD Base +, where the FPS target was easily achieved.
        - At any time, if the 10 second FPS trend drops below a small threshold under the target FPS then TLOD Base + will automatically reduce by a step of the original TLOD base, down to zero if necessary. In external view, this threshold is greater to account for anticipated temporary FPS dips when scenery gets cached when panning.
        - Avoid rapidly changing views or panning your external view too quickly, especially intially as uncached scenery loads in, as you will induce temporary FPS drops that may trigger an unnecessary TLOD Top + reduction.    
        - If the FPS drops temporarily below the target FPS when taking off and TLOD automatically decreases, an attempt will be made to progressive restorely the lost TLOD should conditions return to being favourable after climbing through Alt TLOD Top.
        - The calculated TLOD Base + will be applied as an offset that increasies the entire TLOD altitude schedule by that amount.
        - Cannot be enabled with TLOD Top + due to conflicting control over TLOD Top. Selecting both will result in the most recent selection being enabled and the other disabled, with a dialog box to advise this.
      - TLOD Top + - additional TLOD Top in high elevation areas.
        - Operates the same as TLOD Max + except that it cannot be enabled with TLOD Base + due to conflicting control over TLOD Top. Selecting both will result in the most recent selection being enabled and the other disabled, with a dialog box to advise this.
      - TLOD Top - reduced TLOD Top at night. Operates the same as TLOD Max -. 
  - Pause when MSFS loses focus
    - Will stop LODs and, if applicable, cloud quality from changing while you are focused on another app and not MSFS.
    - Particularly useful for when using MSFS FG as the FG active and inactive frame rate can vary quite considerably and because FG is not always an exact doubling of non-FG FPS. 
  - TLOD Min - Sets the minimum TLOD the automation algorithm will use. (10 - TLOD Max-10 allowable)
  - TLOD Min + - additional TLOD Min with favourable performance conditions.
    - Requires at least 15% FPS headroom above target FPS to work at all. If you use an FPS cap, set your target FPS to at least 15% below it, preferably more.
    - When enabled, the TLOD Min + seek process will automatically start when commencing a flight, regardless of your aircraft's position, and at the conclusion of a flight when on the ground and stopped.
    - This seeking process can be manually restarted by pressing the Reset button, should flight conditions change such that the original TLOD Min + is no longer valid.
    - When seeking on the ground, TLOD Min + will progressively increase, in larger steps at first, until a higher TLOD Min with less than 15% FPS headroom is available.
    - On climb out, TLOD Min + will remain set until your aircraft passes the calculated altitude threshold for the app priority mode to transition from TLOD to FPS priority.
    - While in FPS priority mode, TLOD Min + will calculate to be 50% (IFR) or 25% (VFR) of the lower of either whatever TLOD you are currently getting or TLOD Max without TLOD Mtn Amt, but no lower than TLOD Min.
    - On descent through the calculated TLOD priority mode transition altitude, TLOD Min + will lock until landed to give the app time to reduce TLOD to Min at a moderate rate.
    - If at any time conditions deteriorate after TLOD Min + is set, there is an automatic 20% reduction of TLOD Min + in order to maintain target FPS. 
    - Avoid rapidly changing views or panning your external view too quickly, especially initially as un-cached scenery loads in, as you will induce temporary FPS drops that may trigger an unnecessary TLOD Min + reduction.    
    - Cannot be enabled at the same time as Auto Target FPS due to automation control ambiguity. Selecting both will result in the most recent selection being enabled and the other disabled, with a dialog box to advise this.
  - TLOD Max - Sets the maximum TLOD the automation algorithm will use. (TLOD Min+10 - 1000 allowable)
  - TLOD Max + - additional TLOD Max in high elevation areas. 
    - When enabled, extends TLOD Max in areas where the terrain is higher than Mtn Alt Min (100ft - 100000ft allowable) by the TLOD Mtn Amt amount (10 - 1000 allowable), progressively increasing by the TLOD step size per second until completely activated.
    - If terrain drops below Mtn Alt Min, TLOD Max + will remain fixed for 5 minutes then progressively reduce by the TLOD step size per second until completely deactivated.
  - TLOD Max - reduced TLOD Max at night
    - Halves TLOD Max/Top at night to reduce system workload by not drawing scenery out to distances that can't be seen in the dark anyway.
    - Works with all automation methods: FPS Sensitivity, FPS Tolerance and Auto TLOD.
    - Defaults to enabled in non-expert mode. Enabled in Expert mode by checking the - box to the right of the TLOD Max/Top textbox.
    - When your flight transitions from day to night time, based on your location and the local time, TLOD Max/Top will progressively reduce to half its normal value, including the progressive removal of any TLOD Min/Base + and TLOD Max/Top + in use.
    - When your flight transitions from night to day time, based on your location and the local time, TLOD Max/Top will first progressively increase to its normal value then, providing you are either stopped on the ground or are in the air above Alt TLOD Min/Base, will activate the seeking process if TLOD Min/Base + is enabled and reactivate TLOD Max/Top + if enabled.
    - The status line will show either Day or Night when activated and Δ while transitioning between them.
  - Alt TLOD Base - Altitude (AGL) at or below which TLOD will be at TLOD Min. (100ft - 100000ft allowable)
  - Avg Descent Rate- Used in combination with FPS sensitivity to determine the altitude band in which TLOD will be interpolated between TLOD Min at the Alt TLOD base starting point and the lower of TLOD Max and the maximum TLOD your system can achieve while achieving at least your desired FPS target at a calculated top altitude. (200fpm - 10000fpm allowable)
    - This band ensures that, if you descend at your set Avg Descent Rate or less, that the app can decrement TLOD from TLOD Max to TLOD Min by the Alt TLOD Base without exceeding the LOD Step rate associated with the FPS sensitivity level you have set.
  - Decrease Cloud Quality - When enabled, will reduce/restore cloud quality by one level if the activation condition is met.
    - Activation Methods
      - TLOD is the original method and is most suitable for systems where TLOD has the largest impact on desired MSFS performance.
      - GPU Load is the new method that allows cloud quality changes to occur independently of TLOD and is most suitable for systems where cloud quality has a similar or larger impact on desired MSFS performance than TLOD does.
      - IFR and VFR flight modes will use the same cloud reduction method.
    - TLOD (FPS Sensitivity and FPS Tolerance TLOD Automation Methods)
      - Decreases when TLOD has already auto reduced to TLOD Min and FPS is still below target FPS by more than the FPS tolerance.
      - Cloud Recovery TLOD with optional + (resultant TLOD must fall within TLOD Min+5 and TLOD Max-5)
        - The TLOD level required to cancel an active cloud quality reduction state and restore cloud quality back to its initial higher quality level.
        - Ideally set to 50 TLOD or more above TLOD Min to provide a TLOD buffer to minimise the chance that cloud quality will constantly change down and up.
        - When + is checked, Cloud Recovery TLOD becomes relative to TLOD Min instead of absolute.
    - GPU Load (All TLOD Automation Methods)
      - Requires the GPU-Z companion app to be installed and running for this method to work. If GPU-Z is not running, the user will be alerted to start it in on the app status line in the General section.
      - Decreases when the GPU load, as measured by the GPU-Z companion app, is higher than the user-defined Decrease GPU Load percentage. (50% - 100% allowable)
      - Cloud Recovery GPU load (5% - 90% and at least 10% less than Decrease GPU Load allowable)
        - Recovers when the GPU load is lower than the user-defined Recover GPU Load percentage.
        - Ideally set to at least 15% lower than the Decrease GPU Load percentage to provide a GPU load buffer to minimise the chance that cloud quality will constantly change down and up.
  -  Auto OLOD
     -  When enabled, four user definable parameters relating to this feature will be revealed on the UI.
     -  Rather than the automation being FPS based, which would cause contention with TLOD changes at the same time, OLOD will adjust based on an altitude band with a base (1000ft minimum and less than top) and top level (2000ft minimum, 100000ft maximum and greater than base) and with OLOD values defined for each of these altitudes (10 - 1000 allowable).
     -  The app will set OLOD @ Base at or below the Alt OLOD Base (AGL), set the OLOD @ Top at or above Alt OLOD Top (AGL) and interpolate in between. Note that OLOD @ Base can be higher, lower or the same value as the OLOD @ Top, depending on whether you want OLOD to decrease, increase or stay the same respectively as you ascend. 
<br/><br/>
