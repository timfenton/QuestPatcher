Just a few quick changes to QuestPatcher as a *temporary* fix for Quest 3 BeatSaber modding. All credit to sc2ad for a modified modloader64.so and Lauriethefish for providing the questpatcher tool. 

This fork serves as a temporary solution to mod beatsaber on Quest 3, once BMBF is updated to work on Quest 3 firmware it is *highly recommended* to remod all the things using the latest BMBF

- Step 0: Uninstall Beat saber, lets start fresh.
- Step 1: [Enable developer mode](https://bsmg.wiki/quest-modding.html )
- Step 2: Sideload the downgraded 1.28 beat saber APK according to [these instructions](https://bsmg.wiki/quest-modding-bmbf.html#downgrading-beat-saber)
- Step 2: Download QuestPatcher from the [releases section](https://github.com/timfenton/QuestPatcher/releases/tag/0.1) of *this* repo
- Step 3: Open QuestPatcher on your PC
- Step 4: Select com.beatgames.beatapp if its not already selected
- Step 5: Click "PATCH APP"
- Step 6: Go to tools > Remove old BS Mod Directories
- Step 7: Open beatsaber on quest, accept everything and go to solo mode, skip tutorial, exit beatsaber
- Step 8: Download [Custom Types Latest .QMOD file](https://github.com/sc2ad/Il2CppQuestTypePatching/releases)
- Step 9: Download [Core Mods Latest .QMOD file](https://oculusdb.rui2015.me/api/coremodsdownload/1.28.0_4124311467.qmod)
- Step 10: Install Custom Types.QMOD then install Core Mods Latest QMOD file via the custom mods section of quest patcher

Load Beat saber up and enjoy. 

Installing other mods is possible, however unsupported by mod developers currently. Use at your own risk. 
Download and install mods from ComputerElite's [Mod directory](https://computerelite.github.io/tools/Beat_Saber/questmods.html) and install using this version of Quest patcher.

# QuestPatcher

QuestPatcher is a GUI based mod installer for any il2cpp unity app on the Oculus Quest that runs on Windows, Linux or macOS.
It was originally created for modding Gorilla Tag.

[Latest Stable Release](https://github.com/Lauriethefish/QuestPatcher/releases/latest) | [Latest Nightly Build](https://nightly.link/Lauriethefish/QuestPatcher/workflows/standalone/main)

It uses Sc2ad's [QuestLoader](https://github.com/sc2ad/QuestLoader/) for loading mods and originally followed the patching process of RedBrumbler's [CLI Tool](https://github.com/RedBrumbler/QuestAppPatcher).

See `QuestPatcher.Core/Resources/qmod.schema.json` for the `mod.json` that QMODs must contain, alongside their mod files.
