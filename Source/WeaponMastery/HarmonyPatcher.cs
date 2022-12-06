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

        var original = AccessTools.Method(typeof(Verb_Shoot), "WarmupComplete");
        var postfix = new HarmonyMethod(typeof(Core).GetMethod("OnPawnShoot"));
        instance.Patch(original, null, postfix);
        var original2 = AccessTools.Method(typeof(Verb_MeleeAttack), "TryCastShot");
        var postfix2 = new HarmonyMethod(typeof(Core).GetMethod("OnPawnMelee"));
        instance.Patch(original2, null, postfix2);
        var original3 = AccessTools.Method(typeof(Mod), "WriteSettings");
        var postfix3 = new HarmonyMethod(typeof(Core).GetMethod("OnModWriteSettings"));
        instance.Patch(original3, null, postfix3);
        if (ModSettings.useGeneralMasterySystem)
        {
            var original4 = AccessTools.Method(typeof(RaceProperties), "SpecialDisplayStats");
            var postfix4 = new HarmonyMethod(typeof(Core).GetMethod("AddMasteryDescriptionToDrawStats"));
            instance.Patch(original4, null, postfix4);
        }

        if (ModSettings.masteryOnOutsidePawns)
        {
            var original5 = AccessTools.Method(typeof(IncidentWorker_Raid), "TryGenerateRaidInfo");
            var postfix5 = new HarmonyMethod(typeof(Core).GetMethod("OnRaid"));
            instance.Patch(original5, null, postfix5);
            var original6 = AccessTools.Method(typeof(IncidentWorker_NeutralGroup), "SpawnPawns");
            var postfix6 = new HarmonyMethod(typeof(Core).GetMethod("OnNeutralPawnSpawn"));
            instance.Patch(original6, null, postfix6);
        }

        if (!ModSettings.displayExperience)
        {
            return;
        }

        var original7 = AccessTools.Method(typeof(Dialog_InfoCard), "Setup");
        var postfix7 = new HarmonyMethod(typeof(Core).GetMethod("OnInfoWindowSetup"));
        instance.Patch(original7, null, postfix7);
    }

    public static void SetInstance(Harmony instance)
    {
        HarmonyPatcher.instance = instance;
    }
}
