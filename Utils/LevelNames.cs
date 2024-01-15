﻿using System.Linq;

namespace BrutalCompanyPlus.Utils;

public static class LevelNames {
    public const string CompanyBuilding = "CompanyBuildingLevel";
    public const string Experimentation = "ExperimentationLevel";
    public const string Assurance = "AssuranceLevel";
    public const string Vow = "VowLevel";
    public const string Offense = "OffenseLevel";
    public const string March = "MarchLevel";
    public const string Rend = "RendLevel";
    public const string Dine = "DineLevel";
    public const string Titan = "TitanLevel";
    internal const string Custom = "???_custom_???";

    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly string[] All = {
        Experimentation,
        Assurance,
        Vow,
        Offense,
        March,
        Rend,
        Dine,
        Titan
    };
    
    // Intended for config validation only. Do not use for anything else.
    internal static readonly string[] AllCustom = All.Concat(new[] { Custom }).ToArray();

    public static bool IsCustom(string LevelName) => !All.Contains(LevelName);
}