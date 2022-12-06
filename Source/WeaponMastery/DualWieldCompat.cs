using System.Reflection;
using HarmonyLib;
using Verse;

namespace WeaponMastery;

public static class DualWieldCompat
{
    public static bool isCurrentAttackOffhand;

    public static bool enabled;

    private static MethodInfo tryGetOffHandEqMethod;

    public static Thing GetOffhandWeapon(Pawn pawn)
    {
        var array = new object[] { pawn.equipment, null };
        return (bool)tryGetOffHandEqMethod.Invoke(null, array) ? (Thing)array[1] : null;
    }

    public static void PatchMethods()
    {
        var original = AccessTools.Method("DualWield.Ext_Verb:OffhandTryStartCastOn");
        var prefix = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("OffhandTryStartCastOnPrefix"));
        var postfix = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("OffhandTryStartCastOnPostfix"));
        HarmonyPatcher.instance.Patch(original, prefix, postfix);
        var original2 = AccessTools.Method("DualWield.Stances.Stance_Warmup_DW:Expire");
        var prefix2 = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("ExpirePrefix"));
        var postfix2 = new HarmonyMethod(typeof(DualWieldCompat).GetMethod("ExpirePostfix"));
        HarmonyPatcher.instance.Patch(original2, prefix2, postfix2);
    }

    public static void OffhandTryStartCastOnPostfix()
    {
        isCurrentAttackOffhand = false;
    }

    public static bool OffhandTryStartCastOnPrefix()
    {
        isCurrentAttackOffhand = true;
        return true;
    }

    public static bool ExpirePrefix()
    {
        isCurrentAttackOffhand = true;
        return true;
    }

    public static void ExpirePostfix()
    {
        isCurrentAttackOffhand = false;
    }

    public static void Init()
    {
        if (ModsConfig.IsActive("Roolo.DualWield"))
        {
            enabled = true;
        }

        if (!enabled)
        {
            return;
        }

        PatchMethods();
        tryGetOffHandEqMethod = AccessTools.Method("DualWield.Ext_Pawn_EquipmentTracker:TryGetOffHandEquipment");
    }
}
