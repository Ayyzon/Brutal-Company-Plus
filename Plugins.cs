﻿using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using UnityEngine;
using BrutalCompanyPlus.HarmPatches;
using BrutalCompanyPlus.BCP;
using BepInEx.Configuration;
using static BrutalCompanyPlus.BCP.MyPluginInfo;
using System.IO;

namespace BrutalCompanyPlus
{

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        // Configuration for Factory Enemies Spawn Chance
        public static ConfigEntry<float> FactoryStartOfDaySpawnChance { get; private set; }
        public static ConfigEntry<float> FactoryMidDaySpawnChance { get; private set; }
        public static ConfigEntry<float> FactoryEndOfDaySpawnChance { get; private set; }

        // Configuration for Outside Enemies Spawn Chance
        public static ConfigEntry<float> OutsideStartOfDaySpawnChance { get; private set; }
        public static ConfigEntry<float> OutsideMidDaySpawnChance { get; private set; }
        public static ConfigEntry<float> OutsideEndOfDaySpawnChance { get; private set; }

        // Configuration for Moon Heat Settings
        public static ConfigEntry<float> MoonHeatDecreaseRate { get; private set; }
        public static ConfigEntry<float> MoonHeatIncreaseRate { get; private set; }

        // Configuration for Object Spawn Rate
        public static ConfigEntry<bool> EnableTurretModifications { get; private set; }
        public static ConfigEntry<bool> EnableLandmineModifications { get; private set; }
        public static ConfigEntry<float> TurretSpawnRate { get; private set; }
        public static ConfigEntry<float> LandmineSpawnRate { get; private set; }

        // Configuration for Factory Enemy Rarity
        public static ConfigEntry<string> ExperimentationLevelRarities { get; private set; }
        public static ConfigEntry<string> AssuranceLevelRarities { get; private set; }
        public static ConfigEntry<string> VowLevelRarities { get; private set; }
        public static ConfigEntry<string> MarchLevelRarities { get; private set; }
        public static ConfigEntry<string> RendLevelRarities { get; private set; }
        public static ConfigEntry<string> DineLevelRarities { get; private set; }
        public static ConfigEntry<string> OffenseLevelRarities { get; private set; }
        public static ConfigEntry<string> TitanLevelRarities { get; private set; }

        // Configuration for Free Money
        public static ConfigEntry<bool> EnableFreeMoney { get; private set; }
        public static ConfigEntry<int> FreeMoneyValue { get; private set; }

        // Configuration for Adding All enemies to Enemy List
        public static ConfigEntry<bool> EnableAllEnemy { get; private set; }

        // Configuration for Starting Quota Values
        public static ConfigEntry<int> DeadlineDaysAmount { get; private set; }
        public static ConfigEntry<int> StartingCredits { get; private set; }
        public static ConfigEntry<int> StartingQuota { get; private set; }
        public static ConfigEntry<float> BaseIncrease { get; private set; }

        // Configuration for Scrap Settings
        public static ConfigEntry<int> MinScrap { get; private set; }
        public static ConfigEntry<int> MaxScrap { get; private set; }
        public static ConfigEntry<int> MinTotalScrapValue { get; private set; }
        public static ConfigEntry<int> MaxTotalScrapValue { get; private set; }

        // Configuration for Event Chance
        public static Dictionary<EventEnum, ConfigEntry<int>> eventWeightEntries = new Dictionary<EventEnum, ConfigEntry<int>>();

        private bool brutalPlusInitialized = false;

        void Awake()
        {
            InitializeVariables();
            SetupPatches();
            InitializeBCP_ConfigSettings();
            InitializeDefaultEnemyRarities();

            //Depreciated Config Elements
            UpdateConfigurations();
            RemoveConfigSection(configFilePath, "EventEnabledConfig");
        }

        void Start()
        {
            if (!brutalPlusInitialized)
            {
                CreateBrutalPlusGameObject();
                brutalPlusInitialized = true;
            }
        }

        private void OnDestroy()
        {
            if (!brutalPlusInitialized)
            {
                CreateBrutalPlusGameObject();
                brutalPlusInitialized = true;
            }
        }

        public static List<string> correctEnemyNames = new List<string>
        {
            "Centipede", "Bunker Spider", "Hoarding bug", "Flowerman", "Crawler", "Blob", "Girl", "Puffer", "Nutcracker", "Spring", "Jester", "Masked", "LassoMan"
        };

        public static void UpdateConfigurations()
        {
            List<ConfigEntry<string>> levelConfigs = new List<ConfigEntry<string>>
            {
                ExperimentationLevelRarities, AssuranceLevelRarities, VowLevelRarities, MarchLevelRarities,
                RendLevelRarities, DineLevelRarities, OffenseLevelRarities, TitanLevelRarities
            };

            foreach (var configEntry in levelConfigs)
            {
                var existingConfigValues = ParseConfigRarities(configEntry.Value);
                var updatedConfigValues = new List<string>();

                foreach (var correctName in correctEnemyNames)
                {
                    string rarityValue = existingConfigValues.TryGetValue(correctName, out var existingRarity) ? existingRarity : "-1";
                    updatedConfigValues.Add($"{correctName}:{rarityValue}");
                }

                string updatedConfigValue = string.Join(",", updatedConfigValues);
                if (configEntry.Value != updatedConfigValue)
                {
                    configEntry.Value = updatedConfigValue;
                    BcpLogger.Log($"Updated configuration for {configEntry.Definition.Section}, {configEntry.Definition.Key}: {updatedConfigValue}");
                }
            }
        }

