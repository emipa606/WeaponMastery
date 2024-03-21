using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WeaponMastery;

public static class Core
{
    private static bool writeLock;

    public static ThoughtDef MasteredWeaponUnequipped;

    public static HediffDef MasteredWeaponEquipped;

    public static void OnPawnShoot(Verb_Shoot __instance)
    {
        if (__instance.CurrentTarget.Thing is not Pawn pawn || pawn.Downed || !__instance.CasterIsPawn ||
            __instance.CasterPawn.skills == null)
        {
            return;
        }

        var num = pawn.HostileTo(__instance.caster) ? 170f : 20f;
        var num2 = __instance.verbProps.AdjustedFullCycleTime(__instance, __instance.CasterPawn);
        var pawn2 = __instance.Caster as Pawn;
        var pawnWeapon = GetPawnWeapon(pawn2);

        var masteryWeaponComp = pawnWeapon?.TryGetComp<MasteryWeaponComp>();
        if (masteryWeaponComp == null)
        {
            return;
        }

        masteryWeaponComp.SetCurrentOwner(pawn2);
        if (ModSettings.useSpecificMasterySystem)
        {
            if (!masteryWeaponComp.IsActive())
            {
                masteryWeaponComp.Init();
            }

            masteryWeaponComp.AddExp(pawn2, (int)(num * num2));
        }

        if (!ModSettings.useGeneralMasterySystem)
        {
            return;
        }

        var masteryPawnComp = pawn2.TryGetComp<MasteryPawnComp>();
        if (masteryPawnComp == null)
        {
            return;
        }

        if (!masteryPawnComp.IsActive())
        {
            masteryPawnComp.Init();
        }

        if (ModSettings.classes.TryGetValue(pawnWeapon.def, out var weaponClass))
        {
            masteryPawnComp.AddExp(weaponClass, (int)(num * num2),
                pawnWeapon.def.IsMeleeWeapon);
        }
    }

    public static void OnPawnMelee(Verb_MeleeAttack __instance, LocalTargetInfo ___currentTarget)
    {
        var casterPawn = __instance.CasterPawn;
        var thing = ___currentTarget.Thing;
        if (thing is not Pawn pawn || thing.def.category != ThingCategory.Pawn || pawn.Downed ||
            (int)pawn.GetPosture() > 0 ||
            casterPawn.skills == null)
        {
            return;
        }

        var num = 200f * __instance.verbProps.AdjustedFullCycleTime(__instance, casterPawn);
        var pawnWeapon = GetPawnWeapon(casterPawn);

        var masteryWeaponComp = pawnWeapon?.TryGetComp<MasteryWeaponComp>();
        if (masteryWeaponComp == null)
        {
            return;
        }

        masteryWeaponComp.SetCurrentOwner(casterPawn);
        if (ModSettings.useSpecificMasterySystem)
        {
            if (!masteryWeaponComp.IsActive())
            {
                masteryWeaponComp.Init();
            }

            masteryWeaponComp.AddExp(casterPawn, (int)num);
        }

        if (!ModSettings.useGeneralMasterySystem)
        {
            return;
        }

        var masteryPawnComp = casterPawn.TryGetComp<MasteryPawnComp>();
        if (masteryPawnComp == null)
        {
            return;
        }

        if (!masteryPawnComp.IsActive())
        {
            masteryPawnComp.Init();
        }

        if (ModSettings.classes.TryGetValue(pawnWeapon.def, out var @class))
        {
            masteryPawnComp.AddExp(@class, (int)num, pawnWeapon.def.IsMeleeWeapon);
        }
    }

    public static void InjectStatPartIntoStatDefs()
    {
        var list = new List<StatDef>();
        foreach (var masteryStat in ModSettings.rangedStats)
        {
            var stat = masteryStat.GetStat();
            list.Add(stat);
            if (stat.parts == null)
            {
                stat.parts = [];
            }

            stat.parts.Add(new StatPart_Mastery(stat));
        }

        foreach (var masteryStat in ModSettings.meleeStats)
        {
            var stat2 = masteryStat.GetStat();
            if (list.Contains(stat2))
            {
                continue;
            }

            list.Add(stat2);
            if (stat2.parts == null)
            {
                stat2.parts = [];
            }

            stat2.parts.Add(new StatPart_Mastery(stat2));
        }
    }

    public static void RemoveStatPartFromStatDefs()
    {
        foreach (var masteryStat in ModSettings.rangedStats)
        {
            var stat = masteryStat.GetStat();
            if (stat.parts.Any(item => item is StatPart_Mastery))
            {
                stat.parts = stat.parts.Where(item => item is not StatPart_Mastery).ToList();
            }
        }

        foreach (var masteryStat in ModSettings.meleeStats)
        {
            var stat = masteryStat.GetStat();
            if (stat.parts.Any(item => item is StatPart_Mastery))
            {
                stat.parts = stat.parts.Where(item => item is not StatPart_Mastery).ToList();
            }
        }
    }

