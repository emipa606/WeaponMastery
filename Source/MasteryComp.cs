using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SK_WeaponMastery
{

    //  Store pawns with a list of bonuses, experience and mastery level 
    // for all pawns that used this weapon
    public class MasteryComp : ThingComp
    {

        private Dictionary<Pawn, MasteryCompData> bonusStatsPerPawn;
        private Pawn currentOwner;
        private bool isActive = false;
        private string masteryDescription;
        private string weaponName;

        // These variables are needed for loading data from save file in PostExposeData()
        List<Pawn> dictKeysAsList;
        List<MasteryCompData> dictValuesAsList;

        public void Init(Pawn owner)
        {
            currentOwner = owner;
            bonusStatsPerPawn = new Dictionary<Pawn, MasteryCompData>();
            isActive = true;
        }

        // Add new stat bonus for a pawn
        public void AddStatBonus(Pawn pawn, StatDef bonusStat, float value)
        {
            if (!bonusStatsPerPawn.ContainsKey(pawn))
                bonusStatsPerPawn[pawn] = new MasteryCompData(pawn);

            bonusStatsPerPawn[pawn].AddStatBonus(bonusStat, value);
        }

        // Check if anyone mastered comp attached to this weapon
        public bool IsActive()
        {
            return isActive;
        }

        // Get stat bonus for a pawn
        public float GetStatBonus(Pawn pawn, StatDef stat)
        {
            if (!bonusStatsPerPawn.ContainsKey(pawn))
                return 0;
            return bonusStatsPerPawn[pawn].GetStatBonus(stat);
        }

        // Get stat bonus for current owner
        public float GetStatBonus(StatDef stat)
        {
            if (!bonusStatsPerPawn.ContainsKey(currentOwner))
                return 0;
            return bonusStatsPerPawn[currentOwner].GetStatBonus(stat);
        }

        public void AddExp(Pawn pawn, int experience)
        {
            if (!bonusStatsPerPawn.ContainsKey(pawn))
                bonusStatsPerPawn[pawn] = new MasteryCompData(pawn);
            // Update cached description when pawn levels up
            bonusStatsPerPawn[pawn].AddExp(experience, this.parent.def.IsMeleeWeapon, delegate (int level)
            {
                if (level == 1 && weaponName == null)
                {
                    weaponName = ModSettings.weaponNamesPool.RandomElement();
                    Messages.Message(ModSettings.messages.RandomElement().Translate(pawn.NameShortColored, weaponName), MessageTypeDefOf.NeutralEvent);
                }
                masteryDescription = GenerateDescription();
            });
        }

        // Save/Load comp data to/from rws file
        public override void PostExposeData()
        {
            base.PostExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                // Saving process, allow only active comps to be saved so
                // we don't fill save file with null comps
                if (isActive)
                {
                    List<Pawn> pawns = bonusStatsPerPawn.Keys.ToList();
                    List<MasteryCompData> data = bonusStatsPerPawn.Values.ToList();
                    Scribe_References.Look(ref currentOwner, "currentowner");
                    Scribe_Collections.Look(ref bonusStatsPerPawn, "bonusstatsperpawn", LookMode.Reference, LookMode.Deep, ref pawns, ref data);
                    Scribe_Values.Look(ref isActive, "isactive");
                    Scribe_Values.Look(ref weaponName, "weaponname");
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                // Loading process
                Scribe_Values.Look(ref isActive, "isactive", false);
                if (isActive)
                {
                    Scribe_References.Look(ref currentOwner, "currentowner");
                    Scribe_Collections.Look(ref bonusStatsPerPawn, "bonusstatsperpawn", LookMode.Reference, LookMode.Deep, ref dictKeysAsList, ref dictValuesAsList);
                    Scribe_Values.Look(ref weaponName, "weaponname");
                }
            }
        }

        public void SetCurrentOwner(Pawn newOwner)
        {
            currentOwner = newOwner;
        }

        public override bool AllowStackWith(Thing other)
        {
            return false;
        }

        // Weapon's name in GUI menus
        public override string TransformLabel(string label)
        {
            if (IsActive() && weaponName != null)
                return weaponName;
            return base.TransformLabel(label);
        }

        private string GenerateDescription()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(base.GetDescriptionPart());
            sb.AppendLine("SK_WeaponMastery_WeaponMasteryDescriptionItem".Translate());
            List<KeyValuePair<Pawn, MasteryCompData>> data = bonusStatsPerPawn.ToList();
            string positiveValueColumn = ": +";
            string negativeValueColumn = ": ";
            foreach (KeyValuePair<Pawn, MasteryCompData> item in data)
            {
                sb.AppendLine();
                sb.AppendLine(item.Key.Name.ToString());
                foreach (KeyValuePair<StatDef, float> statbonus in item.Value.GetStatBonusesAsList())
                    sb.AppendLine(" " + statbonus.Key.label.CapitalizeFirst() + (statbonus.Value >= 0f ? positiveValueColumn : negativeValueColumn) + statbonus.Key.ValueToString(statbonus.Value));
            }
            return sb.ToString();
        }

        // Weapon Description
        // Display all mastered pawns with their bonuses
        public override string GetDescriptionPart()
        {
            if (masteryDescription == null)
                masteryDescription = GenerateDescription();
            return masteryDescription;
        }
    }
}
