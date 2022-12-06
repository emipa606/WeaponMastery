using System;
using HarmonyLib;
using Verse;

namespace WeaponMastery;

public static class SimpleSidearmsCompat
{
    public static bool enabled;

    public static bool weaponSwitch;

    public static void PatchMethods()
    {
        var original = AccessTools.Method("PeteTimesSix.SimpleSidearms.Utilities.WeaponAssingment:equipSpecificWeapon");
        var prefix = new HarmonyMethod(typeof(SimpleSidearmsCompat).GetMethod("BeforeWeaponSwitch"));
        var postfix = new HarmonyMethod(typeof(SimpleSidearmsCompat).GetMethod("AfterWeaponSwitch"));
        HarmonyPatcher.instance.Patch(original, prefix, postfix);
        if (!ModSettings.useMoods)
        {
            return;
        }

        var original2 = AccessTools.Method(typeof(ThingOwner<>).MakeGenericType(typeof(Thing)), "TryDrop",
            new[]
            {
                typeof(Thing),
                typeof(IntVec3),
                typeof(Map),
                typeof(ThingPlaceMode),
                typeof(Thing).MakeByRefType(),
                typeof(Action<Thing, int>),
                typeof(Predicate<IntVec3>)
            });
        var postfix2 = new HarmonyMethod(typeof(SimpleSidearmsCompat).GetMethod("AfterDropMethod"));
        HarmonyPatcher.instance.Patch(original2, null, postfix2);
    }

    public static void Init()
    {
        if (ModsConfig.IsActive("PeteTimesSix.SimpleSidearms"))
        {
            enabled = true;
        }

        if (enabled)
        {
            PatchMethods();
        }
    }

    public static bool BeforeWeaponSwitch()
    {
        weaponSwitch = true;
        return true;
    }

    public static void AfterWeaponSwitch()
    {
        weaponSwitch = false;
    }

    public static void AfterDropMethod(Thing thing, IntVec3 dropLoc, Map map, ref bool __result)
    {
        if (!__result || thing is not ThingWithComps)
        {
            return;
        }

        Pawn pawn = null;
        var thingList = dropLoc.GetThingList(map);
        foreach (var item in thingList)
        {
            if (item is Pawn pawn1)
            {
                pawn = pawn1;
            }
        }

        if (pawn is { equipment: { } } && pawn.needs.mood != null && !PawnHasAnyMasteredWeapon(pawn) &&
            pawn.health.hediffSet.HasHediff(Core.MasteredWeaponEquipped))
        {
            pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(Core.MasteredWeaponEquipped));
        }
    }

    public static bool PawnHasAnyMasteredWeapon(Pawn p)
    {
        foreach (var thing in p.inventory.innerContainer)
        {
            if (thing is not ThingWithComps)
            {
                continue;
            }

            var masteryWeaponComp = thing.TryGetComp<MasteryWeaponComp>();
            if (masteryWeaponComp != null && masteryWeaponComp.IsActive() && masteryWeaponComp.PawnHasMastery(p))
            {
                return true;
            }
        }

        var primary = p.equipment.Primary;
        if (primary == null)
        {
            return false;
        }

        var masteryWeaponComp2 = primary.TryGetComp<MasteryWeaponComp>();
        return masteryWeaponComp2 != null && masteryWeaponComp2.IsActive() && masteryWeaponComp2.PawnHasMastery(p);
    }
}
