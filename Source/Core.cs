using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SK_WeaponMastery
{
    // Mod core logic
    public static class Core
    {
        private static bool writeLock;

        // Add mastery experience to shooter pawn
        public static void OnPawnShoot(Verb_Shoot __instance)
        {
            Pawn pawn = __instance.CurrentTarget.Thing as Pawn;
            if (pawn != null && !pawn.Downed && __instance.CasterIsPawn && __instance.CasterPawn.skills != null)
            {
                float num = (pawn.HostileTo(__instance.caster) ? 170f : 20f);
                float num2 = __instance.verbProps.AdjustedFullCycleTime(__instance, __instance.CasterPawn);
                Pawn caster = __instance.Caster as Pawn;
                Thing weapon = caster.equipment.Primary;
                MasteryComp comp = weapon.TryGetComp<MasteryComp>();
                if (comp == null) return;
                if (!comp.IsActive()) comp.Init(caster);
                comp.AddExp(caster, (int)(num * num2));
            }
        }

        // Add mastery experience to melee pawn
        public static void OnPawnMelee(Verb_MeleeAttack __instance, LocalTargetInfo ___currentTarget)
        {
            Pawn casterPawn = __instance.CasterPawn;
            Thing thing = ___currentTarget.Thing;
            Pawn pawn = thing as Pawn;
            if ((!(thing.def.category != ThingCategory.Pawn || pawn.Downed || pawn.GetPosture() > PawnPosture.Standing)) && casterPawn.skills != null)
            {
                float exp = 200f * __instance.verbProps.AdjustedFullCycleTime(__instance, casterPawn);
                Thing weapon = casterPawn?.equipment?.Primary;
                if (weapon == null) return;
                MasteryComp comp = weapon.TryGetComp<MasteryComp>();
                if (comp == null) return;
                if (!comp.IsActive()) comp.Init(casterPawn);
                comp.AddExp(casterPawn, (int)exp);
            }
        }

        // Add custom stat part from my mod into base game stat defs
        // This should be used to influence stat calculation
        public static void InjectStatPartIntoStatDefs()
        {
            List<StatDef> injected = new List<StatDef>();
            foreach (MasteryStat mStat in ModSettings.rangedStats)
            {
                StatDef stat = mStat.GetStat();
                injected.Add(stat);
                // I hope this is ok
                if (stat.parts == null) stat.parts = new List<StatPart>();
                stat.parts.Add(new StatPart_Mastery(stat));
            }
            foreach (MasteryStat mStat in ModSettings.meleeStats)
            {
                StatDef stat = mStat.GetStat();
                if (injected.Contains(stat)) continue;
                injected.Add(stat);
                // I hope this is ok
                if (stat.parts == null) stat.parts = new List<StatPart>();
                stat.parts.Add(new StatPart_Mastery(stat));
            }
        }

        // Remove custom stat part from my mod from base game stat defs
        // Used for reloding configs
        public static void RemoveStatPartFromStatDefs()
        {
            foreach (MasteryStat mStat in ModSettings.rangedStats)
            {
                StatDef stat = mStat.GetStat();
                if (stat.parts.Any((StatPart item) => item is StatPart_Mastery))
                    stat.parts = stat.parts.Where((StatPart item) => !(item is StatPart_Mastery)).ToList();
            }
            foreach (MasteryStat mStat in ModSettings.meleeStats)
            {
                StatDef stat = mStat.GetStat();
                if (stat.parts.Any((StatPart item) => item is StatPart_Mastery))
                    stat.parts = stat.parts.Where((StatPart item) => !(item is StatPart_Mastery)).ToList();
            }
        }

        // Update MasteryComp owner in a weapon
        public static void OnPawnEquipThing(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            if (eq.def.equipmentType == EquipmentType.Primary)
            {
                MasteryComp comp = eq.TryGetComp<MasteryComp>();
                if (comp == null || !comp.IsActive()) return;
                comp.SetCurrentOwner(__instance.pawn);
            }
        }

        public static void AddMasteryCompToWeaponDefs()
        {
            // Ensure that def is a weapon
            // Filter bad weapon defs suchs wood logs, thrumbo horns, etc...
            bool Predicate(ThingDef def)
            {
                return def.IsWeapon && def.HasComp(typeof(CompQuality));
            }

            CompProperties compProperties = new Verse.CompProperties { compClass = typeof(MasteryComp) };
            IEnumerable<ThingDef> defs = DefDatabase<ThingDef>.AllDefs.Where(Predicate).ToList();

            foreach (ThingDef def in defs)
                def.comps.Add(compProperties);
        }

        public static void OnModWriteSettings(Mod __instance)
        {
            if (__instance.Content.Name != WeaponMasteryMod.modName || writeLock) return;
            writeLock = true;

            if (ModSettingsWindow.isOpen)
            {
                ModSettingsWindow.Destroy();
                RemoveStatPartFromStatDefs();
                InjectStatPartIntoStatDefs();
            }
        }
    }
}
