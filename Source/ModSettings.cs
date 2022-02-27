using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SK_WeaponMastery
{
    /*
     * ModSettings are loaded using GetSettings if there's a file.
     * ModSettings DO NOT load the first time because there's no settings
     * file initially. Thus ExposeData() doesn't run when the game loads.
     * ModSettings are saved when the player closes the modsettings window.
     * You could also trigger a manual save by calling ModSettings.Write()
     */
    public class ModSettings : Verse.ModSettings
    {
        public static List<int> experiencePerLevel;
        public static int maxLevel = 3;
        public static bool initialLoad = false;
        public static List<MasteryStat> rangedStats;
        public static List<MasteryStat> meleeStats;
        public static List<string> weaponNamesPool;
        public static List<string> messages;
        private static readonly string WEAPON_NAMES_FILENAME = "WeaponNamesList.txt";

        public override void ExposeData()
        {
            initialLoad = true;
            base.ExposeData();
            Scribe_Collections.Look(ref experiencePerLevel, "experienceperlevel", LookMode.Value);
            Scribe_Collections.Look(ref rangedStats, true, "rangedstats", LookMode.Deep);
            Scribe_Collections.Look(ref meleeStats, true, "meleestats", LookMode.Deep);
            Scribe_Values.Look(ref maxLevel, "maxLevel");
        }

        // Set default settings
        public static void SetSensibleDefaults()
        {
            // Default ExperiencePerLevel
            experiencePerLevel = new List<int>();
            experiencePerLevel.Add(15000);
            experiencePerLevel.Add(20000);
            experiencePerLevel.Add(25000);

            // Default Ranged Stats
            rangedStats = new List<MasteryStat>();
            rangedStats.Add(new MasteryStat(StatDefOf.ShootingAccuracyPawn, 0.01f));
            rangedStats.Add(new MasteryStat(StatDefOf.MoveSpeed, 0.2f));
            rangedStats.Add(new MasteryStat(StatDefOf.HuntingStealth, 0.01f));
            rangedStats.Add(new MasteryStat(StatDefOf.RangedWeapon_Cooldown, -0.1f));
            rangedStats.Add(new MasteryStat(StatDefOf.AimingDelayFactor, -0.02f));

            // Default Melee Stats
            meleeStats = new List<MasteryStat>();
            meleeStats.Add(new MasteryStat(StatDefOf.MoveSpeed, 0.2f));
            meleeStats.Add(new MasteryStat(StatDefOf.MeleeDodgeChance, 0.01f));
            meleeStats.Add(new MasteryStat(StatDefOf.MeleeHitChance, 0.01f));
            meleeStats.Add(new MasteryStat(StatDefOf.CarryingCapacity, 10f));
            meleeStats.Add(new MasteryStat(StatDefOf.SuppressionPower, 0.01f));
        }

        // Find MasteryStat containing the same StatDef parameter
        public static MasteryStat FindStatWithStatDef(StatDef stat, bool melee)
        {
            List<MasteryStat> result;
            if (!melee)
                result = rangedStats.Where((MasteryStat item) => item.GetStat() == stat).ToList();
            else
                result = meleeStats.Where((MasteryStat item) => item.GetStat() == stat).ToList();
            return result.Count > 0 ? result.First() : null;
        }

        public static void RemoveStat(MasteryStat stat, bool melee)
        {
            if (!melee)
                rangedStats.Remove(stat);
            else
                meleeStats.Remove(stat);
        }

        public static void AddStat(MasteryStat stat, bool melee)
        {
            if (!melee)
                rangedStats.Add(stat);
            else
                meleeStats.Add(stat);
        }

        // Convert string defnames to StatDefs and remove ones that didn't load
        public static void ResolveStats()
        {
            foreach (MasteryStat item in rangedStats)
                item.Resolve();
            foreach (MasteryStat item in meleeStats)
                item.Resolve();
            rangedStats = rangedStats.Where((MasteryStat item) => item.GetStat() != null).ToList();
            meleeStats = meleeStats.Where((MasteryStat item) => item.GetStat() != null).ToList();
        }

        // Get a random MasteryStat
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
            string[] names = Files.GetLinesFromTextFile(WEAPON_NAMES_FILENAME, true);
            if (names != null)
                weaponNamesPool = new List<string>(names);
            else
            {
                weaponNamesPool = new List<string>();
                weaponNamesPool.Add("Weapon Names Missing!");
            }
        }

        // Message Language Keys
        public static void InitMessageKeys()
        {
            messages = new List<string>();
            string key = "SK_WeaponMastery_Message";
            for (int i = 0; i < 4; i++)
            {
                messages.Add(key + (i + 1).ToString());
            }
        }
    }
}
