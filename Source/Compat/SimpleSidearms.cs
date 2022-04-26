using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace SK_WeaponMastery.Compat
{
    public static class SimpleSidearmsCompat
    {
        public static bool enabled = false;
        public static bool weaponSwitch = false;

        public static void PatchMethods()
        {
            // Patch WeaponAssingment equipSpecificWeapon method
            MethodInfo switchWeaponMethod = AccessTools.Method("PeteTimesSix.SimpleSidearms.Utilities.WeaponAssingment:equipSpecificWeapon");
            HarmonyMethod beforeSwitchMethod = new HarmonyMethod(typeof(SimpleSidearmsCompat).GetMethod("BeforeWeaponSwitch"));
            HarmonyMethod afterSwitchMethod = new HarmonyMethod(typeof(SimpleSidearmsCompat).GetMethod("AfterWeaponSwitch"));
            HarmonyPatcher.instance.Patch(switchWeaponMethod, beforeSwitchMethod, afterSwitchMethod);

            MethodInfo tryDropMethod = AccessTools.Method(typeof(ThingOwner<>).MakeGenericType(new Type[] { typeof(Thing) }), "TryDrop", new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(ThingPlaceMode), typeof(Thing).MakeByRefType(), typeof(Action<Thing, int>), typeof(Predicate<IntVec3>) });
            HarmonyMethod afterDropMethod = new HarmonyMethod(typeof(SimpleSidearmsCompat).GetMethod("AfterDropMethod"));
            HarmonyPatcher.instance.Patch(tryDropMethod, null, afterDropMethod);
        }
        public static void Init()
        {
            if (ModsConfig.IsActive("PeteTimesSix.SimpleSidearms"))
                enabled = true;
            if (!enabled) return;
            PatchMethods();
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

        // Consider pawn dropping their favorite weapon from sidearm slot
        // This won't go through unequip event meaning no mood updates
        public static void AfterDropMethod(Thing thing, IntVec3 dropLoc, Map map, ref bool __result)
        {
            if (!__result) return;
            ThingWithComps item = thing as ThingWithComps;
            if (item == null) return;
            Pawn owner = null;
            List<Thing> list = dropLoc.GetThingList(map);
            foreach (Thing t in list)
                if (t is Pawn)
                    owner = t as Pawn;
            if (owner == null || PawnHasAnyMasteredWeapon(owner)) return;
            owner.needs.mood.thoughts.memories.TryGainMemory(Core.MasteredWeaponUnequipped);
            if (owner.health.hediffSet.HasHediff(Core.MasteredWeaponEquipped))
                owner.health.RemoveHediff(owner.health.hediffSet.GetFirstHediffOfDef(Core.MasteredWeaponEquipped));
        }

        public static bool PawnHasAnyMasteredWeapon(Pawn p)
        {
            foreach (Thing item in p.inventory.innerContainer)
            {
                ThingWithComps possibleWeapon = item as ThingWithComps;
                if (possibleWeapon == null) continue;
                MasteryWeaponComp comp = item.TryGetComp<MasteryWeaponComp>();
                if (comp == null || !comp.IsActive()) continue;
                return comp.PawnHasMastery(p);
            }
            ThingWithComps primary = p.equipment.Primary;
            if (primary != null)
            {
                MasteryWeaponComp comp = primary.TryGetComp<MasteryWeaponComp>();
                return (comp != null && comp.IsActive() && comp.PawnHasMastery(p)); 
            }
            return false;
        }
    }
}
