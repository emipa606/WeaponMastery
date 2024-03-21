using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;

namespace WeaponMastery;

public class ModSettings : Verse.ModSettings
{
    public static List<int> experiencePerLevel;

    public static int maxLevel = 3;

    public static bool initialLoad;

    public static bool useCustomNames;

    public static bool masteryOnOutsidePawns = true;

    public static bool useMoods;

    public static bool useSpecificMasterySystem = true;

    public static bool useGeneralMasterySystem;

    public static bool displayExperience;

    public static bool KeepOriginalWeaponNameQuality;

    public static float chanceToNameWeapon = 0.35f;

    public static float bondedWeaponExperienceMultipier = 1.5f;

    public static float masteriesPercentagePerEvent = 0.25f;

    public static float eventWeaponNameChance = 0.15f;

    public static int numberOfRelicBonusStats = 5;

    public static Dictionary<string, string> overrideClasses = new Dictionary<string, string>();

    public static readonly Dictionary<ThingDef, string> classes = new Dictionary<ThingDef, string>();

    public static List<MasteryStat> rangedStats;

    public static List<MasteryStat> meleeStats;

    public static List<string> weaponNamesPool;

    public static List<string> customWeaponNamesPool = [];

    public static List<string> messages;

    private static readonly string WEAPON_NAMES_DEF_PATH = Path.Combine("Languages", "English", "WeaponNamesList.txt");

    private static readonly string MOD_PACKAGE_ID = "Sk.WeaponMastery";