        private static Dictionary<string, string> ParseConfigRarities(string configValue)
        {
            return configValue.Split(',')
                              .Select(part => part.Split(':'))
                              .Where(parts => parts.Length == 2)
                              .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());
        }

        string configFilePath = Path.Combine(BepInEx.Paths.ConfigPath, "BrutalCompanyPlus.cfg");
        public void RemoveConfigSection(string filePath, string sectionName)
        {
            try
            {
                var lines = File.ReadAllLines(filePath).ToList();
                var newLines = new List<string>();
                bool inSection = false;

                foreach (var line in lines)
                {
                    if (line.Trim().Equals($"[{sectionName}]"))
                    {
                        inSection = true; // Start skipping lines
                        continue;
                    }

                    if (inSection && line.StartsWith("[")) // Detect start of a new section
                    {
                        inSection = false; // End of target section, stop skipping
                    }

                    if (!inSection)
                    {
                        newLines.Add(line); // Keep line if it's not in the section
                    }
                }

                File.WriteAllLines(filePath, newLines);
            }
            catch (Exception ex)
            {
                BcpLogger.Log($"Error occurred: {ex.Message}");
            }
        }

        public static void InitializeDefaultEnemyRarities()
        {
            // Initialize default rarities for ExperimentationLevel
            var experimentationLevelRarities = new Dictionary<string, int>
            {
                { "Centipede", 50 }, { "Bunker Spider", 25 }, { "Hoarding bug", 30 }, { "Flowerman", 30 }, { "Crawler", 20 }, { "Blob", 25 }, { "Girl", 2 },
                { "Puffer", 40 }, { "Nutcracker", 25 }, { "Spring", 25 }, { "Jester", 3 }, { "Masked", 5 }, { "LassoMan", 1 }
            };
            Variables.DefaultEnemyRarities["ExperimentationLevel"] = experimentationLevelRarities;

            // Initialize default rarities for AssuranceLevel
            var assuranceLevelRarities = new Dictionary<string, int>
            {
                { "Centipede", 50 }, { "Bunker Spider", 40 }, { "Hoarding bug", 50 }, { "Flowerman", 30 }, { "Crawler", 20 }, { "Blob", 25 }, { "Girl", 2 },
                { "Puffer", 40 }, { "Nutcracker", 25 }, { "Spring", 25 }, { "Jester", 3 }, { "Masked", 10 }, { "LassoMan", 1 }
            };
            Variables.DefaultEnemyRarities["AssuranceLevel"] = assuranceLevelRarities;

            // Initialize default rarities for "VowLevel": 
            var vowLevelRarities = new Dictionary<string, int>
            {
                { "Centipede", 50 }, { "Bunker Spider", 40 }, { "Hoarding bug", 50 }, { "Flowerman", 30 }, { "Crawler", 20 }, { "Blob", 25 }, { "Girl", 10 },
                { "Puffer", 40 }, { "Nutcracker", 25 }, { "Spring", 40 }, { "Jester", 15 }, { "Masked", 20 }, { "LassoMan", 0 }
            };
            Variables.DefaultEnemyRarities["VowLevel"] = vowLevelRarities;

            // Initialize default rarities for "OffenseLevel": 
            var offenseLevelRarities = new Dictionary<string, int>
            {
                { "Centipede", 50 }, { "Bunker Spider", 40 }, { "Hoarding bug", 50 }, { "Flowerman", 30 }, { "Crawler", 20 }, { "Blob", 25 }, { "Girl", 10 },
                { "Puffer", 40 }, { "Nutcracker", 25 }, { "Spring", 40 }, { "Jester", 15 }, { "Masked", 20 }, { "LassoMan", 0 }
            };
            Variables.DefaultEnemyRarities["OffenseLevel"] = offenseLevelRarities;

            // Initialize default rarities for "MarchLevel": 
            var marchLevelRarities = new Dictionary<string, int>
            {
                { "Centipede", 50 }, { "Bunker Spider", 40 }, { "Hoarding bug", 50 }, { "Flowerman", 30 }, { "Crawler", 20 }, { "Blob", 25 }, { "Girl", 10 },
                { "Puffer", 40 }, { "Nutcracker", 25 }, { "Spring", 40 }, { "Jester", 15 }, { "Masked", 20 }, { "LassoMan", 0 }
            };
            Variables.DefaultEnemyRarities["MarchLevel"] = marchLevelRarities;

            // Initialize default rarities for "RendLevel": 
            var rendLevelRarities = new Dictionary<string, int>
            {
                { "Centipede", 35 }, { "Bunker Spider", 25 }, { "Hoarding bug", 30 }, { "Flowerman", 56 }, { "Crawler", 60 }, { "Blob", 40 }, { "Girl", 50 },
                { "Puffer", 40 }, { "Nutcracker", 60 }, { "Spring", 58 }, { "Jester", 57 }, { "Masked", 50 }, { "LassoMan", 2 }
            };
            Variables.DefaultEnemyRarities["RendLevel"] = rendLevelRarities;

            // Initialize default rarities for "DineLevel": 
            var dineLevelRarities = new Dictionary<string, int>
            {
                { "Centipede", 35 }, { "Bunker Spider", 25 }, { "Hoarding bug", 30 }, { "Flowerman", 56 }, { "Crawler", 60 }, { "Blob", 40 }, { "Girl", 50 },
                { "Puffer", 40 }, { "Nutcracker", 60 }, { "Spring", 58 }, { "Jester", 57 }, { "Masked", 50 }, { "LassoMan", 2 }
            };
            Variables.DefaultEnemyRarities["DineLevel"] = dineLevelRarities;

            // Initialize default rarities for "TitanLevel": 
            var titanLevelRarities = new Dictionary<string, int>
            {
                { "Centipede", 35 }, { "Bunker Spider", 25 }, { "Hoarding bug", 30 }, { "Flowerman", 56 }, { "Crawler", 60 }, { "Blob", 40 }, { "Girl", 50 },
                { "Puffer", 40 }, { "Nutcracker", 60 }, { "Spring", 58 }, { "Jester", 57 }, { "Masked", 50 }, { "LassoMan", 2 }
            };
            Variables.DefaultEnemyRarities["TitanLevel"] = titanLevelRarities;
        }

        public void InitializeBCP_ConfigSettings()
        {
            // Configuration for Factory Enemies
            FactoryStartOfDaySpawnChance = Config.Bind("EnemySpawnSettings.Factory", "StartOfDaySpawnChance", -1f, "Factory enemy spawn chance at the start of the day. Set to -1 to use Brutals default value. (vanilla is around 2-5 depending on moon)");
            FactoryMidDaySpawnChance = Config.Bind("EnemySpawnSettings.Factory", "MidDaySpawnChance", -1f, "Factory enemy spawn chance at midday. Set to -1 to use Brutals default value. (vanilla is around 5-10 depending on moon)");
            FactoryEndOfDaySpawnChance = Config.Bind("EnemySpawnSettings.Factory", "EndOfDaySpawnChance", -1f, "Factory enemy spawn chance at the end of the day. Set to -1 to use Brutals default value. (vanilla is around 10-15 depending on moon)");

            // Configuration for Outside Enemies
            OutsideStartOfDaySpawnChance = Config.Bind("EnemySpawnSettings.Outside", "StartOfDaySpawnChance", -1f, "Outside enemy spawn chance at the start of the day. Set to -1 to use default value. (vanilla is 0)");
            OutsideMidDaySpawnChance = Config.Bind("EnemySpawnSettings.Outside", "MidDaySpawnChance", -1f, "Outside enemy spawn chance at midday. Set to -1 to use default value. (vanilla is 0.5)");
            OutsideEndOfDaySpawnChance = Config.Bind("EnemySpawnSettings.Outside", "EndOfDaySpawnChance", -1f, "Outside enemy spawn chance at the end of the day. Set to -1 to use default value. (vanilla is 5)");

            MoonHeatDecreaseRate = Config.Bind("MoonHeatSettings", "MoonHeatDecreaseRate", 10f, "Amount by which moon heat decreases when not visiting the planet");
            MoonHeatIncreaseRate = Config.Bind("MoonHeatSettings", "MoonHeatIncreaseRate", 20f, "Amount by which moon heat increases when landing back on the same planet");

            EnableTurretModifications = Config.Bind("MapObjectModificationSettings", "EnableTurretModifications", true, "Enable modifications to turret spawn rates on every moon, False would default to game logic");
            TurretSpawnRate = Config.Bind("MapObjectModificationSettings", "TurretSpawnRate", 8f, "Default spawn amount for turrets on every moon");
            EnableLandmineModifications = Config.Bind("MapObjectModificationSettings", "EnableLandmineModifications", true, "Enable modifications to landmine spawn rates on every moon, False would default to game logic");
            LandmineSpawnRate = Config.Bind("MapObjectModificationSettings", "LandmineSpawnRate", 30f, "Default spawn amount for landmines on every moon");

            ExperimentationLevelRarities = Config.Bind("CustomLevelRarities", "Experimentation", "Centipede:-1,Bunker Spider:-1,Hoarding bug:-1,Flowerman:-1,Crawler:-1,Blob:-1,Girl:-1,Puffer:-1,Nutcracker:-1,Spring:-1,Jester:-1,Masked:-1,LassoMan:-1", "Define custom enemy rarities for Experimentation (0 = no spawn, 100 = max chance, -1 = default Brutals rarity)");
            AssuranceLevelRarities = Config.Bind("CustomLevelRarities", "Assurance", "Centipede:-1,Bunker Spider:-1,Hoarding bug:-1,Flowerman:-1,Crawler:-1,Blob:-1,Girl:-1,Puffer:-1,Nutcracker:-1,Spring:-1,Jester:-1,Masked:-1,LassoMan:-1", "Define custom enemy rarities for Assurance (0 = no spawn, 100 = max chance, -1 = default Brutals rarity)");
            VowLevelRarities = Config.Bind("CustomLevelRarities", "Vow", "Centipede:-1,Bunker Spider:-1,Hoarding bug:-1,Flowerman:-1,Crawler:-1,Blob:-1,Girl:-1,Puffer:-1,Nutcracker:-1,Spring:-1,Jester:-1,Masked:-1,LassoMan:-1", "Define custom enemy rarities for Vow (0 = no spawn, 100 = max chance, -1 = default Brutals rarity)");
            MarchLevelRarities = Config.Bind("CustomLevelRarities", "March", "Centipede:-1,Bunker Spider:-1,Hoarding bug:-1,Flowerman:-1,Crawler:-1,Blob:-1,Girl:-1,Puffer:-1,Nutcracker:-1,Spring:-1,Jester:-1,Masked:-1,LassoMan:-1", "Define custom enemy rarities for March (0 = no spawn, 100 = max chance, -1 = default Brutals rarity)");
            RendLevelRarities = Config.Bind("CustomLevelRarities", "Rend", "Centipede:-1,Bunker Spider:-1,Hoarding bug:-1,Flowerman:-1,Crawler:-1,Blob:-1,Girl:-1,Puffer:-1,Nutcracker:-1,Spring:-1,Jester:-1,Masked:-1,LassoMan:-1", "Define custom enemy rarities for Rend (0 = no spawn, 100 = max chance, -1 = default Brutals rarity)");
            DineLevelRarities = Config.Bind("CustomLevelRarities", "Dine", "Centipede:-1,Bunker Spider:-1,Hoarding bug:-1,Flowerman:-1,Crawler:-1,Blob:-1,Girl:-1,Puffer:-1,Nutcracker:-1,Spring:-1,Jester:-1,Masked:-1,LassoMan:-1", "Define custom enemy rarities for Dine (0 = no spawn, 100 = max chance, -1 = default Brutals rarity)");
            OffenseLevelRarities = Config.Bind("CustomLevelRarities", "Offense", "Centipede:-1,Bunker Spider:-1,Hoarding bug:-1,Flowerman:-1,Crawler:-1,Blob:-1,Girl:-1,Puffer:-1,Nutcracker:-1,Spring:-1,Jester:-1,Masked:-1,LassoMan:-1", "Define custom enemy rarities for Offense (0 = no spawn, 100 = max chance, -1 = default Brutals rarity)");
            TitanLevelRarities = Config.Bind("CustomLevelRarities", "Titan", "Centipede:-1,Bunker Spider:-1,Hoarding bug:-1,Flowerman:-1,Crawler:-1,Blob:-1,Girl:-1,Puffer:-1,Nutcracker:-1,Spring:-1,Jester:-1,Masked:-1,LassoMan:-1", "Define custom enemy rarities for Titan (0 = no spawn, 100 = max chance, -1 = default Brutals rarity)");

            EnableFreeMoney = Config.Bind("EventOptions", "EnableFreeMoney", true, "This will give free money everytime survive and escape the planet");
            FreeMoneyValue = Config.Bind("EventOptions", "FreeMoneyValue", 150, "This will control the amount of money you get when EnableFreeMoney is true");

            EnableAllEnemy = Config.Bind("EnemySettings", "EnableAllEnemy", true, "This will add every enemy type to each moon as a spawn chance");

            DeadlineDaysAmount = Config.Bind("QuotaSettings", "DeadlineDaysAmount", 4, "Days available before deadline");
            StartingCredits = Config.Bind("QuotaSettings", "StartingCredits", 200, "Credits at the start of a new session");
            StartingQuota = Config.Bind("QuotaSettings", "StartingQuota", 400, "Starting quota amount in a new session");
            BaseIncrease = Config.Bind("QuotaSettings", "BaseIncrease", 275f, "Quota increase after meeting the previous quota");

            MinScrap = Config.Bind("ScrapSettings", "MinScrap", 15, "Minimum scraps that can spawn on each moon");
            MaxScrap = Config.Bind("ScrapSettings", "MaxScrap", 75, "Maximum scraps that can spawn on each moon");
            MinTotalScrapValue = Config.Bind("ScrapSettings", "MinTotalScrapValue", 1500, "Minimum total scrap value on the moon");
            MaxTotalScrapValue = Config.Bind("ScrapSettings", "MaxTotalScrapValue", 5000, "Maximum total scrap value on the moon");

            eventWeightEntries[EventEnum.None] = Config.Bind("EventChanceConfig", "None", 50, "[Nothing Happened Today] Nothing special will happen (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.Turret] = Config.Bind("EventChanceConfig", "Turret", 50, "[Turret Terror] This will spawn turrets all over the place inside the factory (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.Delivery] = Config.Bind("EventChanceConfig", "Delivery", 50, "[ICE SCREAM] This will order between 3 - 9 random items from the shop (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.BlobEvolution] = Config.Bind("EventChanceConfig", "BlobEvolution", 50, "[They have EVOLVED] This will spawn only Blobs and they can open doors and move much faster (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.Chaos] = Config.Bind("EventChanceConfig", "Chaos", 50, "[CHAOS COMPANY] This will increase the spawn rates of enemyies inside the factory at a significant rate (Set Chance between 0 -100)");
            eventWeightEntries[EventEnum.SurfaceExplosion] = Config.Bind("EventChanceConfig", "SurfaceExplosion", 50, "[The Surface is explosive] Mines wills spawn at the feet of players not in the ship or factory, they also have a delayed fuse (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.FaceHuggers] = Config.Bind("EventChanceConfig", "FaceHuggers", 50, "[Internecivus Raptus?] This will ONLY spawn MANY Centipedes into the factory (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.TheRumbling] = Config.Bind("EventChanceConfig", "TheRumbling", 50, "[The Rumbling] This will spawn MANY Forest Giants when the ship has fully landed (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.TheyWant2Play] = Config.Bind("EventChanceConfig", "TheyWant2Play", 50, "[The just want to play] This will spawn several Ghost girls into the level (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.BeastInside] = Config.Bind("EventChanceConfig", "BeastInside", 50, "[The Beasts Inside] This will spawn ONLY Eyeless Dogs into the Factory, spawn rate changes depending on moon (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.Unfair] = Config.Bind("EventChanceConfig", "Unfair", 50, "[This is just unfair] This will spawn several outside enemies and inside enemies at a significant rate (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.BadPlanet] = Config.Bind("EventChanceConfig", "BadPlanet", 50, "[This planet is NOT safe] This will trigger several events, SurfaceExplosion, InstaJester, BlobEvolution, ShipTurret, Unfair (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.InstaJester] = Config.Bind("EventChanceConfig", "InstaJester", 50, "[Pop goes the... HOLY FUC-] This will spawn several jesters that have a short crank timer between 0 - 10 seconds instead of 30 - 45 seconds (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.InsideOut] = Config.Bind("EventChanceConfig", "InsideOut", 50, "[Inside Out!] This will spawn 4 Coil heads outside, they will instantly roam around the ship (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.Landmine] = Config.Bind("EventChanceConfig", "Landmine", 50, "[Minescape Terror] This will spawn MANY landmines inside the factory (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.Sacrifice] = Config.Bind("EventChanceConfig", "Sacrifice", 50, "[The Hunger Games?] This will rotate through players at a given rate, when the selected player steps inside the factory.. They get choosen for death. (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.ShipTurret] = Config.Bind("EventChanceConfig", "ShipTurret", 50, "[When did we get this installed?!?] This will spawn a turret inside the ship facing the controls (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.HoardTown] = Config.Bind("EventChanceConfig", "HoardTown", 50, "[Hoarder Town] This will ONLY spawn MANY Hoarder Bugs inside the factory (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.TheyAreShy] = Config.Bind("EventChanceConfig", "TheyAreShy", 50, "[They are shy!] This will ONLY spawn several Spring Heads and Brackens inside the factory (Set Chance between 0 - 100)");
            eventWeightEntries[EventEnum.ResetHeat] = Config.Bind("EventChanceConfig", "ResetHeat", 50, "[All Moons Heat Reset] This will reset the moon heat for all moons (Set Chance between 0 - 100)");
        }

        private void InitializeVariables()
        {
            Variables.mls = BepInEx.Logging.Logger.CreateLogSource(PLUGIN_NAME);

            Variables.mls.LogWarning("Loaded Brutal Company Plus and applying patches.");

            Variables.mls = base.Logger;

            Variables.levelHeatVal = new Dictionary<SelectableLevel, float>();

            Variables.enemyRaritys = new Dictionary<SpawnableEnemyWithRarity, int>();

            Variables.levelEnemySpawns = new Dictionary<SelectableLevel, List<SpawnableEnemyWithRarity>>();

            Variables.enemyPropCurves = new Dictionary<SpawnableEnemyWithRarity, AnimationCurve>();

        }

        private void SetupPatches()
        {
            Variables._harmony.PatchAll(typeof(LevelEventsPatches));

            Variables._harmony.PatchAll(typeof(QuotaPatches));

            Variables._harmony.PatchAll(typeof(EnemyAIPatches));
        }

        private void CreateBrutalPlusGameObject()
        {
            try
            {
                Variables.mls.LogInfo("Attempting to Initialize BrutalPlus");
                GameObject brutalplus = new GameObject("BrutalPlus");

                // Make sure the GameObject is active
                brutalplus.SetActive(true);

                BrutalPlus brutalPlusComponent = brutalplus.AddComponent<BrutalPlus>();

                // Ensure the component is enabled
                brutalPlusComponent.enabled = true;

                // Prevent the GameObject from being destroyed on load
                DontDestroyOnLoad(brutalplus);

                Variables.loaded = true;
            }
            catch (Exception ex)
            {
                Variables.mls.LogInfo("Exception in CreateBrutalPlusGameObject: " + ex.Message);
                BcpLogger.Log("Exception in CreateBrutalPlusGameObject: " + ex.Message);
            }
        }

        public static void CleanupAndResetAI()
        {
            Functions.CleanUpAllVariables();
            BrutalPlus.CleanupAllSpawns();
        }

        public static void UpdateAIInNewLevel(SelectableLevel newLevel)
        {
            foreach (var enemy in newLevel.Enemies)
            {
                var enemyPrefab = enemy.enemyType.enemyPrefab;
                foreach (var aiType in Variables.aiPresence.Keys.ToList())
                {
                    if (enemyPrefab.GetComponent(aiType) != null)
                    {
                        Variables.aiPresence[aiType] = true;
                    }
                }
            }
        }

        public static void AddMissingAIToNewLevel(SelectableLevel newLevel)
        {
            //Config value to setup all enemies inside
            if (EnableAllEnemy.Value)
            {
                SelectableLevel[] levels = StartOfRound.Instance.levels;
                foreach (var level in levels)
                {
                    foreach (var spawnable in level.Enemies)
                    {
                        var enemyPrefab = spawnable.enemyType.enemyPrefab;
                        foreach (var aiType in Variables.aiPresence.Keys.ToList())
                        {
                            if (enemyPrefab.GetComponent(aiType) != null && !Variables.aiPresence[aiType])
                            {
                                Variables.aiPresence[aiType] = true;
                                newLevel.Enemies.Add(spawnable);
                                Variables.mls.LogWarning($"\n\nAdded Enemy: > {spawnable.enemyType.enemyPrefab.name} < to the Enemy list\n\n");
                                //BcpLogger.Log($"Added Enemy: > {spawnable.enemyType.enemyPrefab.name} < to the Enemy list");
                            }
                        }
                    }
                }
            }
        }

        public static EventEnum SelectRandomEvent()
        {
            var weightedEvents = eventWeightEntries
                .Where(kvp => kvp.Value.Value > 0) // Only consider events with weight > 0
                .Select(kvp => new { Event = kvp.Key, Weight = kvp.Value.Value })
                .ToList();

            // Remove the last event if there are other options
            if (weightedEvents.Count > 1)
            {
                weightedEvents = weightedEvents.Where(e => e.Event != Variables.lastEvent).ToList();
            }

            if (weightedEvents.Count == 0)
            {
                return EventEnum.None;
            }

            int totalWeight = weightedEvents.Sum(e => e.Weight);
            int randomNumber = UnityEngine.Random.Range(0, totalWeight);
            int weightSum = 0;

            foreach (var weightedEvent in weightedEvents)
            {
                weightSum += weightedEvent.Weight;
                if (randomNumber <= weightSum)
                {
                    Variables.lastEvent = weightedEvent.Event; // Update the last event
                    return weightedEvent.Event;
                }
            }

            return EventEnum.None; // Fallback, should not normally reach here
        }


        public static void ApplyCustomLevelEnemyRarities(List<SpawnableEnemyWithRarity> enemies, string levelName)
        {
            try
            {
                BcpLogger.Log($"Applying custom rarities for level: {levelName}");

                var levelConfig = GetConfigForLevel(levelName);
                BcpLogger.Log($"Config for level {levelName}: {levelConfig?.Value}");

                if (levelConfig == null || string.IsNullOrWhiteSpace(levelConfig.Value))
                {
                    BcpLogger.Log($"No custom config found or config is empty for level {levelName}. Using default rarities.");
                    return;
                }

                var customRarities = levelConfig.Value.Split(',')
                                                    .Select(r => r.Split(':'))
                                                    .Where(r => r.Length == 2)
                                                    .ToDictionary(r => r[0].Trim(), r => int.Parse(r[1]));

                if (!Variables.DefaultEnemyRarities.TryGetValue(levelName, out var levelDefaultRarities))
                {
                    BcpLogger.Log($"No default rarities found for level {levelName}");
                    return;
                }

                foreach (var enemy in enemies)
                {
                    if (customRarities.TryGetValue(enemy.enemyType.enemyName, out int customRarity))
                    {
                        if (customRarity != -1)
                        {
                            BcpLogger.Log($"Setting custom rarity for {enemy.enemyType.enemyName} to {customRarity}");
                            enemy.rarity = customRarity;
                        }
                        else if (levelDefaultRarities.TryGetValue(enemy.enemyType.enemyName, out int defaultRarity))
                        {
                            BcpLogger.Log($"Using default rarity for {enemy.enemyType.enemyName}: {defaultRarity}");
                            enemy.rarity = defaultRarity;
                        }
                    }
                    else if (levelDefaultRarities.TryGetValue(enemy.enemyType.enemyName, out int defaultRarity))
                    {
                        BcpLogger.Log($"No custom rarity for {enemy.enemyType.enemyName}. Using default rarity: {defaultRarity}");
                        enemy.rarity = defaultRarity;
                    }
                    else
                    {
                        BcpLogger.Log($"No rarity information found for {enemy.enemyType.enemyName}. Skipping.");
                    }
                }
            }
            catch (Exception e)
            {
                BcpLogger.Log($"Exception in ApplyCustomLevelEnemyRarities: {e.ToString()}");
            }
        }

        private static ConfigEntry<string> GetConfigForLevel(string levelName)
        {
            switch (levelName)
            {
                case "ExperimentationLevel": return ExperimentationLevelRarities;
                case "AssuranceLevel": return AssuranceLevelRarities;
                case "VowLevel": return VowLevelRarities;
                case "MarchLevel": return MarchLevelRarities;
                case "RendLevel": return RendLevelRarities;
                case "DineLevel": return DineLevelRarities;
                case "OffenseLevel": return OffenseLevelRarities;
                case "TitanLevel": return TitanLevelRarities;
                default: return null;
            }
        }

        public static void UpdateLevelEnemies(SelectableLevel newLevel, float MoonHeat)
        {
            List<SpawnableEnemyWithRarity> enemies;
            if (Variables.levelEnemySpawns.TryGetValue(newLevel, out enemies))
            {
                ApplyCustomLevelEnemyRarities(enemies, newLevel.name);
                newLevel.Enemies = enemies;
            }
        }

        public static void InitializeLevelHeatValues(SelectableLevel newLevel)
        {
            if (!Variables.levelHeatVal.ContainsKey(newLevel))
            {
                Variables.levelHeatVal.Add(newLevel, 0f);
            }

            if (!Variables.levelEnemySpawns.ContainsKey(newLevel))
            {
                List<SpawnableEnemyWithRarity> list = new List<SpawnableEnemyWithRarity>();
                foreach (SpawnableEnemyWithRarity item in newLevel.Enemies)
                {
                    list.Add(item);
                }
                Variables.levelEnemySpawns.Add(newLevel, list);
            }
        }

        public static void AdjustHeatValuesForAllLevels(EventEnum eventEnum, SelectableLevel newLevel)
        {
            foreach (SelectableLevel level in Variables.levelHeatVal.Keys.ToList<SelectableLevel>())
            {
                if (newLevel != level)
                {
                    float HeatValue;
                    Variables.levelHeatVal.TryGetValue(level, out HeatValue);
                    Variables.levelHeatVal[level] = Mathf.Clamp(HeatValue - MoonHeatDecreaseRate.Value, 0f, 100f);
                }

                if (eventEnum == EventEnum.ResetHeat || eventEnum == EventEnum.BadPlanet)
                {
                    Variables.levelHeatVal[level] = 0f;
                }
            }
        }

        public static float DisplayHeatMessages(SelectableLevel newLevel)
        {
            float HeatValue;
            Variables.levelHeatVal.TryGetValue(newLevel, out HeatValue);
            HUDManager.Instance.AddTextToChatOnServer("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n", -1);
            HUDManager.Instance.AddTextToChatOnServer("<color=orange>MOON IS AT " + HeatValue.ToString() + "% HEAT</color>", -1);
            if (HeatValue >= 30f)
            {
                HUDManager.Instance.AddTextToChatOnServer("<size=10><color=red>Your Moon Heat is Getting HIGH (Things might starting spawning). <color=white>\nVisit other moons to decrease your moon heat!</color></size>", -1);
            }
            return HeatValue;
        }

        public static void HandleEventSpecificAdjustments(EventEnum eventEnum, SelectableLevel newLevel)
        {

            if (newLevel.sceneName == "CompanyBuilding")
            {
                eventEnum = EventEnum.None;
            }

            switch (eventEnum)
            {
                case EventEnum.None:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=green>Nothing Happened Today!</color>", -1);
                    break;


                case EventEnum.Turret:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>Turret Terror</color>", -1);
                    break;


                case EventEnum.Landmine:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>Minescape Terror</color>", -1);
                    break;


                case EventEnum.InsideOut:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>Inside Out!</color>", -1);
                    Variables.SpawnInsideOut = true;
                    break;


                //case EventEnum.SmiteMe:
                //    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>He's angry with you</color>", -1);
                //    newLevel.currentWeather = LevelWeatherType.Stormy;
                //    Variables.SmiteEnabled = true;
                //    break;


                case EventEnum.HoardTown:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>Hoarder Town</color>", -1);
                    Variables.presetEnemiesToSpawn.Add(new Variables.EnemySpawnInfo(typeof(HoarderBugAI), 15, SpawnLocation.Vent, false, false));

                    Functions.FindEnemyPrefabByType(typeof(HoarderBugAI), newLevel.Enemies, newLevel);

                    foreach (var spawnableEnemy in newLevel.Enemies)
                    {
                        spawnableEnemy.rarity = 0;
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<HoarderBugAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                    }
                    break;


                case EventEnum.BeastInside:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>The Beasts Inside!</color>", -1);
                    Functions.TheBeastsInside();
                    foreach (var spawnableEnemy in newLevel.Enemies)
                    {
                        spawnableEnemy.rarity = 0;
                    }
                    break;


                case EventEnum.TheRumbling:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>The Rumbling!</color>", -1);
                    Functions.TheRumbling();
                    Variables.TheRumbling = true;
                    break;


                case EventEnum.Sacrifice:
                    HUDManager.Instance.AddTextToChatOnServer($"<color=yellow>EVENT<color=white>:</color></color>\n<color=red>The Hunger Games?</color>", -1);
                    Variables.Tribute = true;
                    break;


                case EventEnum.BadPlanet:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>This planet is NOT friendly!!!</color>", -1);

                    Functions.FindEnemyPrefabByType(typeof(CentipedeAI), newLevel.Enemies, newLevel);
                    Functions.FindEnemyPrefabByType(typeof(JesterAI), newLevel.Enemies, newLevel);
                    Functions.FindEnemyPrefabByType(typeof(DressGirlAI), newLevel.Enemies, newLevel);
                    Functions.FindEnemyPrefabByType(typeof(SpringManAI), newLevel.Enemies, newLevel);
                    Functions.FindEnemyPrefabByType(typeof(BlobAI), newLevel.Enemies, newLevel);

                    foreach (var spawnableEnemy in newLevel.Enemies)
                    {
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<CentipedeAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<JesterAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<DressGirlAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<SpringManAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<BlobAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                    }

                    Variables.InstaJester = true;
                    Variables.BlobsHaveEvolved = true;
                    Variables.shouldSpawnTurret = true;
                    Variables.slSpawnTimer = -10f;
                    Variables.surpriseLandmines += 120;

                    break;

                case EventEnum.Chaos:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>CHAOS COMPANY</color>", -1);

                    Functions.FindEnemyPrefabByType(typeof(CentipedeAI), newLevel.Enemies, newLevel);
                    Functions.FindEnemyPrefabByType(typeof(JesterAI), newLevel.Enemies, newLevel);
                    Functions.FindEnemyPrefabByType(typeof(DressGirlAI), newLevel.Enemies, newLevel);
                    Functions.FindEnemyPrefabByType(typeof(SpringManAI), newLevel.Enemies, newLevel);
                    Functions.FindEnemyPrefabByType(typeof(BlobAI), newLevel.Enemies, newLevel);

                    foreach (var spawnableEnemy in newLevel.Enemies)
                    {
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<CentipedeAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<JesterAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<DressGirlAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<SpringManAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<BlobAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                    }
                    break;


                case EventEnum.Unfair:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>That's just unfair</color>", -1);
                    break;


                case EventEnum.FaceHuggers:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>Internecivus Raptus?</color>", -1);
                    Variables.WaitUntilPlayerInside = true;
                    Functions.FindEnemyPrefabByType(typeof(CentipedeAI), newLevel.Enemies, newLevel);

                    Variables.presetEnemiesToSpawn.Add(new Variables.EnemySpawnInfo(typeof(CentipedeAI), 15, SpawnLocation.Vent, false, false));

                    foreach (var spawnableEnemy in newLevel.Enemies)
                    {
                        spawnableEnemy.rarity = 0;
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<CentipedeAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                    }
                    break;


                case EventEnum.BlobEvolution:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>They are EVOLVING?!?</color>", -1);

                    Functions.FindEnemyPrefabByType(typeof(BlobAI), newLevel.Enemies, newLevel);

                    foreach (var spawnableEnemy in newLevel.Enemies)
                    {
                        spawnableEnemy.rarity = 0;

                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<BlobAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                    }
                    Variables.BlobsHaveEvolved = true;
                    break;


                case EventEnum.Delivery:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=green>ICE SCREAM!!!</color>", -1);
                    int RandomAmount = UnityEngine.Random.Range(2, 9);
                    for (int i = 0; i < RandomAmount; i++)
                    {
                        int RandomItem = UnityEngine.Random.Range(0, 12);
                        UnityEngine.Object.FindObjectOfType<Terminal>().orderedItemsFromTerminal.Add(RandomItem);
                    }
                    break;


                case EventEnum.InstaJester:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>Pop goes the.. HOLY FUC- </color>", -1);

                    //Ensure or add Jester to Moon
                    Functions.FindEnemyPrefabByType(typeof(JesterAI), newLevel.Enemies, newLevel);

                    //Spawn Jester
                    Variables.presetEnemiesToSpawn.Add(new Variables.EnemySpawnInfo(typeof(JesterAI), 2, SpawnLocation.Vent, false, false));

                    Variables.InstaJester = true;
                    break;


                case EventEnum.TheyAreShy:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>They are shy!</color>", -1);

                    //Ensure or add FlowermanAI to Moon
                    Functions.FindEnemyPrefabByType(typeof(FlowermanAI), newLevel.Enemies, newLevel);

                    foreach (var spawnableEnemy in newLevel.Enemies)
                    {
                        spawnableEnemy.rarity = 0;
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<FlowermanAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                        if (spawnableEnemy.enemyType.enemyPrefab.GetComponent<SpringManAI>() != null)
                        {
                            spawnableEnemy.rarity = 100;
                        }
                    }

                    break;


                case EventEnum.TheyWant2Play:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>They just wants to play!!</color>", -1);

                    //Ensure or add DressGirlAI to Moon
                    Functions.FindEnemyPrefabByType(typeof(DressGirlAI), newLevel.Enemies, newLevel);

                    Variables.presetEnemiesToSpawn.Add(new Variables.EnemySpawnInfo(typeof(DressGirlAI), 5, SpawnLocation.Vent, false, false));
                    break;


                case EventEnum.SurfaceExplosion:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>The surface is explosive</color>", -1);
                    Variables.slSpawnTimer = -10f;
                    Variables.surpriseLandmines += 120;
                    break;


                case EventEnum.ResetHeat:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=green>All Moons Heat Reset</color>", -1);
                    // Logic for Reset Heat event
                    break;


                case EventEnum.ShipTurret:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=red>When did we get this installed?!?</color>", -1);
                    Variables.shouldSpawnTurret = true;
                    break;


                default:
                    HUDManager.Instance.AddTextToChatOnServer("<color=yellow>EVENT<color=white>:</color></color>\n<color=green>Nothing happened today!</color>", -1);
                    break;
            }
        }

        //This adjust landmines and turrets for each level
        public static void UpdateMapObjects(SelectableLevel newLevel, EventEnum eventEnum)
        {
            foreach (SpawnableMapObject spawnableMapObject in newLevel.spawnableMapObjects)
            {
                // Check if the map object is a Turret
                if (spawnableMapObject.prefabToSpawn.GetComponentInChildren<Turret>() != null)
                {
                    Variables.turret = spawnableMapObject.prefabToSpawn;
                    if (eventEnum == EventEnum.Turret || eventEnum == EventEnum.BadPlanet)
                    {
                        spawnableMapObject.numberToSpawn = new AnimationCurve(new Keyframe[]
                        {
                    new Keyframe(0f, 200f),
                    new Keyframe(1f, 25f)
                        });
                    }
                    else if (EnableTurretModifications.Value)
                    {
                        spawnableMapObject.numberToSpawn = new AnimationCurve(new Keyframe[]
                        {
                    new Keyframe(0f, 0f),
                    new Keyframe(1f, TurretSpawnRate.Value)
                        });
                    }
                }
                // Check if the map object is a Landmine
                else if (spawnableMapObject.prefabToSpawn.GetComponentInChildren<Landmine>() != null)
                {
                    Variables.landmine = spawnableMapObject.prefabToSpawn;
                    if (eventEnum == EventEnum.Landmine || eventEnum == EventEnum.BadPlanet)
                    {
                        spawnableMapObject.numberToSpawn = new AnimationCurve(new Keyframe[]
                        {
                    new Keyframe(0f, 300f),
                    new Keyframe(1f, 170f)
                        });
                    }
                    else if (EnableLandmineModifications.Value)
                    {
                        spawnableMapObject.numberToSpawn = new AnimationCurve(new Keyframe[]
                        {
                    new Keyframe(0f, 0f),
                    new Keyframe(1f, LandmineSpawnRate.Value)
                        });
                    }
                }

                // Log the map object for debugging
                Variables.mls.LogInfo(spawnableMapObject.prefabToSpawn.ToString());
            }
        }

        //Log information
        public static void LogEnemyInformation(SelectableLevel newLevel)
        {
            Variables.mls.LogWarning("Map Objects");
            foreach (SpawnableMapObject spawnableMapObject in newLevel.spawnableMapObjects)
            {
                Variables.mls.LogInfo(spawnableMapObject.prefabToSpawn.ToString());
            }
            Variables.mls.LogWarning("Enemies");
            foreach (SpawnableEnemyWithRarity spawnableEnemy in newLevel.Enemies)
            {
                Variables.mls.LogInfo(spawnableEnemy.enemyType.enemyName + "--rarity = " + spawnableEnemy.rarity.ToString());
            }
            // And similarly for Daytime Enemies...
        }

        //This adjust the value and total of items on the map
        public static void UpdateLevelProperties(SelectableLevel newLevel)
        {
            // Add the new level to the modified levels list if not already present
            if (!Variables.levelsModified.Contains(newLevel))
            {
                Variables.levelsModified.Add(newLevel);

                // Adjusting minimum and maximum scrap values based on config
                newLevel.minScrap = Plugin.MinScrap.Value;
                newLevel.maxScrap = Plugin.MaxScrap.Value;

                // Adjusting minimum and maximum total scrap value based on config
                newLevel.minTotalScrapValue = Plugin.MinTotalScrapValue.Value;
                newLevel.maxTotalScrapValue = Plugin.MaxTotalScrapValue.Value;

                newLevel.maxEnemyPowerCount += 70;
                newLevel.maxOutsideEnemyPowerCount += 10;
                newLevel.maxDaytimeEnemyPowerCount += 50;
            }

            // Other level property adjustments can be added here
        }


        public static void ModifyEnemySpawnChances(SelectableLevel newLevel, EventEnum eventEnum, float MoonHeat)
        {
            float scaledMoonHeat = 0;
            if (MoonHeat > 30)
            {
                scaledMoonHeat = (MoonHeat / 100.0f) * 10.0f;
            }
            // Use config values for Factory Enemies, or default if set to -1
            float factoryStartValue = FactoryStartOfDaySpawnChance.Value != -1 ? FactoryStartOfDaySpawnChance.Value : 3f;
            float factoryMidValue = FactoryMidDaySpawnChance.Value != -1 ? FactoryMidDaySpawnChance.Value : 7f;
            float factoryEndValue = FactoryEndOfDaySpawnChance.Value != -1 ? FactoryEndOfDaySpawnChance.Value : 15f;

            newLevel.enemySpawnChanceThroughoutDay = new AnimationCurve(new Keyframe[]
            {
        new Keyframe(0f, factoryStartValue),
        new Keyframe(0.5f, factoryMidValue),
        new Keyframe(1f, factoryEndValue)
            });

            // Use config values for Outside Enemies, or default if set to -1
            float outsideStartValue = OutsideStartOfDaySpawnChance.Value != -1 ? OutsideStartOfDaySpawnChance.Value : -2f;
            float outsideMidValue = OutsideMidDaySpawnChance.Value != -1 ? OutsideMidDaySpawnChance.Value : 0f;
            float outsideEndValue = OutsideEndOfDaySpawnChance.Value != -1 ? OutsideEndOfDaySpawnChance.Value : 5f;

            newLevel.outsideEnemySpawnChanceThroughDay = new AnimationCurve(new Keyframe[]
            {
        new Keyframe(0f, outsideStartValue + scaledMoonHeat),
        new Keyframe(0.5f, outsideMidValue + scaledMoonHeat),
        new Keyframe(1f, outsideEndValue + scaledMoonHeat)
            });

            // Adjust spawn chances based on the event
            switch (eventEnum)
            {
                case EventEnum.Unfair:
                    newLevel.outsideEnemySpawnChanceThroughDay = new AnimationCurve(new Keyframe[]
                    {
                        new Keyframe(0f, 5f),
                        new Keyframe(0.5f, 5f),
                        new Keyframe(1f, 5.3f)
                    });
                    break;

                case EventEnum.BadPlanet:
                    newLevel.maxEnemyPowerCount += 200;
                    newLevel.outsideEnemySpawnChanceThroughDay = new AnimationCurve(new Keyframe[]
                    {
                        new Keyframe(0f, 5f),
                        new Keyframe(0.5f, 5f),
                        new Keyframe(1f, 5.3f)
                    });
                    newLevel.enemySpawnChanceThroughoutDay = new AnimationCurve(new Keyframe[]
                    {
                        new Keyframe(0f, 10),
                        new Keyframe(0.5f, 50),
                        new Keyframe(1f, 100)
                    });
                    break;

                case EventEnum.Chaos:
                    newLevel.maxEnemyPowerCount += 200;
                    newLevel.enemySpawnChanceThroughoutDay = new AnimationCurve(new Keyframe[]
                    {
                        new Keyframe(0f, 10),
                        new Keyframe(0.5f, 50),
                        new Keyframe(1f, 100)
                    });
                    break;

                default:
                    // Default adjustments or no adjustments
                    break;
            }

            // Additional adjustments to spawn chances can be added here
        }

    }
}