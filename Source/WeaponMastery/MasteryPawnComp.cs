using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace WeaponMastery;

internal class MasteryPawnComp : ThingComp
{
    private Dictionary<string, MasteryCompData> bonusStatsPerClass;

    private List<string> dictKeysAsList;

    private List<MasteryCompData> dictValuesAsList;

    private bool isActive;

    private string masteryDescription;

    public void Init()
    {
        bonusStatsPerClass = new Dictionary<string, MasteryCompData>();
        isActive = true;
    }

    public void AddStatBonus(string weaponClass, StatDef bonusStat, float value)
    {
        if (!bonusStatsPerClass.ContainsKey(weaponClass))
        {
            bonusStatsPerClass[weaponClass] = new MasteryCompData();
        }

        bonusStatsPerClass[weaponClass].AddStatBonus(bonusStat, value);
    }

    public bool IsActive()
    {
        return isActive;
    }

    public float GetStatBonus(string weaponClass, StatDef stat)
    {
        return !bonusStatsPerClass.ContainsKey(weaponClass) ? 0f : bonusStatsPerClass[weaponClass].GetStatBonus(stat);
    }

    public void AddExp(string weaponClass, int experience, bool isMelee)
    {
        if (!bonusStatsPerClass.ContainsKey(weaponClass))
        {
            bonusStatsPerClass[weaponClass] = new MasteryCompData();
        }

        var num = 1f;
        if (ModsConfig.RoyaltyActive && IsBondedWeapon())
        {
            num = ModSettings.bondedWeaponExperienceMultipier;
        }

        bonusStatsPerClass[weaponClass].AddExp((int)(experience * num), isMelee, delegate { GenerateDescription(); });
    }

    private bool IsBondedWeapon()
    {
        var pawn = parent as Pawn;
        return pawn?.equipment?.Primary?.TryGetComp<CompBladelinkWeapon>()?.CodedPawn == pawn;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        switch (Scribe.mode)
        {
            case LoadSaveMode.Saving when !isActive:
                return;
            case LoadSaveMode.Saving:
            {
                var list = bonusStatsPerClass.Keys.ToList();
                var list2 = bonusStatsPerClass.Values.ToList();
                if (bonusStatsPerClass != null)
                {
                    Scribe_Collections.Look(ref bonusStatsPerClass, "bonusstatsperclass", LookMode.Value, LookMode.Deep,
                        ref list, ref list2);
                }

                Scribe_Values.Look(ref isActive, "isactive");
                break;
            }
            case LoadSaveMode.LoadingVars or LoadSaveMode.ResolvingCrossRefs:
            {
                Scribe_Values.Look(ref isActive, "isactive");
                if (isActive)
                {
                    Scribe_Collections.Look(ref bonusStatsPerClass, "bonusstatsperclass", LookMode.Value, LookMode.Deep,
                        ref dictKeysAsList, ref dictValuesAsList);
                }

                break;
            }
            case LoadSaveMode.PostLoadInit when isActive:
            {
                if (bonusStatsPerClass == null)
                {
                    bonusStatsPerClass = new Dictionary<string, MasteryCompData>();
                }
                else if (AnyWeaponHasMastery())
                {
                    GenerateDescription();
                }

                break;
            }
        }
    }

    public void GenerateDescription()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("SK_WeaponMastery_WeaponMasteryDescriptionItem".Translate());
        var list = bonusStatsPerClass.ToList();
        var text = ": +";
        var text2 = ": ";
        foreach (var keyValuePair in list)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"{Utils.Capitalize(keyValuePair.Key)}: ");
            foreach (var statBonusesAs in keyValuePair.Value.GetStatBonusesAsList())
            {
                stringBuilder.AppendLine(
                    $" {statBonusesAs.Key.label.CapitalizeFirst()}{(statBonusesAs.Value >= 0f ? text : text2)}{statBonusesAs.Key.ValueToString(statBonusesAs.Value)}");
            }

            if (ModSettings.displayExperience && !keyValuePair.Value.IsMaxLevel())
            {
                stringBuilder.AppendLine(
                    $"Level: {keyValuePair.Value.GetMasteryLevel()}  Experience: {keyValuePair.Value.GetExperience()}/{ModSettings.GetExperienceForLevel(keyValuePair.Value.GetMasteryLevel())}");
            }
        }

        stringBuilder.AppendLine();
        masteryDescription = stringBuilder.ToString();
    }

    private bool AnyWeaponHasMastery()
    {
        var list = bonusStatsPerClass.ToList();
        foreach (var keyValuePair in list)
        {
            if (keyValuePair.Value.HasMastery())
            {
                return true;
            }
        }

        return false;
    }

    public string GetDescription()
    {
        if (!isActive || !AnyWeaponHasMastery() && !ModSettings.displayExperience)
        {
            return base.GetDescriptionPart();
        }

        return masteryDescription;
    }

    public void SetLevel(string weaponClass, int level)
    {
        bonusStatsPerClass[weaponClass].SetMasteryLevel(level);
    }
}