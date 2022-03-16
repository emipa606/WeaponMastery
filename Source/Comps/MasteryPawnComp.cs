using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SK_WeaponMastery
{
    class MasteryPawnComp : ThingComp
    {
        private Dictionary<ThingDef, MasteryCompData> bonusStatsPerWeapon;
        private bool isActive = false;
        private string masteryDescription;

        // These variables are needed for loading data from save file in PostExposeData()
        List<ThingDef> dictKeysAsList;
        List<MasteryCompData> dictValuesAsList;

        public void Init()
        {
            bonusStatsPerWeapon = new Dictionary<ThingDef, MasteryCompData>();
            isActive = true;
        }

        // Add new stat bonus for a weaoon
        public void AddStatBonus(ThingDef weapon, StatDef bonusStat, float value)
        {
            if (!bonusStatsPerWeapon.ContainsKey(weapon))
                bonusStatsPerWeapon[weapon] = new MasteryCompData();

            bonusStatsPerWeapon[weapon].AddStatBonus(bonusStat, value);
        }

        // Check if comp was init
        public bool IsActive()
        {
            return isActive;
        }

        // Get stat bonus for current weapon
        public float GetStatBonus(ThingDef weapon, StatDef stat)
        {
            if (!bonusStatsPerWeapon.ContainsKey(weapon))
                return 0;
            return bonusStatsPerWeapon[weapon].GetStatBonus(stat);
        }

        public void AddExp(ThingDef weapon, int experience)
        {
            if (!bonusStatsPerWeapon.ContainsKey(weapon))
                bonusStatsPerWeapon[weapon] = new MasteryCompData();

            float multiplier = 1;
            if (ModsConfig.RoyaltyActive && IsBondedWeapon()) multiplier = ModSettings.bondedWeaponExperienceMultipier;

            bonusStatsPerWeapon[weapon].AddExp((int)(experience * multiplier), weapon.IsMeleeWeapon, delegate (int level)
            {
                // Update cached description when pawn levels up
                GenerateDescription();
            });
        }

        // Check if weapon is bonded to current owner
        private bool IsBondedWeapon()
        {
            Pawn parent = this.parent as Pawn;
            CompBladelinkWeapon link = parent?.equipment?.Primary?.TryGetComp<CompBladelinkWeapon>();
            return link?.CodedPawn == parent;
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
                    List<ThingDef> weapons = bonusStatsPerWeapon.Keys.ToList();
                    List<MasteryCompData> data = bonusStatsPerWeapon.Values.ToList();
                    if (bonusStatsPerWeapon != null)
                        Scribe_Collections.Look(ref bonusStatsPerWeapon, "bonusstatsperweapon", LookMode.Def, LookMode.Deep, ref weapons, ref data);
                    Scribe_Values.Look(ref isActive, "isactive");
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
            {
                // Loading process
                Scribe_Values.Look(ref isActive, "isactive", false);
                if (isActive)
                    Scribe_Collections.Look(ref bonusStatsPerWeapon, "bonusstatsperweapon", LookMode.Def, LookMode.Deep, ref dictKeysAsList, ref dictValuesAsList);
            }
            else if (Scribe.mode == LoadSaveMode.PostLoadInit && isActive)
                if (AnyWeaponHasMastery()) GenerateDescription();
        }

        private void GenerateDescription()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("SK_WeaponMastery_WeaponMasteryDescriptionItem".Translate());
            List<KeyValuePair<ThingDef, MasteryCompData>> data = bonusStatsPerWeapon.ToList();
            string positiveValueColumn = ": +";
            string negativeValueColumn = ": ";
            foreach (KeyValuePair<ThingDef, MasteryCompData> item in data)
            {
                sb.AppendLine();
                sb.AppendLine($"{item.Key.LabelCap}: ");
                foreach (KeyValuePair<StatDef, float> statbonus in item.Value.GetStatBonusesAsList())
                    sb.AppendLine(" " + statbonus.Key.label.CapitalizeFirst() + (statbonus.Value >= 0f ? positiveValueColumn : negativeValueColumn) + statbonus.Key.ValueToString(statbonus.Value));
            }
            sb.AppendLine();
            masteryDescription = sb.ToString();
        }

        private bool AnyWeaponHasMastery()
        {
            List<KeyValuePair<ThingDef, MasteryCompData>> data = bonusStatsPerWeapon.ToList();
            foreach (KeyValuePair<ThingDef, MasteryCompData> item in data)
                if (item.Value.HasMastery()) return true;

            return false;
        }

        public string GetDescription()
        {
            if (!isActive || !AnyWeaponHasMastery()) return base.GetDescriptionPart();
            return masteryDescription;
        }
    }
}