    public static void OnPawnEquipThing(Pawn_EquipmentTracker __instance, ThingWithComps eq)
    {
        if (eq.def.equipmentType != EquipmentType.Primary)
        {
            return;
        }

        var masteryWeaponComp = eq.TryGetComp<MasteryWeaponComp>();
        if (masteryWeaponComp == null || !masteryWeaponComp.IsActive())
        {
            return;
        }

        masteryWeaponComp.SetCurrentOwner(__instance.pawn);
        if (!ModSettings.useMoods || !masteryWeaponComp.PawnHasMastery(__instance.pawn))
        {
            return;
        }

        __instance.pawn.health.AddHediff(MasteredWeaponEquipped);
        __instance.pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(MasteredWeaponUnequipped);
    }

    public static void OnPawnEquipRemove(Pawn_EquipmentTracker __instance, ThingWithComps eq)
    {
        if (eq.def.equipmentType != EquipmentType.Primary)
        {
            return;
        }

        var masteryWeaponComp = eq.TryGetComp<MasteryWeaponComp>();
        if (masteryWeaponComp == null || !masteryWeaponComp.IsActive() ||
            !masteryWeaponComp.PawnHasMastery(__instance.pawn))
        {
            return;
        }

        __instance.pawn.needs.mood.thoughts.memories.TryGainMemory(MasteredWeaponUnequipped);
        if (__instance.pawn.health.hediffSet.HasHediff(MasteredWeaponEquipped))
        {
            __instance.pawn.health.RemoveHediff(
                __instance.pawn.health.hediffSet.GetFirstHediffOfDef(MasteredWeaponEquipped));
        }
    }

    public static void AddMasteryWeaponCompToWeaponDefsAndSetClasses()
    {
        var compProperties = new CompProperties
        {
            compClass = typeof(MasteryWeaponComp)
        };
        var modWeapons = GetModWeapons();
        foreach (var item2 in modWeapons)
        {
            var modExtension = item2.GetModExtension<DefModExtension_Mastery>();
            item2.comps.Add(compProperties);
            if (modExtension != null)
            {
                ModSettings.classes[item2] = modExtension.gmclass;
            }
        }
    }

    public static void AddMasteryPawnCompToHumanoidDefs()
    {
        var compProperties = new CompProperties
        {
            compClass = typeof(MasteryPawnComp)
        };
        IEnumerable<ThingDef> enumerable = DefDatabase<ThingDef>.AllDefs.Where(Predicate).ToList();
        foreach (var item2 in enumerable)
        {
            item2.comps.Add(compProperties);
        }

        return;

        static bool Predicate(ThingDef def)
        {
            return def.race is { Humanlike: true };
        }
    }

    public static void OnModWriteSettings(Mod __instance)
    {
        if (__instance.Content.Name != WeaponMasteryMod.modName || writeLock)
        {
            return;
        }

        writeLock = true;
        if (!ModSettingsWindow.isOpen)
        {
            return;
        }

        ModSettingsWindow.Destroy();
        RemoveStatPartFromStatDefs();
        InjectStatPartIntoStatDefs();
    }

    public static void OnRaid(IncidentWorker_Raid __instance, IncidentParms parms, List<Pawn> pawns, bool debugTest,
        ref bool __result)
    {
        if (__result)
        {
            GenerateMasteriesForPawns(pawns);
        }
    }

    public static void OnNeutralPawnSpawn(IncidentWorker_NeutralGroup __instance, IncidentParms parms,
        ref List<Pawn> __result)
    {
        GenerateMasteriesForPawns(__result);
    }

