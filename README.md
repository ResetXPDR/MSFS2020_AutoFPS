# MSFS2020_AutoFPS

Based on muumimorko's idea and code in MSFS_AdaptiveLOD, as further developed by Fragtality in DynamicLOD and myself in DynamicLOD_ResetEdition.<br/><br/>

This utility is new development that is a simplification DynamicLOD_ResetEdition. It aims to improve MSFS performance and smoothness by automatically changing the TLOD and OLOD based on the current AGL and an easy to use GUI. It provides features such as:<br/>
- Adjusting TLOD automatically to achieve a user-defined target FPS band based on user-defined maximum and minimum LODs,<br/>
- Simultaneous PC and VR mode compatibilty,<br>
- Cloud quality decrease option for when FPS can't be achieved at the lowest desired TLOD,<br/>
- Correct display of FPS with Frame Generation active,<br/> 
- Auto future MSFS version compatibility, provided MSFS memory changes are like in previous updates,<br/>
- Update prompt if newer utility version found on startup,<br/>
- Auto restoration of original settings changed by the utility, and</br>
- Greatly simplified GUI.<br/><br/>

Important:<br/> 
- This utility directly accesses active MSFS memory locations while MSFS is running to read and set OLOD, TLOD and cloud quality settings on the fly. From 0.3.7 version onwards, the utility will first verify that the MSFS memory locations being used are still valid and if not, likely because of an MSFS version change, will attempt to find where they have been relocated. If it does find the new memory locations and they pass validation tests, the utility will update itself automatically and will function as normal. If it can't find or validate MSFS memory locations at any time when starting up, the utility will self-restrict to read only mode to prevent the utility making changes to unknown MSFS memory locations.<br/>
- As such, I believe the app to be robust in its interaction with validated MSFS memory locations and to be responsible in disabling itself if it can't guarantee that. Nonetheless, this utility is offered as is and no responsibility will be taken for unintended negative side effects. Use at your own risk!<br/><br/>

If you are not familiar with what MSFS graphics settings do, specifically TLOD, OLOD and cloud quality, and don't understand the consequences of changing them, it is highly recommended you do not use this utility.
<br/><br/>

This utility is unsigned because I am a hobbyist and the cost of obtaining certification is prohibitive to me. As a result, you may get a warning message of a potentially dangerous app when you download it in a web browser like Chrome. You can either trust this download, based on feedback you can easily find on Avsim and Youtube, and run a virus scan and malware scan before you install just be sure, otherwise choose not to and not have this utility version.<br/><br/>

## Requirements

The Installer will install the following Software:
- .NET 7 Desktop Runtime (x64)
- MobiFlight Event/WASM Module

<br/>

