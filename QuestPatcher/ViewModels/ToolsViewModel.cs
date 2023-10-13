using Avalonia.Controls;
using QuestPatcher.Models;
using QuestPatcher.Services;
using QuestPatcher.Views;
using System;
using System.Diagnostics;
using System.IO;
using ReactiveUI;
using QuestPatcher.Core;
using QuestPatcher.Core.Models;
using QuestPatcher.Core.Patching;
using Serilog;
using JetBrains.Annotations;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;

namespace QuestPatcher.ViewModels
{
    public class ToolsViewModel : ViewModelBase
    {
        public Config Config { get; }

        public ProgressViewModel ProgressView { get; }

        public OperationLocker Locker { get; }
        
        public ThemeManager ThemeManager { get; }

        public string AdbButtonText => _isAdbLogging ? "Stop ADB Log" : "Start ADB Log";

        private bool _isAdbLogging;

        private readonly Window _mainWindow;
        private readonly SpecialFolders _specialFolders;
        private readonly PatchingManager _patchingManager;
        private readonly AndroidDebugBridge _debugBridge;
        private readonly QuestPatcherUiService _uiService;
        private readonly InfoDumper _dumper;

        public ToolsViewModel(Config config, ProgressViewModel progressView, OperationLocker locker, Window mainWindow, SpecialFolders specialFolders, PatchingManager patchingManager, AndroidDebugBridge debugBridge, QuestPatcherUiService uiService, InfoDumper dumper, ThemeManager themeManager)
        {
            Config = config;
            ProgressView = progressView;
            Locker = locker;
            ThemeManager = themeManager;

            _mainWindow = mainWindow;
            _specialFolders = specialFolders;
            _patchingManager = patchingManager;
            _debugBridge = debugBridge;
            _uiService = uiService;
            _dumper = dumper;

            _debugBridge.StoppedLogging += (_, _) =>
            {
                Log.Information("ADB log exited");
                _isAdbLogging = false;
                this.RaisePropertyChanged(nameof(AdbButtonText));
            };
        }

        public async void UninstallApp()
        {
            try
            {
                DialogBuilder builder = new()
                {
                    Title = "Are you sure?",
                    Text = "Uninstalling your app will exit QuestPatcher, as it requires your app to be installed. If you ever reinstall your app, reopen QuestPatcher and you can repatch"
                };
                builder.OkButton.Text = "Uninstall App";
                if (await builder.OpenDialogue(_mainWindow))
                {
                    Locker.StartOperation();
                    try
                    {
                        Log.Information("Uninstalling app . . .");
                        await _patchingManager.UninstallCurrentApp();
                    }
                    finally
                    {
                        Locker.FinishOperation();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to uninstall app: {ex}");
            }
        }

        public void OpenLogsFolder()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _specialFolders.LogsFolder,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        public async void QuickFix()
        {
            Locker.StartOperation(true); // ADB is not available during a quick fix, as we redownload platform-tools
            try
            {
                await _uiService.QuickFix();
                Log.Information("Done!");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to clear cache: {ex}");
                DialogBuilder builder = new()
                {
                    Title = "Failed to clear cache",
                    Text = "Running the quick fix failed due to an unhandled error",
                    HideCancelButton = true
                };
                builder.WithException(ex);

                await builder.OpenDialogue(_mainWindow);
            }   finally
            {
                Locker.FinishOperation();
            }
        }

        public async void RemoveOldModDirectories()
        {
            try
            {
                List<string> modPaths = new List<string>
                {
                    "/sdcard/Android/data/com.beatgames.beatsaber/files/libs",
                    "/sdcard/Android/data/com.beatgames.beatsaber/files/mods"
                };

                await _debugBridge.Chmod(modPaths, "777");

                foreach(string modPath in modPaths)
                {
                    Log.Information(modPath);
                    await _debugBridge.RemoveDirectory(modPath);
                }

                Log.Information("Done!");

                DialogBuilder builder = new()
                {
                    Title = "Finished removing old mod folders!",
                    Text = $"The following mod folders were removed: \n{modPaths[0]}\n{modPaths[1]}",
                    HideCancelButton = true
                };
                await builder.OpenDialogue();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to remove the old mod directories: {ex}");
                DialogBuilder builder = new()
                {
                    Title = "Failed to remove the old mod directories",
                    Text = "Removing the old mod directories failed due to an unhandled error",
                    HideCancelButton = true
                };
                builder.WithException(ex);

                await builder.OpenDialogue(_mainWindow);
            }
        }

        public async void FixModPermissions()
        {
            try
            {
                List<string> modPaths = new List<string>
                {
                    "/sdcard/Android/data/com.beatgames.beatsaber/files/libs/*",
                    "/sdcard/Android/data/com.beatgames.beatsaber/files/mods/*"
                };

                List<string> libsExist = await _debugBridge.ListDirectoryFiles(modPaths[0].Replace("/*",""));
                List<string> modsExist = await _debugBridge.ListDirectoryFiles(modPaths[1].Replace("/*", ""));

                if(libsExist.Count <= 0)
                    throw new Exception("Library files are not copied, ensure you have installed core mods successfully");

                if(modsExist.Count <= 0)
                    throw new Exception("Mod files are not copied, ensure you have installed core mods successfully");

                await _debugBridge.Chmod(modPaths, "+r");
                Log.Information("Done!");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to fix the mod directory permissions: {ex}");
                DialogBuilder builder = new()
                {
                    Title = "Failed to fix the mod directory permissions",
                    Text = "Running the fix permissions failed due to an unhandled error",
                    HideCancelButton = true
                };
                builder.WithException(ex);

                await builder.OpenDialogue(_mainWindow);
            }
        }

        public async void ToggleAdbLog()
        {
            if(_isAdbLogging)
            {
                _debugBridge.StopLogging();
            }
            else
            {
                Log.Information("Starting ADB log");
                await _debugBridge.StartLogging(Path.Combine(_specialFolders.LogsFolder, "adb.log"));

                _isAdbLogging = true;
                this.RaisePropertyChanged(nameof(AdbButtonText));
            }
        }

        public async void CreateDump()
        {
            Locker.StartOperation();
            try
            {
                // Create the dump in the default location (the data directory)
                string dumpLocation = await _dumper.CreateInfoDump();

                string? dumpFolder = Path.GetDirectoryName(dumpLocation);
                if (dumpFolder != null)
                {
                    // Open the dump's directory for convenience
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = dumpFolder,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
            catch (Exception ex)
            {
                // Show a dialog with any errors
                Log.Error($"Failed to create dump: {ex}");
                DialogBuilder builder = new()
                {
                    Title = "Failed to create dump",
                    Text = "Creating the dump failed due to an unhandled error",
                    HideCancelButton = true
                };
                builder.WithException(ex);

                await builder.OpenDialogue(_mainWindow);
            }
            finally
            {
                Locker.FinishOperation();
            }
        }

        public async void ChangeApp()
        {
            await _uiService.OpenChangeAppMenu(false);
        }

        public void OpenThemesFolder()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ThemeManager.ThemesDirectory,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}
