using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WeaponMastery;

public class MasteryCompData : IExposable
{
    protected Dictionary<StatDef, float> bonusStats = new();

    protected int experience;

    protected int masteryLevel;

    public virtual void ExposeData()
    {
        switch (Scribe.mode)
        {
            case LoadSaveMode.Saving:
            {
                var list = bonusStats.ToList();
                var list2 = new List<float>();
                var list3 = new List<StatDef>();
                foreach (var item in list)
                {
                    list2.Add(item.Value);
                    list3.Add(item.Key);
                }

                Scribe_Collections.Look(ref list2, "floats", LookMode.Value);
                Scribe_Collections.Look(ref list3, "defs", LookMode.Def);
                break;
            }
            case LoadSaveMode.LoadingVars:
            {
                var list4 = new List<float>();
                var list5 = new List<StatDef>();
                bonusStats = new Dictionary<StatDef, float>();
                Scribe_Collections.Look(ref list4, "floats", LookMode.Value);
                Scribe_Collections.Look(ref list5, "defs", LookMode.Def);
                for (var i = 0; i < list5.Count; i++)
                {
                    if (list5[i] != null)
                    {
                        bonusStats.Add(list5[i], list4[i]);
                    }
                }

                break;
            }
        }

        Scribe_Values.Look(ref masteryLevel, "masterylevel");
        Scribe_Values.Look(ref experience, "experience");
    }

    public void AddStatBonus(StatDef stat, float value)
    {
        bonusStats.TryAdd(stat, 0f);

        bonusStats[stat] += value;
    }

    public float GetStatBonus(StatDef stat)
    {
        return bonusStats.GetValueOrDefault(stat, 0f);
    }

    public void AddExp(int experience, bool isMelee, Action<int> postLevelUp = null)
    {
        if (IsMaxLevel())
        {
            return;
        }

        this.experience += experience;
        if (this.experience < ModSettings.GetExperienceForLevel(masteryLevel))
        {
            return;
        }

        this.experience -= ModSettings.GetExperienceForLevel(masteryLevel);
        masteryLevel++;
        rollBonusStat(isMelee);
        postLevelUp?.Invoke(masteryLevel);
    }

    public bool IsMaxLevel()
    {
        return masteryLevel == ModSettings.maxLevel;
    }

    private void rollBonusStat(bool isMelee)
    {
        var masteryStat = ModSettings.PickBonus(isMelee);
        if (masteryStat != null)
        {
            AddStatBonus(masteryStat.GetStat(), masteryStat.GetOffset());
        }
    }

    public bool HasMastery()
    {
        return masteryLevel != 0;
    }

    public List<KeyValuePair<StatDef, float>> GetStatBonusesAsList()
    {
        return bonusStats.ToList();
    }

    public int GetExperience()
    {
        return experience;
    }

    public int GetMasteryLevel()
    {
        return masteryLevel;
    }

    public void SetMasteryLevel(int value)
    {
        masteryLevel = value;
    }
}