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

        public static void PatchVanillaMethods(Harmony instance)
        {
            if (instance == null)
            {
                Logger.WriteToHarmonyFile("Missing harmony instance");
                return;
            }
            // Patch WarmupComplete method
            MethodInfo warmupCompleteMethod = AccessTools.Method(typeof(Verb_Shoot), "WarmupComplete");
            HarmonyMethod onPawnShootMethod = new HarmonyMethod(typeof(Core).GetMethod("OnPawnShoot"));
            instance.Patch(warmupCompleteMethod, null, onPawnShootMethod);

            // Patch TryCastShot method
            MethodInfo tryCastShotMethod = AccessTools.Method(typeof(Verb_MeleeAttack), "TryCastShot");
            HarmonyMethod onPawnMelee = new HarmonyMethod(typeof(Core).GetMethod("OnPawnMelee"));
            instance.Patch(tryCastShotMethod, null, onPawnMelee);

            // Patch OnEquipNotify method
            MethodInfo notifyEquipmentAddedMethod = AccessTools.Method(typeof(Pawn_EquipmentTracker), "Notify_EquipmentAdded");
            HarmonyMethod onPawnEquipMethod = new HarmonyMethod(typeof(Core).GetMethod("OnPawnEquipThing"));
            instance.Patch(notifyEquipmentAddedMethod, null, onPawnEquipMethod);

            // Patch Mod WriteSettings method
            MethodInfo writeSettingsMetho = AccessTools.Method(typeof(Mod), "WriteSettings");
            HarmonyMethod onModWriteSettingsMethod = new HarmonyMethod(typeof(Core).GetMethod("OnModWriteSettings"));
            instance.Patch(writeSettingsMetho, null, onModWriteSettingsMethod);
        }
    }
}
