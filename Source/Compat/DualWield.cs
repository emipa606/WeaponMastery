using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using RimWorld;
using HarmonyLib;
using Verse;

namespace SK_WeaponMastery.Compat
{
    public static class DualWieldCompat
    {
        public static bool isCurrentAttackOffhand = false;
        public static bool enabled = false;

        public static Thing GetOffhandWeapon(Pawn pawn)
        {
            MethodInfo method = AccessTools.Method("DualWield.Ext_Pawn_EquipmentTracker:TryGetOffHandEquipment");
            if (method != null) Logger.WriteToHarmonyFile("METHOD FOUND");
            object[] methodParams = new object[] { pawn.equipment, null };
            bool result = (bool)method.Invoke(null, methodParams);
            return result ? (Thing)methodParams[1] : null;
        }

        public static void PatchMethods()
        {
            // Patch DualWield Ext_Pawn TryStartOffHandAttack method
            MethodInfo tryStartOffhandAttachkMethod = AccessTools.Method("DualWield.Ext_Pawn:TryStartOffHandAttack");
            HarmonyMethod postfixMethod = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("TryStartOffHandAttackPostfix"));
            HarmonyPatcher.instance.Patch(tryStartOffhandAttachkMethod, null, postfixMethod);
        }

        public static void TryStartOffHandAttackPostfix()
        {
            isCurrentAttackOffhand = true;
        }

        public static void Init()
        {
            if (ModsConfig.IsActive("Roolo.DualWield"))
                enabled = true;
            if (!enabled) return;
            PatchMethods();
        }
    }
}
