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
        public static ThoughtDef MasteredWeaponUnequipped;
        public static HediffDef MasteredWeaponEquipped;

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
                MasteryWeaponComp comp = weapon.TryGetComp<MasteryWeaponComp>();
                if (comp == null) return;
                comp.SetCurrentOwner(caster);
                if (ModSettings.useSpecificMasterySystem)
                {
                    if (!comp.IsActive()) comp.Init();
                    comp.AddExp(caster, (int)(num * num2));
                }
                if (!ModSettings.useGeneralMasterySystem) return;
                MasteryPawnComp compPawn = caster.TryGetComp<MasteryPawnComp>();
                if (compPawn == null) return;
                if (!compPawn.IsActive()) compPawn.Init();
                compPawn.AddExp(weapon.def, (int)(num * num2));
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
                MasteryWeaponComp comp = weapon.TryGetComp<MasteryWeaponComp>();
                if (comp == null) return;
                comp.SetCurrentOwner(casterPawn);
                if (ModSettings.useSpecificMasterySystem)
                {
                    if (!comp.IsActive()) comp.Init();
                    comp.AddExp(casterPawn, (int)exp);
                }
                if (!ModSettings.useGeneralMasterySystem) return;
                MasteryPawnComp compPawn = casterPawn.TryGetComp<MasteryPawnComp>();
                if (compPawn == null) return;
                if (!compPawn.IsActive()) compPawn.Init();
                compPawn.AddExp(weapon.def, (int)exp);
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

        // Update MasteryWeaponComp owner in a weapon
        public static void OnPawnEquipThing(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            if (eq.def.equipmentType == EquipmentType.Primary)
            {
                MasteryWeaponComp comp = eq.TryGetComp<MasteryWeaponComp>();
                if (comp == null || !comp.IsActive()) return;
                comp.SetCurrentOwner(__instance.pawn);
                if (ModSettings.useMoods && comp.PawnHasMastery(__instance.pawn))
                {
                    __instance.pawn.health.AddHediff(MasteredWeaponEquipped);
                    __instance.pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(MasteredWeaponUnequipped);
                }
            }
        }

        // When pawns unequips item
        public static void OnPawnEquipRemove(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            if (eq.def.equipmentType == EquipmentType.Primary)
            {
                MasteryWeaponComp comp = eq.TryGetComp<MasteryWeaponComp>();
                if (comp == null || !comp.IsActive()) return;
                if (comp.PawnHasMastery(__instance.pawn))
                {
                    __instance.pawn.needs.mood.thoughts.memories.TryGainMemory(MasteredWeaponUnequipped);
                    if (__instance.pawn.health.hediffSet.HasHediff(MasteredWeaponEquipped))
                        __instance.pawn.health.RemoveHediff(__instance.pawn.health.hediffSet.GetFirstHediffOfDef(MasteredWeaponEquipped));
                }
            }
        }

        public static void AddMasteryWeaponCompToWeaponDefs()
        {
            // Ensure that def is a weapon
            // Filter bad weapon defs suchs wood logs, thrumbo horns, etc...
            bool Predicate(ThingDef def)
            {
                return def.IsWeapon && def.HasComp(typeof(CompQuality));
            }

            CompProperties compProperties = new Verse.CompProperties { compClass = typeof(MasteryWeaponComp) };
            IEnumerable<ThingDef> defs = DefDatabase<ThingDef>.AllDefs.Where(Predicate).ToList();

            foreach (ThingDef def in defs)
                def.comps.Add(compProperties);
        }

        public static void AddMasteryPawnCompToHumanoidDefs()
        {
            // Ensure that def is a weapon
            // Filter bad weapon defs suchs wood logs, thrumbo horns, etc...
            bool Predicate(ThingDef def)
            {
                return def.race != null && def.race.Humanlike;
            }

            CompProperties compProperties = new Verse.CompProperties { compClass = typeof(MasteryPawnComp) };
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

        // Add mastery levels to raid
        public static void OnRaid(IncidentWorker_Raid __instance, IncidentParms parms, List<Pawn> pawns, bool debugTest, ref bool __result)
        {
            if (!__result) return;
            GenerateMasteriesForPawns(pawns);
        }

        // Add mastery levels to neutral pawns. Trade caravans, visitors
        // etc ...
        public static void OnNeutralPawnSpawn(IncidentWorker_NeutralGroup __instance, IncidentParms parms, ref List<Pawn> __result)
        {
            GenerateMasteriesForPawns(__result);
        }

        // Generate weapon masteries on a list of pawns
        private static void GenerateMasteriesForPawns(List<Pawn> pawns)
        {
            if (pawns == null) return;
            List<Pawn> clonedReferences = new List<Pawn>(pawns);
            int masteryOwnersCount = (int)Math.Ceiling(clonedReferences.Count * ModSettings.masteriesPercentagePerEvent);
            int currentOwnersCount = 0;
            Random rng = new Random();
            while (clonedReferences.Count != 0 && currentOwnersCount < masteryOwnersCount)
            {
                Pawn selectedPawn = clonedReferences.RandomElement();
                bool bonusAdded = false;
                // Is Pawn Humanoid and has weapon
                if (selectedPawn.RaceProps.Humanlike && selectedPawn.equipment?.Primary != null)
                {
                    if (ModSettings.useSpecificMasterySystem)
                    {
                        MasteryWeaponComp comp = selectedPawn.equipment.Primary.TryGetComp<MasteryWeaponComp>();
                        if (comp != null)
                        {
                            // Roll stats and weapon name
                            comp.SetCurrentOwner(selectedPawn);
                            comp.Init();
                            int statsCount = rng.Next(1, ModSettings.maxLevel);
                            for (int i = 0; i < statsCount; i++)
                            {
                                MasteryStat stat = ModSettings.PickBonus(selectedPawn.equipment.Primary.def.IsMeleeWeapon);
                                if (stat != null)
                                    comp.AddStatBonus(selectedPawn, stat.GetStat(), stat.GetOffset());
                            }
                            float weaponNameRoll = (float)rng.NextDouble();
                            if (weaponNameRoll <= ModSettings.eventWeaponNameChance) comp.SetWeaponName(ModSettings.PickWeaponName());
                            comp.GenerateDescription();
                            bonusAdded = true;
                        }
                    }
                    if (ModSettings.useGeneralMasterySystem)
                    {
                        MasteryPawnComp comp = selectedPawn.TryGetComp<MasteryPawnComp>();
                        if (comp != null)
                        {
                            comp.Init();
                            int statsCount = rng.Next(1, ModSettings.maxLevel);
                            for (int i = 0; i < statsCount; i++)
                            {
                                MasteryStat stat = ModSettings.PickBonus(selectedPawn.equipment.Primary.def.IsMeleeWeapon);
                                if (stat != null)
                                    comp.AddStatBonus(selectedPawn.equipment.Primary.def, stat.GetStat(), stat.GetOffset());
                            }
                            comp.GenerateDescription();
                            bonusAdded = true;
                        }
                    }
                }
                if (bonusAdded) currentOwnersCount++;
                clonedReferences.Remove(selectedPawn);
            }
        }

        // Display general weapon mastery description in pawn's information
        // tab
        public static void AddMasteryDescriptionToDrawStats(ThingDef parentDef, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn == null) return;
            IEnumerable<StatDrawEntry> NewFunc(IEnumerable<StatDrawEntry> functionOutput)
            {
                foreach (StatDrawEntry item in functionOutput) yield return item;
                if (pawn.def.race.Humanlike)
                {
                    MasteryPawnComp comp = pawn.TryGetComp<MasteryPawnComp>();
                    if (comp != null && comp.IsActive())
                        yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "SK_WeaponMastery_StatWeaponMastery".Translate(), "", comp.GetDescription(), 20);
                }
            }
            __result = NewFunc(__result);
        }
    }
}
