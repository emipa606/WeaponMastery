using HarmonyLib;
using RimWorld;
using Verse;

namespace WeaponMastery;

public static class HarmonyPatcher
{
    public static Harmony instance;

    public static void PatchVanillaMethods()
    {
        if (instance == null)
        {
            Logger.WriteToHarmonyFile("Missing harmony instance");
            return;
        }

        var originalWarmupComplete = AccessTools.Method(typeof(Verb_Shoot), nameof(Verb_Shoot.WarmupComplete));
        var postfixOnPawnShoot = new HarmonyMethod(typeof(Core).GetMethod(nameof(Core.OnPawnShoot)));
        instance.Patch(originalWarmupComplete, null, postfixOnPawnShoot);
        var originalTryCastShot = AccessTools.Method(typeof(Verb_MeleeAttack), "TryCastShot");
        var postfixOnPawnMelee = new HarmonyMethod(typeof(Core).GetMethod(nameof(Core.OnPawnMelee)));
        instance.Patch(originalTryCastShot, null, postfixOnPawnMelee);
        var originalWriteSettings = AccessTools.Method(typeof(Mod), nameof(Mod.WriteSettings));
        var postfixOnModWriteSettings = new HarmonyMethod(typeof(Core).GetMethod(nameof(Core.OnModWriteSettings)));
        instance.Patch(originalWriteSettings, null, postfixOnModWriteSettings);
        if (ModSettings.useGeneralMasterySystem)
        {
            var originalSpecialDisplayStats =
                AccessTools.Method(typeof(RaceProperties), nameof(RaceProperties.SpecialDisplayStats));
            var postfixAddMasteryDescriptionToDrawStats =
                new HarmonyMethod(typeof(Core).GetMethod(nameof(Core.AddMasteryDescriptionToDrawStats)));
            instance.Patch(originalSpecialDisplayStats, null, postfixAddMasteryDescriptionToDrawStats);
        }

        if (ModSettings.masteryOnOutsidePawns)
        {
            var originalTryGenerateRaidInfo = AccessTools.Method(typeof(IncidentWorker_Raid), "TryGenerateRaidInfo");
            var postfixOnRaid = new HarmonyMethod(typeof(Core).GetMethod(nameof(Core.OnRaid)));
            instance.Patch(originalTryGenerateRaidInfo, null, postfixOnRaid);
            var originalSpawnPawns = AccessTools.Method(typeof(IncidentWorker_NeutralGroup), "SpawnPawns");
            var postfixOnNeutralPawnSpawn = new HarmonyMethod(typeof(Core).GetMethod(nameof(Core.OnNeutralPawnSpawn)));
            instance.Patch(originalSpawnPawns, null, postfixOnNeutralPawnSpawn);
        }

        if (!ModSettings.displayExperience)
        {
            return;
        }

        var originalSetup = AccessTools.Method(typeof(Dialog_InfoCard), "Setup");
        var postfixOnInfoWindowSetup = new HarmonyMethod(typeof(Core).GetMethod(nameof(Core.OnInfoWindowSetup)));
        instance.Patch(originalSetup, null, postfixOnInfoWindowSetup);
    }

    public static void SetInstance(Harmony instance)
    {
        HarmonyPatcher.instance = instance;
    }
}