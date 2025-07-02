using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace WeaponMastery;

public class MasteryWeaponComp : ThingComp
{
    private Dictionary<Pawn, MasteryWeaponCompData> bonusStatsPerPawn;

    private Pawn currentOwner;

    private List<Pawn> dictKeysAsList;

    private List<MasteryWeaponCompData> dictValuesAsList;

    private bool isActive;

    private string masteryDescription;

    private Dictionary<StatDef, float> relicBonuses;

    private string weaponName;

    public void Init()
    {
        bonusStatsPerPawn = new Dictionary<Pawn, MasteryWeaponCompData>();
        if (ModsConfig.IdeologyActive && parent.IsRelic())
        {
            relicBonuses = new Dictionary<StatDef, float>();
            for (var i = 0; i < ModSettings.numberOfRelicBonusStats; i++)
            {
                var masteryStat = ModSettings.PickBonus(parent.def.IsMeleeWeapon);
                if (masteryStat == null)
                {
                    continue;
                }

                if (relicBonuses.ContainsKey(masteryStat.GetStat()))
                {
                    relicBonuses[masteryStat.GetStat()] += masteryStat.GetOffset();
                }
                else
                {
                    relicBonuses[masteryStat.GetStat()] = masteryStat.GetOffset();
                }
            }
        }

        isActive = true;
    }

    public void AddStatBonus(Pawn pawn, StatDef bonusStat, float value)
    {
        if (!bonusStatsPerPawn.ContainsKey(pawn))
        {
            bonusStatsPerPawn[pawn] = new MasteryWeaponCompData(pawn);
        }

        bonusStatsPerPawn[pawn].AddStatBonus(bonusStat, value);
    }

    public bool IsActive()
    {
        return isActive;
    }

    private float getStatBonus(StatDef stat)
    {
        return !bonusStatsPerPawn.ContainsKey(currentOwner) ? 0f : bonusStatsPerPawn[currentOwner].GetStatBonus(stat);
    }

    private float getStatBonusRelic(StatDef stat)
    {
        if (relicBonuses == null || !relicBonuses.ContainsKey(stat) || !ownerBelievesInSameIdeology())
        {
            return 0f;
        }

        return relicBonuses[stat];
    }

    public float GetCombinedBonus(StatDef stat)
    {
        if (currentOwner == null)
        {
            return 0f;
        }

        return getStatBonus(stat) + getStatBonusRelic(stat);
    }

    private bool ownerBelievesInSameIdeology()
    {
        return parent.StyleSourcePrecept?.ideo == currentOwner.Ideo;
    }

    public void AddExp(Pawn pawn, int experience)
    {
        if (!bonusStatsPerPawn.ContainsKey(pawn))
        {
            bonusStatsPerPawn[pawn] = new MasteryWeaponCompData(pawn);
        }

        var num = 1f;
        if (ModsConfig.RoyaltyActive && isBondedWeapon())
        {
            num = ModSettings.bondedWeaponExperienceMultipier;
        }

        bonusStatsPerPawn[pawn].AddExp((int)(experience * num), parent.def.IsMeleeWeapon, delegate(int level)
        {
            var value = Rand.Value;
            if (level == 1)
            {
                if (ModSettings.useMoods)
                {
                    var despised = true;
                    if (ModsConfig.IdeologyActive)
                    {
                        var ideo = pawn.Ideo;
                        if (ideo != null && ideo.GetDispositionForWeapon(parent.def) == IdeoWeaponDisposition.Despised)
                        {
                            despised = false;
                        }
                    }

                    if (despised)
                    {
                        pawn.health.AddHediff(Core.MasteredWeaponEquipped);
                    }
                }

                if (weaponName == null && value <= ModSettings.chanceToNameWeapon)
                {
                    weaponName = ModSettings.keepOriginalWeaponNameQuality
                        ? $"\"{ModSettings.PickWeaponName()}\" {parent.LabelCap}"
                        : ModSettings.PickWeaponName();

                    Messages.Message(ModSettings.messages.RandomElement().Translate(pawn.NameShortColored, weaponName),
                        MessageTypeDefOf.NeutralEvent);
                }
            }

            GenerateDescription();
        });
    }

    private bool isBondedWeapon()
    {
        return parent.TryGetComp<CompBladelinkWeapon>()?.CodedPawn == currentOwner;
    }