Currently in development, but when available [Download here](https://github.com/ResetXPDR/AutoLOD_ResetEdition/releases/latest)

(Under Assests, the AutoLOD_ResetEdition-Installer-vXYZ.exe File)

<br/><br/>

## Installation / Update / Uninstall
Basically: Just run the Installer.<br/>

Some Notes:
- AutoLOD_ResetEdition has to be stopped before installing.
- If the MobiFlight Module is not installed or outdated, MSFS also has to be stopped.
- If you have duplicate MobiFlight Modules installed, in either your official or community folders, the utility may display 0 value Sim Values and otherwise not function. Remove the duplicate versions, rerun the utility installer and it should now work.
- Do not run the Installer as Admin!
- If you wish to retain your settings for an update version, do NOT uninstall first, as that deletes all app files, including the config file. Just run the installer, select update and your settings will be retained.
- For Auto-Start either your FSUIPC7.ini or EXE.xml (MSFS) is modified. The Installer does not create a Backup.
- The utility may be blocked by Windows Security or your AV-Scanner, try if unblocking and/or setting an Exception helps (for the whole Folder)
- The Installation-Location is fixed to %appdata%\AutoLOD_ResetEdition (your Users AppData\Roaming Folder) and can't be changed.
  - Binary in %appdata%\AutoLOD_ResetEdition\bin
  - Logs in %appdata%\AutoLOD_ResetEdition\log
  - Config: %appdata%\AutoLOD_ResetEdition\AutoLOD_ResetEdition.config

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
  - Will not show valid values unless all three connections are green.
  - Red values mean FPS Adaption is active, orange means LOD stepping is active, black means steady state, n/a means not available right now.
- General
  - You can have (exactly) six different Sets/Profiles for the AGL/LOD Pairs to switch between (manually but dynamically).
  - Cruise LOD Updates, when checked, will continue to update LOD values based on AGL in the cruise phase, which is useful for VFR flights over undulating terrain and has an otherwise negligble impact on high level or IFR flights so it is recommended to enable this.
  - LOD Step Max, when checked, allows the utility to slow the rate of change in LOD per second, with increase and decrease being individually settable, to smooth out LOD table changes. This allows you to have large steps in your LOD tables without experiencing abrupt changes like having it disabled would do, hence it is recommended to turn it on and start out with the default steps of 5.
  - App status area in the bottom right will display messages depending on connection status about new utility updates, compatibility test failures, PC or VR mode and whether Frame Generation is currently active (MSFS must have the focus for this to display FG FPS correctly). 
- LOD Level Tables
  - The first Pair with AGL 0 can not be deleted. The AGL can not be changed. Only the xLOD.
  - Additional Pairs can be added at any AGL and xLOD desired. Pairs will always be sorted by AGL.
  - Plus is Add, Minus is Remove, S is Set (Change). Remove and Set require to double-click the Pair first.
  - A Pair is selected (and the configured xLOD applied) when the current AGL is above the configured AGL. If the current AGL goes below the configured AGL, the next lower Pair will be selected.
  - A new Pair is only selected in Accordance to the VS Trend - i.e. a lower Pair won't be selected if you're actually Climbing (only the next higher)
  - Many users are finding it better to reduce, not increase, OLOD values at higher altitudes as you can't clearly see objects from such distances anyway, especially in VR.
- FPS Adaption:
  - Settings in the FPS adaption area only work if you have checked Limit LODs.
  - FPS Adaption will activate when your FPS is below the target FPS you have set, after any Delay start you have set.
  - Reduce TLOD/OLOD is the maximum values it will reduce those settings by from the current LOD pair values, minimum TLOD/OLOD permitting. If you want to use the Decrease Cloud Quality option without reducing LODs, set these both to 0.
  - Minimum TLOD/OLOD is the minimum values it will allow those settings to reduce to.
  - Delay start is how many seconds of FPS below the target FPS have to occur before FPS Adaption will activate, to stop it false triggering with a transient FPS drop. Default is 1 second but 2 seconds is good too.
  - Reduce for is how many seconds of FPS above the target FPS, plus cloud recover FPS if used, have to occur before FPS Adaption will cancel, to stop it false cancelling with a unsustained FPS increases.
  - Decrease Cloud Quality, when checked, will reduce cloud quality by one level while FPS adaption is active. 
  - Cloud Recovery FPS + is how many FPS to add to the target FPS for determining whether to cancel FPS adaption once activated. This provides an FPS buffer to account for the increased FPS achieved by reducing cloud quality to stop FPS adaption constantly toggling on and off.
- **Less is more**:
  - Fewer Increments/Decrements are better of reasonable Step-Size (roughly in the Range of 25-75) or use Step LOD Max to spread LOD changes out over time.
  - Don't overdo it with extreme low or high xLOD Values. A xLOD of 100 is reasonable fine on Ground, 200-ish is reasonable fine in the air. 400 if you have a super computer.
  - Tune your AGL/LOD Pairs to the desired Performance (which is more than just FPS).
  - FPS Adaption is just *one temporary* Adjustment on the current AGL/xLOD Pair to fight some special/rare Situations.
  - Forcing the Sim to (un)load Objects in rapid Succession defeats the Goal to reduce Stutters. It is *not* about FPS.
  - Smooth Transitions lead to smoother experiences.  
<br/><br/>
