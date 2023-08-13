using RimWorld;
using UnityEngine;
using Verse;

namespace WeaponMastery;

internal class StatPart_Mastery : StatPart
{
    public StatPart_Mastery(StatDef parentStat)
    {
        this.parentStat = parentStat;
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (!req.HasThing)
        {
            return;
        }

        if (req.Thing.def.race != null)
        {
            if (req.Thing is not Pawn pawn || pawn.equipment?.Primary == null)
            {
                return;
            }

            var masteryWeaponComp = pawn.equipment.Primary.TryGetComp<MasteryWeaponComp>();
            if (masteryWeaponComp == null || !masteryWeaponComp.IsActive())
            {
                return;
            }

            val += masteryWeaponComp.GetCombinedBonus(parentStat);
            var currentOwner = masteryWeaponComp.GetCurrentOwner();
            if (currentOwner == null)
            {
                return;
            }

            var masteryPawnComp = masteryWeaponComp.GetCurrentOwner().TryGetComp<MasteryPawnComp>();
            if (masteryPawnComp != null && masteryPawnComp.IsActive() &&
                ModSettings.classes.TryGetValue(pawn.equipment.Primary.def, out var @class))
            {
                val += masteryPawnComp.GetStatBonus(@class, parentStat);
            }
        }
        else
        {
            if (!req.Thing.def.HasComp(typeof(MasteryWeaponComp)))
            {
                return;
            }

            var masteryWeaponComp2 = req.Thing.TryGetComp<MasteryWeaponComp>();
            if (masteryWeaponComp2 == null || !masteryWeaponComp2.IsActive())
            {
                return;
            }

            val += masteryWeaponComp2.GetCombinedBonus(parentStat);
            var currentOwner2 = masteryWeaponComp2.GetCurrentOwner();
            if (currentOwner2 == null)
            {
                return;
            }

            var masteryPawnComp2 = masteryWeaponComp2.GetCurrentOwner().TryGetComp<MasteryPawnComp>();
            if (masteryPawnComp2 != null && masteryPawnComp2.IsActive() &&
                ModSettings.classes.TryGetValue(req.Thing.def, out var @class))
            {
                val += masteryPawnComp2.GetStatBonus(@class, parentStat);
            }
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        var num = 0f;
        switch (req.HasThing)
        {
            case true when req.Thing.def.race != null:
            {
                if (req.Thing is not Pawn pawn || pawn.equipment?.Primary == null)
                {
                    return "";
                }

                var masteryWeaponComp = pawn.equipment.Primary.TryGetComp<MasteryWeaponComp>();
                if (masteryWeaponComp == null || !masteryWeaponComp.IsActive())
                {
                    return "";
                }

                num += masteryWeaponComp.GetCombinedBonus(parentStat);
                var currentOwner = masteryWeaponComp.GetCurrentOwner();
                if (currentOwner != null)
                {
                    var masteryPawnComp = masteryWeaponComp.GetCurrentOwner().TryGetComp<MasteryPawnComp>();
                    if (masteryPawnComp != null && masteryPawnComp.IsActive() &&
                        ModSettings.classes.TryGetValue(pawn.equipment.Primary.def, out var @class))
                    {
                        num += masteryPawnComp.GetStatBonus(@class,
                            parentStat);
                    }
                }

                break;
            }
            case true when req.Thing.def.HasComp(typeof(MasteryWeaponComp)):
            {
                var masteryWeaponComp2 = req.Thing.TryGetComp<MasteryWeaponComp>();
                if (masteryWeaponComp2 == null || !masteryWeaponComp2.IsActive())
                {
                    return "";
                }

                num += masteryWeaponComp2.GetCombinedBonus(parentStat);
                var currentOwner2 = masteryWeaponComp2.GetCurrentOwner();
                if (currentOwner2 != null)
                {
                    var masteryPawnComp2 = masteryWeaponComp2.GetCurrentOwner().TryGetComp<MasteryPawnComp>();
                    if (masteryPawnComp2 != null && masteryPawnComp2.IsActive() &&
                        ModSettings.classes.TryGetValue(req.Thing.def, out var @class))
                    {
                        num += masteryPawnComp2.GetStatBonus(@class, parentStat);
                    }
                }

                break;
            }
            default:
                num = 0f;
                break;
        }

        return num >= 0f
            ? $"{"SK_WeaponMastery_WeaponMasteryDescriptionItem".Translate()} + {parentStat.ValueToString(Mathf.Abs(num))}"
            : $"{"SK_WeaponMastery_WeaponMasteryDescriptionItem".Translate()} - {parentStat.ValueToString(Mathf.Abs(num))}";
    }
}