    private static void GenerateMasteriesForPawns(List<Pawn> pawns)
    {
        if (pawns == null)
        {
            return;
        }

        var list = new List<Pawn>(pawns);
        var num = (int)Math.Ceiling(list.Count * ModSettings.masteriesPercentagePerEvent);
        var num2 = 0;
        while (list.Count != 0 && num2 < num)
        {
            var pawn = list.RandomElement();
            var generatedComp = false;
            if (pawn.RaceProps.Humanlike && pawn.equipment?.Primary != null)
            {
                if (ModSettings.useSpecificMasterySystem)
                {
                    var masteryWeaponComp = pawn.equipment.Primary.TryGetComp<MasteryWeaponComp>();
                    if (masteryWeaponComp != null)
                    {
                        masteryWeaponComp.SetCurrentOwner(pawn);
                        masteryWeaponComp.Init();
                        var num3 = Rand.RangeInclusive(1, ModSettings.maxLevel);
                        for (var i = 0; i < num3; i++)
                        {
                            var masteryStat = ModSettings.PickBonus(pawn.equipment.Primary.def.IsMeleeWeapon);
                            if (masteryStat != null)
                            {
                                masteryWeaponComp.AddStatBonus(pawn, masteryStat.GetStat(), masteryStat.GetOffset());
                            }
                        }

                        masteryWeaponComp.SetLevel(pawn, num3);
                        var value = Rand.Value;
                        if (value <= ModSettings.eventWeaponNameChance)
                        {
                            masteryWeaponComp.GenerateWeaponName();
                        }

                        masteryWeaponComp.GenerateDescription();
                        generatedComp = true;
                    }
                }

                if (ModSettings.useGeneralMasterySystem)
                {
                    var masteryPawnComp = pawn.TryGetComp<MasteryPawnComp>();
                    if (masteryPawnComp != null && ModSettings.classes.ContainsKey(pawn.equipment.Primary.def))
                    {
                        masteryPawnComp.Init();
                        var num4 = Rand.RangeInclusive(1, ModSettings.maxLevel);
                        for (var j = 0; j < num4; j++)
                        {
                            var masteryStat2 = ModSettings.PickBonus(pawn.equipment.Primary.def.IsMeleeWeapon);
                            if (masteryStat2 != null)
                            {
                                masteryPawnComp.AddStatBonus(ModSettings.classes[pawn.equipment.Primary.def],
                                    masteryStat2.GetStat(), masteryStat2.GetOffset());
                            }
                        }

                        masteryPawnComp.SetLevel(ModSettings.classes[pawn.equipment.Primary.def], num4);
                        masteryPawnComp.GenerateDescription();
                        generatedComp = true;
                    }
                }
            }

            if (generatedComp)
            {
                num2++;
            }

            list.Remove(pawn);
        }
    }

    public static void AddMasteryDescriptionToDrawStats(ThingDef parentDef, StatRequest req,
        ref IEnumerable<StatDrawEntry> __result)
    {
        var pawn = req.Thing as Pawn;
        if (pawn != null)
        {
            __result = NewFunc(__result);
        }

        return;

        IEnumerable<StatDrawEntry> NewFunc(IEnumerable<StatDrawEntry> functionOutput)
        {
            foreach (var item in functionOutput)
            {
                yield return item;
            }

            if (pawn.def.race.Humanlike)
            {
                var comp = pawn.TryGetComp<MasteryPawnComp>();
                if (comp?.IsActive() ?? false)
                {
                    yield return new StatDrawEntry(StatCategoryDefOf.BasicsPawn,
                        "WeaponMastery_StatWeaponMastery".Translate(), "", comp.GetDescription(), 20);
                }
            }
        }
    }

    public static void OnInfoWindowSetup(Dialog_InfoCard __instance, Thing ___thing)
    {
        if (___thing == null)
        {
            return;
        }

        var masteryWeaponComp = ___thing.TryGetComp<MasteryWeaponComp>();
        if (masteryWeaponComp != null && masteryWeaponComp.IsActive())
        {
            masteryWeaponComp.GenerateDescription();
            return;
        }

        var masteryPawnComp = ___thing.TryGetComp<MasteryPawnComp>();
        if (masteryPawnComp != null && masteryPawnComp.IsActive())
        {
            masteryPawnComp.GenerateDescription();
        }
    }

    public static Thing GetPawnWeapon(Pawn pawn)
    {
        if (DualWieldCompat.enabled && DualWieldCompat.isCurrentAttackOffhand)
        {
            return DualWieldCompat.GetOffhandWeapon(pawn);
        }

        return pawn?.equipment?.Primary;
    }

    public static void OverrideClasses()
    {
        if (ModSettings.overrideClasses == null)
        {
            ModSettings.overrideClasses = new Dictionary<string, string>();
            return;
        }

        var list = new List<string>();
        foreach (var item in ModSettings.overrideClasses)
        {
            var thingDef = DefDatabase<ThingDef>.AllDefsListForReading.Find(def => def.defName == item.Key);
            if (thingDef == null)
            {
                list.Add(item.Key);
            }
            else
            {
                ModSettings.classes[thingDef] = item.Value;
            }
        }

        foreach (var item2 in list)
        {
            ModSettings.overrideClasses.Remove(item2);
        }
    }

    public static IEnumerable<ThingDef> GetModWeapons()
    {
        return DefDatabase<ThingDef>.AllDefs.Where(Predicate).ToList();

        static bool Predicate(ThingDef def)
        {
            var modExtension = def.GetModExtension<DefModExtension_Mastery>();
            if (modExtension is { blacklisted: true })
            {
                return false;
            }

            return def.IsWeapon && def.HasComp(typeof(CompQuality));
        }
    }
}