using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace SK_WeaponMastery
{

    // Store a list of bonuses, experience and mastery level for a single pawn
    public class MasteryCompData : IExposable
    {
        private Dictionary<StatDef, float> bonusStats;
        private Pawn pawn;
        private int masteryLevel = 0;
        private int experience = 0;

        public MasteryCompData() { }

        public MasteryCompData(Pawn pawn)
        {
            this.pawn = pawn;
            bonusStats = new Dictionary<StatDef, float>();
        }

        public void AddStatBonus(StatDef stat, float value)
        {
            if (!bonusStats.ContainsKey(stat))
                bonusStats[stat] = 0;
            bonusStats[stat] += value;
        }

        public float GetStatBonus(StatDef stat)
        {
            if (!bonusStats.ContainsKey(stat)) return 0;
            return bonusStats[stat];
        }

        public void AddExp(int experience, bool isMelee, Action<int> postLevelUp = null)
        {
            if (IsMaxLevel()) return;

            this.experience += experience;
            if (this.experience >= ModSettings.GetExperienceForLevel(masteryLevel))
            {
                this.experience -= ModSettings.GetExperienceForLevel(masteryLevel);
                masteryLevel += 1;
                RollBonusStat(isMelee);
                postLevelUp?.Invoke(masteryLevel);
            }
        }

        private bool IsMaxLevel()
        {
            return masteryLevel == ModSettings.maxLevel;
        }

        private void RollBonusStat(bool isMelee)
        {
            MasteryStat bonus = ModSettings.PickBonus(isMelee);
            AddStatBonus(bonus.GetStat(), bonus.GetOffset());
        }

        // Save/Load class variables to/from rws file
        // Store defName string instead of defs to handle defs being removed
        // mid game.
        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                List<KeyValuePair<StatDef, float>> list = bonusStats.ToList();
                List<float> floats = new List<float>();
                List<StatDef> defs = new List<StatDef>();
                foreach (KeyValuePair<StatDef, float> item in list)
                {
                    floats.Add(item.Value);
                    defs.Add(item.Key);
                }
                Scribe_Collections.Look(ref floats, "floats", LookMode.Value);
                Scribe_Collections.Look(ref defs, "defs", LookMode.Def);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<float> floats = new List<float>();
                List<StatDef> defs = new List<StatDef>();
                bonusStats = new Dictionary<StatDef, float>();
                Scribe_Collections.Look(ref floats, "floats", LookMode.Value);
                Scribe_Collections.Look(ref defs, "defs", LookMode.Def);
                for (int i = 0; i < defs.Count; i++)
                    if (defs[i] != null)
                        bonusStats.Add(defs[i], floats[i]);
            }
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref masteryLevel, "masterylevel");
            Scribe_Values.Look(ref experience, "experience");
        }

        public List<KeyValuePair<StatDef, float>> GetStatBonusesAsList()
        {
            return bonusStats.ToList();
        }
    }
}