    public override void ExposeData()
    {
        initialLoad = true;
        base.ExposeData();
        Scribe_Collections.Look(ref experiencePerLevel, "experienceperlevel", LookMode.Value);
        Scribe_Collections.Look(ref rangedStats, "rangedstats", LookMode.Deep);
        Scribe_Collections.Look(ref meleeStats, "meleestats", LookMode.Deep);
        Scribe_Values.Look(ref maxLevel, "maxLevel", 3);
        Scribe_Values.Look(ref chanceToNameWeapon, "chancetonameweapon", 0.35f);
        Scribe_Values.Look(ref bondedWeaponExperienceMultipier, "bondedweaponexperiencemultipier", 1.5f);
        Scribe_Values.Look(ref numberOfRelicBonusStats, "numberofrelicbonusstats", 5);
        Scribe_Values.Look(ref useCustomNames, "usecustomnames");
        Scribe_Collections.Look(ref customWeaponNamesPool, "customweaponnamespool", LookMode.Value);
        Scribe_Values.Look(ref masteriesPercentagePerEvent, "masteriespercentageperevent", 0.25f);
        Scribe_Values.Look(ref eventWeaponNameChance, "eventweaponnamechance", 0.15f);
        Scribe_Values.Look(ref masteryOnOutsidePawns, "masteryonoutsidepawns", true);
        Scribe_Values.Look(ref useMoods, "usemoods");
        Scribe_Values.Look(ref useSpecificMasterySystem, "usespecificmasterysystem", true);
        Scribe_Values.Look(ref useGeneralMasterySystem, "usegeneralmasterysystem");
        Scribe_Values.Look(ref displayExperience, "displayexperience");
        Scribe_Values.Look(ref KeepOriginalWeaponNameQuality, "displayweaponoriginalnamequality");
        Scribe_Collections.Look(ref overrideClasses, "overrideclasses");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && customWeaponNamesPool == null)
        {
            customWeaponNamesPool = [];
        }
    }

    public static void Reset()
    {
        experiencePerLevel = [];
        rangedStats = [];
        meleeStats = [];
        maxLevel = 3;
        chanceToNameWeapon = 0.35f;
        bondedWeaponExperienceMultipier = 1.5f;
        numberOfRelicBonusStats = 5;
        useCustomNames = false;
        masteriesPercentagePerEvent = 0.25f;
        eventWeaponNameChance = 0.15f;
        masteryOnOutsidePawns = true;
        useMoods = false;
        useSpecificMasterySystem = true;
        useGeneralMasterySystem = false;
        displayExperience = false;
        KeepOriginalWeaponNameQuality = false;
        overrideClasses = new Dictionary<string, string>();
        SetSensibleDefaults();
    }

    public static void SetSensibleDefaults()
    {
        experiencePerLevel =
        [
            15000,
            20000,
            25000
        ];
        rangedStats =
        [
            new MasteryStat(StatDefOf.ShootingAccuracyPawn, 0.01f),
            new MasteryStat(StatDefOf.MoveSpeed, 0.2f),
            new MasteryStat(StatDefOf.HuntingStealth, 0.01f),
            new MasteryStat(StatDefOf.RangedWeapon_Cooldown, -0.1f),
            new MasteryStat(StatDefOf.AimingDelayFactor, -0.02f)
        ];
        meleeStats =
        [
            new MasteryStat(StatDefOf.MoveSpeed, 0.2f),
            new MasteryStat(StatDefOf.MeleeDodgeChance, 0.01f),
            new MasteryStat(StatDefOf.MeleeHitChance, 0.01f),
            new MasteryStat(StatDefOf.CarryingCapacity, 10f)
        ];
        if (ModsConfig.IdeologyActive)
        {
            meleeStats.Add(new MasteryStat(StatDefOf.SuppressionPower, 0.01f));
        }
    }

    public static MasteryStat FindStatWithStatDef(StatDef stat, bool melee)
    {
        var list = melee
            ? meleeStats.Where(item => item.GetStat() == stat).ToList()
            : rangedStats.Where(item => item.GetStat() == stat).ToList();
        return list.Count > 0 ? list.First() : null;
    }

    public static void RemoveStat(MasteryStat stat, bool melee)
    {
        if (!melee)
        {
            rangedStats.Remove(stat);
        }
        else
        {
            meleeStats.Remove(stat);
        }
    }

    public static void AddStat(MasteryStat stat, bool melee)
    {
        if (!melee)
        {
            rangedStats.Add(stat);
        }
        else
        {
            meleeStats.Add(stat);
        }
    }

    public static void ResolveStats()
    {
        foreach (var masteryStat in rangedStats)
        {
            masteryStat.Resolve();
        }

        foreach (var masteryStat in meleeStats)
        {
            masteryStat.Resolve();
        }

        rangedStats = rangedStats.Where(item => item.GetStat() != null).ToList();
        meleeStats = meleeStats.Where(item => item.GetStat() != null).ToList();
    }

    public static MasteryStat PickBonus(bool melee)
    {
        return melee ? meleeStats.RandomElement() : rangedStats.RandomElement();
    }

    public static int GetExperienceForLevel(int level)
    {
        return experiencePerLevel[level];
    }

    public static void LoadWeaponNames()
    {
        var list = new List<string>();
        foreach (var runningMod in LoadedModManager.RunningMods)
        {
            var modMetaData = runningMod.ModMetaData;
            if (modMetaData.LoadAfter != null)
            {
                foreach (var item in modMetaData.LoadAfter)
                {
                    if (item != MOD_PACKAGE_ID)
                    {
                        continue;
                    }

                    list.Add(runningMod.RootDir);
                    break;
                }
            }

            if (modMetaData.Dependencies == null)
            {
                continue;
            }

            foreach (var dependency in modMetaData.Dependencies)
            {
                if (dependency.packageId != MOD_PACKAGE_ID)
                {
                    continue;
                }

                list.Add(runningMod.RootDir);
                break;
            }
        }

        foreach (var item2 in list)
        {
            var linesFromTextFile = Files.GetLinesFromTextFile(
                Path.Combine(item2, "Languages", LanguageDatabase.activeLanguage.LegacyFolderName,
                    "WeaponNamesList.txt"), false);
            var linesFromTextFile2 = Files.GetLinesFromTextFile(
                Path.Combine(item2, "Languages", LanguageDatabase.activeLanguage.folderName, "WeaponNamesList.txt"),
                false);
            if (linesFromTextFile2 != null)
            {
                weaponNamesPool = [..linesFromTextFile2];
            }
            else if (linesFromTextFile != null)
            {
                weaponNamesPool = [..linesFromTextFile];
            }
        }

        if (weaponNamesPool != null)
        {
            return;
        }

        var linesFromTextFile3 = Files.GetLinesFromTextFile(WEAPON_NAMES_DEF_PATH, true);
        if (linesFromTextFile3 != null)
        {
            weaponNamesPool = [..linesFromTextFile3];
            return;
        }

        weaponNamesPool = ["Missing Names"];
    }

    public static void InitMessageKeys()
    {
        messages = [];
        var text = "SK_WeaponMastery_Message";
        for (var i = 0; i < 4; i++)
        {
            messages.Add(text + (i + 1));
        }
    }

    public static string PickWeaponName()
    {
        if (useCustomNames && customWeaponNamesPool.Count > 0)
        {
            return customWeaponNamesPool.RandomElement();
        }

        return weaponNamesPool.RandomElement();
    }
}