    private void filterNullPawns()
    {
        var list = bonusStatsPerPawn.ToList();
        bonusStatsPerPawn.Clear();
        foreach (var keyValuePair in list)
        {
            if (!keyValuePair.Key.DestroyedOrNull())
            {
                bonusStatsPerPawn.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref currentOwner, "currentowner");
        switch (Scribe.mode)
        {
            case LoadSaveMode.Saving when !isActive:
                return;
            case LoadSaveMode.Saving:
            {
                filterNullPawns();
                if (bonusStatsPerPawn == null && weaponName == null && relicBonuses == null)
                {
                    isActive = false;
                    return;
                }

                if (bonusStatsPerPawn is { Count: > 0 })
                {
                    var list = bonusStatsPerPawn.Keys.ToList();
                    var list2 = bonusStatsPerPawn.Values.ToList();
                    Scribe_Collections.Look(ref bonusStatsPerPawn, "bonusstatsperpawn", LookMode.Reference,
                        LookMode.Deep,
                        ref list, ref list2);
                }

                Scribe_Values.Look(ref isActive, "isactive");
                if (weaponName != null)
                {
                    Scribe_Values.Look(ref weaponName, "weaponname");
                }

                if (parent.IsRelic() && relicBonuses != null)
                {
                    Scribe_Collections.Look(ref relicBonuses, "relicbonuses", LookMode.Def, LookMode.Value);
                }

                break;
            }
            case LoadSaveMode.LoadingVars or LoadSaveMode.ResolvingCrossRefs:
            {
                Scribe_Values.Look(ref isActive, "isactive");
                if (isActive)
                {
                    Scribe_Collections.Look(ref bonusStatsPerPawn, "bonusstatsperpawn", LookMode.Reference,
                        LookMode.Deep,
                        ref dictKeysAsList, ref dictValuesAsList);
                    Scribe_Values.Look(ref weaponName, "weaponname");
                    Scribe_Collections.Look(ref relicBonuses, "relicbonuses", LookMode.Def, LookMode.Value);
                }

                break;
            }
            case LoadSaveMode.PostLoadInit when isActive:
            {
                if (bonusStatsPerPawn != null && anyPawnHasMastery())
                {
                    GenerateDescription();
                }
                else if (bonusStatsPerPawn == null)
                {
                    bonusStatsPerPawn = new Dictionary<Pawn, MasteryWeaponCompData>();
                }

                break;
            }
        }
    }

    public void SetCurrentOwner(Pawn newOwner)
    {
        currentOwner = newOwner;
    }

    public Pawn GetCurrentOwner()
    {
        return currentOwner;
    }

    public override bool AllowStackWith(Thing other)
    {
        return false;
    }

    public override string TransformLabel(string label)
    {
        if (isActive && weaponName != null)
        {
            return weaponName;
        }

        return base.TransformLabel(label);
    }

    public void GenerateDescription()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(parent.def.LabelCap);
        stringBuilder.AppendLine(base.GetDescriptionPart());
        stringBuilder.AppendLine("SK_WeaponMastery_WeaponMasteryDescriptionItem".Translate());
        var list = bonusStatsPerPawn.ToList();
        var text = ": +";
        var text2 = ": ";
        foreach (var keyValuePair in list)
        {
            if (keyValuePair.Key == null)
            {
                continue;
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(keyValuePair.Key.Name.ToString());
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
        if (relicBonuses != null)
        {
            stringBuilder.AppendLine("SK_WeaponMastery_WeaponMasteryDescriptionTitleRelicBonus".Translate());
            foreach (var item in relicBonuses.ToList())
            {
                stringBuilder.AppendLine(
                    $" {item.Key.label.CapitalizeFirst()}{(item.Value >= 0f ? text : text2)}{item.Key.ValueToString(item.Value)}");
            }
        }

        masteryDescription = stringBuilder.ToString();
    }

    public override string GetDescriptionPart()
    {
        if (!isActive || !anyPawnHasMastery() && !ModSettings.displayExperience)
        {
            return base.GetDescriptionPart();
        }

        return masteryDescription;
    }

    public void GenerateWeaponName()
    {
        weaponName = ModSettings.keepOriginalWeaponNameQuality
            ? $"\"{ModSettings.PickWeaponName()}\" {parent.LabelCap}"
            : ModSettings.PickWeaponName();
    }

    public bool PawnHasMastery(Pawn pawn)
    {
        if (bonusStatsPerPawn == null || !bonusStatsPerPawn.ContainsKey(pawn))
        {
            return false;
        }

        return bonusStatsPerPawn[pawn].HasMastery();
    }

    private bool anyPawnHasMastery()
    {
        var list = bonusStatsPerPawn?.ToList();
        if (list == null)
        {
            return false;
        }

        foreach (var keyValuePair in list)
        {
            if (keyValuePair.Value?.HasMastery() == true)
            {
                return true;
            }
        }

        return false;
    }

    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        if (!isActive)
        {
            return;
        }

        SetCurrentOwner(pawn);
        if (!ModSettings.useMoods || !PawnHasMastery(pawn))
        {
            return;
        }

        if (ModsConfig.IdeologyActive)
        {
            var ideo = pawn.Ideo;
            if (ideo != null && ideo.GetDispositionForWeapon(parent.def) == IdeoWeaponDisposition.Despised)
            {
                return;
            }
        }

        pawn.health.AddHediff(Core.MasteredWeaponEquipped);
        pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(Core.MasteredWeaponUnequipped);
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);
        if (!isActive || !ModSettings.useMoods || !PawnHasMastery(pawn))
        {
            return;
        }

        if (ModsConfig.IdeologyActive)
        {
            var ideo = pawn.Ideo;
            if (ideo != null && ideo.GetDispositionForWeapon(parent.def) == IdeoWeaponDisposition.Despised)
            {
                return;
            }
        }

        if (SimpleSidearmsCompat.enabled &&
            (SimpleSidearmsCompat.weaponSwitch || SimpleSidearmsCompat.PawnHasAnyMasteredWeapon(pawn)) || pawn.Dead)
        {
            return;
        }

        pawn.needs.mood.thoughts.memories.TryGainMemory(Core.MasteredWeaponUnequipped);
        if (pawn.health.hediffSet.HasHediff(Core.MasteredWeaponEquipped))
        {
            pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(Core.MasteredWeaponEquipped));
        }
    }

    public void SetLevel(Pawn pawn, int level)
    {
        bonusStatsPerPawn[pawn].SetMasteryLevel(level);
    }
}