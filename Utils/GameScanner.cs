﻿using GameLauncher.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace GameLauncher.Utils
{
    public class GameScanner
    {
        
        private readonly string Steam32 = "SOFTWARE\\VALVE\\";
        private readonly string Steam64 = "SOFTWARE\\Wow6432Node\\Valve\\";
        //private readonly string EpicRegistry = "SOFTWARE\\WOW6432Node\\EpicGames\\Unreal Engine";
        private readonly string UplayRegistry = "SOFTWARE\\WOW6432Node\\Ubisoft\\Launcher";
        //private readonly string OriginsRegistry = "SOFTWARE\\WOW6432Node\\Origin";
        private readonly string BlizzardRegistry = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
        public bool DirExists { get; set; }
        private string gamePath = string.Empty;

        public ObservableCollection<Platform> LibraryDirectories { get; set; }

        public GameScanner()
        {
            LibraryDirectories = new ObservableCollection<Platform>();
        }

        public void Scan()
        {
            //TEST();
            GetBattleNetDirs();
            GetSteamDirs();
            GetEpicDirs();
            GetOriginsDir();
            GetExternalUplayGames();
            GetUplayDirs();
            ReadUserAddedDirectories();
        }

        private void ReadUserAddedDirectories()
        {
            var dirs = Properties.Settings.Default.FolderPaths;
            if (dirs != null || dirs.Count != 0)
            {
                foreach (var dir in dirs)
                {
                    if (Directory.Exists(dir) && dir != null)
                    {
                        LibraryDirectories.Add(new Platform
                        {
                            PlatformType = Platforms.NONE,
                            Name = nameof(Platforms.NONE),
                            InstallationPath = dir
                        });
                    }
                    else
                    {
                        // remove the dirs that are in the settings and dont exits on the machine
                        dirs.Remove(dir);
                        Properties.Settings.Default.Save();
                    }
                }
            }
        }

        private void TEST()
        {


            RegistryKey key = Registry.LocalMachine.OpenSubKey(BlizzardRegistry);
            if (key != null)
            {
                foreach (string ksubKey in key.GetSubKeyNames())
                {
                    using (RegistryKey subKey = key.OpenSubKey(ksubKey))
                    {
                        foreach (string subkeyname in subKey.GetValueNames())
                        {

                            string tempTitle = string.Empty, installLocation = string.Empty, publisher = string.Empty;

                            if (subkeyname.ToString() == "DisplayName")
                            {
                                tempTitle = subKey.GetValue("DisplayName").ToString();
                            }

                            if (subkeyname.ToString() == "DisplayName")
                            {
                                var x = subKey.GetValue("DisplayName").ToString();
                                if (x.Contains("Epic Games"))
                                {
                                    var s = x;
                                }
                                tempTitle = subKey.GetValue("DisplayName").ToString();
                            }

                            if (subkeyname.ToString() == "Publisher")
                            {
                                publisher = subKey.GetValue("Publisher").ToString();
                                if (publisher.Contains("Epic Games"))
                                {
                                    var x = subKey.GetValue("DisplayName").ToString();
                                }
                            }

                            //if (tempTitle == "Fortnite")
                            //{
                            //    gamePath = subKey.GetValue("InstallLocation").ToString();
                            //    string[] splitArray = gamePath.Split('\\');
                            //    string gameNameFromPath = splitArray[splitArray.Length - 1];
                            //    string gameInstalledDir = gamePath.Replace(gameNameFromPath, "");

                            //    LibraryDirectories.Add(new Platform
                            //    {
                            //        PlatformType = Platforms.BLIZZARD,
                            //        Name = nameof(Platforms.BLIZZARD),
                            //        InstallationPath = gameInstalledDir
                            //    });
                            //}

                        }
                    }
                }
            }
        }

        // seems fine error wise
        public void GetSteamDirs()
        {
            try
            {
                RegistryKey key = string.IsNullOrEmpty(Registry.LocalMachine.OpenSubKey(Steam64).ToString())
                    ? Registry.LocalMachine.OpenSubKey(Steam32)
                    : Registry.LocalMachine.OpenSubKey(Steam64);

                DirExists = CheckIfRegistryDirExists(Steam64 + "\\Steam", "InstallPath");

                if (DirExists)
                {
                    foreach (string k in key.GetSubKeyNames())
                    {
                        using (RegistryKey subKey = key.OpenSubKey(k))
                        {
                            string steamPath = subKey.GetValue("InstallPath").ToString();
                            string configPath = steamPath + "/steamapps/libraryfolders.vdf";
                            if (File.Exists(configPath))
                            {
                                IEnumerable<string> configLines = File.ReadAllLines(configPath)
                                    .Where(l => !string.IsNullOrEmpty(l) && l.Contains(":\\"));
                                foreach (var line in configLines)
                                {
                                    LibraryDirectories.Add(new Platform
                                    {
                                        PlatformType = Platforms.STEAM,
                                        Name = nameof(Platforms.STEAM),
                                        InstallationPath = $"{line.Substring(line.IndexOf(":") - 1, line.Length - line.IndexOf(":"))}\\steamapps\\common"
                                    });
                                }

                                // adds the steam library where windows is installed.
                                LibraryDirectories.Add(new Platform
                                {
                                    PlatformType = Platforms.STEAM,
                                    Name = nameof(Platforms.STEAM),
                                    InstallationPath = $"{steamPath}\\steamapps\\common"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void GetOriginsDir()
        {
            string title = string.Empty, publisher = string.Empty;
            RegistryKey key = Registry.LocalMachine.OpenSubKey(BlizzardRegistry);
            bool PublisherFound = false;
            foreach (string ksubKey in key.GetSubKeyNames())
            {
                using (RegistryKey subKey = key.OpenSubKey(ksubKey))
                {
                    foreach (string subkeyname in subKey.GetValueNames())
                    {
                        PublisherFound = false;
                        if (subkeyname.ToString() == "Publisher")
                        {
                            publisher = subKey.GetValue("Publisher").ToString();
                            title = subKey.GetValue("DisplayName").ToString();
                            PublisherFound = true;
                        }
                        if (subkeyname.ToString() == "InstallLocation")
                        {
                            gamePath = subKey.GetValue("InstallLocation").ToString();
                        }
                        if (PublisherFound == true)
                        {
                            if (publisher.Contains("Electronic Arts") && !title.Contains("Origin"))
                            {
                                gamePath = subKey.GetValue("InstallLocation").ToString();

                                if (title.Contains('™'))
                                {
                                    title.Replace('™', ' ');
                                }
                                int index = gamePath.IndexOf(title);
                                if (index > 0)
                                    gamePath = gamePath.Substring(0, index);

                                LibraryDirectories.Add(new Platform
                                {
                                    PlatformType = Platforms.ORIGIN,
                                    Name = nameof(Platforms.ORIGIN),
                                    InstallationPath = gamePath
                                });
                            }
                        }
                    }
                }
            }
        }

        //gets x86 directory, this is where epic games launcher is installed. But games are installed at Program Files 64
        public void GetEpicDirs()
        {
            char[] letters = { 'c', 'd', 'e' };

            foreach (var letter in letters)
            {
                string programFiles = "";
                //string programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                if (Directory.Exists($"{letter}:\\Program Files"))
                {
                    programFiles = $"{letter}:\\Program Files";
                    string[] dirs = Directory.GetDirectories(programFiles);
                    var epicGames64 = Directory
                                    .GetDirectories(programFiles)
                                    .Where(folder => folder.Equals($"{letter}:\\Program Files\\Epic Games"))
                                    .ToList();
                    try
                    {
                        if (epicGames64 != null && Directory.Exists(epicGames64[0]))
                        {
                            LibraryDirectories.Add(new Platform
                            {
                                PlatformType = Platforms.EPIC,
                                Name = "Epic",
                                InstallationPath = epicGames64[0]
                            });
                        }

                        // this code gets this directory > C:\Program Files (x86)\Epic Games\
                        // but games dont get installed by default into that directory
                        // so this way is pointless
                        //DirExists = CheckIfRegistryDirExists(EpicRegistry, "INSTALLDIR");
                        //if (DirExists)
                        //{
                        //    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(EpicRegistry))
                        //    {
                        //        foreach (string k in key.GetSubKeyNames())
                        //        {
                        //            using (RegistryKey subkey = key.OpenSubKey(k))
                        //            {
                        //                string epicGamesPath = subkey.GetValue("InstalledDirectory").ToString();
                        //                epicGamesPath = epicGamesPath.Substring(0, epicGamesPath.Length - 4);
                        //                LibraryDirectories.Add(new Platform
                        //                {
                        //                    PlatformType = Platforms.EPIC,
                        //                    Name = "Epic",
                        //                    InstallationPath = epicGamesPath
                        //                });
                        //            }
                        //        }
                        //    }
                        //}
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

            }

        }

        public void GetUplayDirs()
        {
            try
            {
                DirExists = CheckIfRegistryDirExists(UplayRegistry, "InstallDir");

                if (DirExists)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(UplayRegistry))
                    {
                        string uplayPath = key.GetValue("InstallDir").ToString();
                        string uplayPathTrimmed = uplayPath + "games";
                        LibraryDirectories.Add(new Platform
                        {
                            PlatformType = Platforms.UPLAY,
                            Name = nameof(Platforms.UPLAY),
                            InstallationPath = uplayPathTrimmed
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void GetExternalUplayGames()
        {
            string key = @"SOFTWARE\WOW6432Node\Ubisoft\Launcher\Installs";
            RegistryKey keyExists = Registry.LocalMachine.OpenSubKey(key);

            if (keyExists != null)
            {
                foreach (string subKey in keyExists.GetSubKeyNames())
                {
                    if (subKey != "205")
                    {
                        using (RegistryKey k = keyExists.OpenSubKey(subKey))
                        {
                            string gamePath = k.GetValue("InstallDir").ToString();
                            string[] splitArray = gamePath.Split('/');
                            string gameNameFromPath = splitArray[splitArray.Length - 2];
                            string gameInstalledDir = gamePath.Replace(gameNameFromPath, "");

                            LibraryDirectories.Add(new Platform
                            {
                                PlatformType = Platforms.UPLAY,
                                Name = nameof(Platforms.UPLAY),
                                InstallationPath = gameInstalledDir
                            });
                        }
                    }
                }
            }
        }

        public void GetBattleNetDirs()
        {
            List<string> battleNetGames = AddBattleNetGamesToList();

            RegistryKey key = Registry.LocalMachine.OpenSubKey(BlizzardRegistry);
            if (key != null)
            {
                foreach (string ksubKey in key.GetSubKeyNames())
                {
                    using (RegistryKey subKey = key.OpenSubKey(ksubKey))
                    {
                        foreach (string subkeyname in subKey.GetValueNames())
                        {
                            foreach (var battleNetGame in battleNetGames)
                            {
                                string tempTitle = string.Empty, installLocation = string.Empty;

                                if (subkeyname.ToString() == "DisplayName")
                                {
                                    tempTitle = subKey.GetValue("DisplayName").ToString();
                                }

                                if (battleNetGame.Equals(tempTitle))
                                {
                                    gamePath = subKey.GetValue("InstallLocation").ToString();
                                    string[] splitArray = gamePath.Split('\\');
                                    string gameNameFromPath = splitArray[splitArray.Length - 1];
                                    string gameInstalledDir = gamePath.Replace(gameNameFromPath, "");

                                    LibraryDirectories.Add(new Platform
                                    {
                                        PlatformType = Platforms.BLIZZARD,
                                        Name = nameof(Platforms.BLIZZARD),
                                        InstallationPath = gameInstalledDir
                                    });
                                }
                            }
                        }
                    }
                }
            }

        }

        public List<string> AddBattleNetGamesToList()
        {
            List<string> battleNetGames = new List<string>();

            battleNetGames.Add("Hearthstone");
            battleNetGames.Add("World of Warcraft");
            battleNetGames.Add("Diablo III");
            battleNetGames.Add("Starcraft II");
            battleNetGames.Add("Heroes of the Storm");
            battleNetGames.Add("Overwatch");
            battleNetGames.Add("StarCraft");
            battleNetGames.Add("Warcraft III");
            battleNetGames.Add("Destiny 2");
            battleNetGames.Add("Call of Duty: Black Ops 4");

            // To be added when install dir for COD MW is found
            //battleNetGames.Add("Call of Duty: Modern Warfare");

            return battleNetGames;
        }

        private string RemoveSpecialWordsAndChars(string prop)
        {
            var charsToRemove = new string[] { "(", ")", "Demo", "Trial" };

            foreach (var c in charsToRemove)
                prop = prop.Replace(c, string.Empty);
            return prop;
        }

        private bool CheckIfRegistryDirExists(string key, string value)
        {
            RegistryKey r;
            bool keyExists = false;
            try
            {
                r = Registry.LocalMachine.OpenSubKey(key);
                if (r != null)
                {
                    string k = r.GetValue(value).ToString();

                    if (r != null && (k != null && Directory.Exists(k)))
                        keyExists = true;
                    else
                        keyExists = false;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return keyExists;
        }

        public ObservableCollection<Game> GetExecutables()
        {
            ObservableCollection<Game> games = new ObservableCollection<Game>();
            foreach (Platform libDir in LibraryDirectories)
            {
                if (libDir.PlatformType.Equals(Platforms.BLIZZARD))
                {
                    var lib = libDir.InstallationPath;
                    if (lib != null && Directory.Exists(lib))
                    {
                        string[] gameDirs = Directory.GetDirectories(lib);

                        foreach (var gameDir in gameDirs)
                        {
                            if (gameDir.Equals(gamePath))
                            {
                                string[] exes = Directory.GetFiles(gameDir, "*.exe", SearchOption.AllDirectories);
                                if (exes.Length != 0)
                                {
                                    Game game = new Game
                                    {
                                        Name = new DirectoryInfo(RemoveSpecialWordsAndChars(gameDir)).Name,
                                        Platform = libDir.PlatformType,
                                        Executables = new List<string>(exes),
                                        InstallPath = gameDir

                                    };

                                    games.Add(game);
                                }
                            }
                        }
                    }
                }
                else
                {
                    var lib = libDir.InstallationPath;
                    if (lib != null && Directory.Exists(lib))
                    {
                        string[] gameDirs = Directory.GetDirectories(lib);


                        foreach (var gameDir in gameDirs)
                        {
                            string[] exes = Directory.GetFiles(gameDir, "*.exe", SearchOption.AllDirectories);
                            if (exes.Length != 0)
                            {

                                Game game = new Game
                                {
                                    Name = new DirectoryInfo(RemoveSpecialWordsAndChars(gameDir)).Name,
                                    Platform = libDir.PlatformType,
                                    Executables = new List<string>(exes),
                                    InstallPath = gameDir
                                };

                                games.Add(game);


                            }
                        }

                    }
                }

            }
            return games;
        }

    }
}
