using RimWorld;
using Verse;

namespace SK_WeaponMastery
{
    /*
     Stat Defs contain a list of stat parts which affect the final stat value
     calculation.
    
     This class is added into each vanilla Stat Def stat parts list 
     in order to modify the final stat value calculations.

     Class tries to find MasteryComp, gets the necessary bonus value
     and modifies calculation accordingly.
    */
    class StatPart_Mastery : StatPart
    {
        public StatPart_Mastery(StatDef parentStat)
        {
            this.parentStat = parentStat;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (!req.HasThing) return;

            if (req.Thing.def.race != null)
            {
                // Stat bonuses for pawn
                Pawn pawn = req.Thing as Pawn;
                if (pawn == null || pawn.equipment?.Primary == null) return;
                MasteryComp comp = pawn.equipment.Primary.TryGetComp<MasteryComp>();
                if (comp == null || !comp.IsActive()) return;
                val += comp.GetStatBonus(pawn, parentStat);
            }
            else if (req.Thing.def.HasComp(typeof(MasteryComp)))
            {
                // Stat bonuses for gun
                MasteryComp comp = req.Thing.TryGetComp<MasteryComp>();
                if (comp == null || !comp.IsActive()) return;
                val += comp.GetStatBonus(parentStat);
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            float bonus;
            if (req.Thing.def.race != null)
            {
                // Stat bonuses for pawn
                Pawn pawn = req.Thing as Pawn;
                if (pawn == null || pawn.equipment?.Primary == null) return "";
                MasteryComp comp = pawn.equipment.Primary.TryGetComp<MasteryComp>();
                if (comp == null || !comp.IsActive()) return "";
                bonus = comp.GetStatBonus(pawn, parentStat);
            }
            else if (req.Thing.def.HasComp(typeof(MasteryComp)))
            {
                // Stat bonuses for gun
                MasteryComp comp = req.Thing.TryGetComp<MasteryComp>();
                if (comp == null || !comp.IsActive()) return "";
                bonus = comp.GetStatBonus(parentStat);
            }
            else bonus = 0;

            return bonus >= 0f ? 
                "SK_WeaponMastery_WeaponMasteryDescriptionItem".Translate().ToString() + " + " + parentStat.ValueToString(UnityEngine.Mathf.Abs(bonus)) : 
                "SK_WeaponMastery_WeaponMasteryDescriptionItem".Translate().ToString() + " - " + parentStat.ValueToString(UnityEngine.Mathf.Abs(bonus));
        }
    }
}
