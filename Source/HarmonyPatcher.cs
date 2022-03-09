using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;


namespace SK_WeaponMastery
{
    
    // Responsible for patching pre/post fix methods into the game's 
    // original methods
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
            // Patch Verb_Shoot WarmupComplete method
            MethodInfo warmupCompleteMethod = AccessTools.Method(typeof(Verb_Shoot), "WarmupComplete");
            HarmonyMethod onPawnShootMethod = new HarmonyMethod(typeof(Core).GetMethod("OnPawnShoot"));
            instance.Patch(warmupCompleteMethod, null, onPawnShootMethod);

            // Patch Verb_MeleeAttack TryCastShot method
            MethodInfo tryCastShotMethod = AccessTools.Method(typeof(Verb_MeleeAttack), "TryCastShot");
            HarmonyMethod onPawnMelee = new HarmonyMethod(typeof(Core).GetMethod("OnPawnMelee"));
            instance.Patch(tryCastShotMethod, null, onPawnMelee);

            // Patch Pawn_EquipmentTracker OnEquipNotify method
            MethodInfo notifyEquipmentAddedMethod = AccessTools.Method(typeof(Pawn_EquipmentTracker), "Notify_EquipmentAdded");
            HarmonyMethod onPawnEquipMethod = new HarmonyMethod(typeof(Core).GetMethod("OnPawnEquipThing"));
            instance.Patch(notifyEquipmentAddedMethod, null, onPawnEquipMethod);

            // Patch Mod WriteSettings method
            MethodInfo writeSettingsMetho = AccessTools.Method(typeof(Mod), "WriteSettings");
            HarmonyMethod onModWriteSettingsMethod = new HarmonyMethod(typeof(Core).GetMethod("OnModWriteSettings"));
            instance.Patch(writeSettingsMetho, null, onModWriteSettingsMethod);

            // Patch these when mastery on outsider pawns is enabled in config
            if (ModSettings.masteryOnOutsidePawns)
            {
                // Patch IncidentWorker_Raid TryGenerateRaidInfo method
                MethodInfo tryGenerateRaidMethod = AccessTools.Method(typeof(IncidentWorker_Raid), "TryGenerateRaidInfo");
                HarmonyMethod onRaidMethod = new HarmonyMethod(typeof(Core).GetMethod("OnRaid"));
                instance.Patch(tryGenerateRaidMethod, null, onRaidMethod);

                // Patch IncidentWorker_NeutralGroup SpawnPawns method
                MethodInfo spawnPawnsMethod = AccessTools.Method(typeof(IncidentWorker_NeutralGroup), "SpawnPawns");
                HarmonyMethod onNeutralPawnSpawnMethod = new HarmonyMethod(typeof(Core).GetMethod("OnNeutralPawnSpawn"));
                instance.Patch(spawnPawnsMethod, null, onNeutralPawnSpawnMethod);
            }
        }

        public static void SetInstance(Harmony instance)
        {
            HarmonyPatcher.instance = instance;
        }
    }
}
