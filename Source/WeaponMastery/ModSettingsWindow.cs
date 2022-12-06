using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse;

namespace WeaponMastery;

[StaticConstructorOnStartup]
public static class ModSettingsWindow
{
    public const int MAX_LEVELS = 100;
    public static int selectedLevelIndex;

    public static int selectedLevelValue;

    private static int selectedWeaponIndex;

    private static int selectedClassIndex;

    private static string selectedLevelStringBuffer;

    private static string customNameInputStringBuffer;

    private static string addClassInputStringBuffer;

    private static string removeClassInputStringBuffer;

    private static string selectedCustomName;

    private static ModSettingsWindowThread thread;

    private static List<StatDef> allStatDefs;

    private static List<ThingDef> allWeapons;

    private static List<string> allClasses;

    private static StatDef selectedRangedDef;

    private static StatDef selectedMeleeDef;

    public static MasteryStat selectedRangedMasteryStat;

    public static MasteryStat selectedMeleeMasteryStat;

    private static Texture2D cachedWeaponTexture;

    public static float rangedStatOffset;

    public static float meleeStatOffset;

    public static bool isRangedStatEnabled;

    public static bool isMeleeStatEnabled;

    public static bool isRangedStatPercentage;

    public static bool isMeleeStatPercentage;

    public static bool isOpen;

    public static bool initSemaphore;

    private static Vector2 scrollPosition = Vector2.zero;

    private static readonly int DEFAULT_EXP = 15000;

    private static readonly float DEFAULT_STAT_OFFSET = 1f;

    private static readonly float MIN_STAT_BONUS = -5f;

    private static readonly float MAX_STAT_BONUS = 5f;

    private static readonly float MIN_BONDED_WEAPON_MULTIPLIER = 1f;

    private static readonly float MAX_BONDED_WEAPON_MULTIPLIER = 3f;

    private static readonly int MIN_RELIC_BONUS_STATS = 1;

    private static readonly int MAX_RELIC_BONUS_STATS = 10;

    public static void Init()
    {
        if (initSemaphore)
        {
            return;
        }

        initSemaphore = true;
        isOpen = true;
        selectedLevelIndex = 0;
        selectedWeaponIndex = 0;
        selectedClassIndex = 0;
        selectedLevelValue = ModSettings.experiencePerLevel[0];
        allStatDefs = DefDatabase<StatDef>.AllDefs.ToList();
        selectedRangedDef = allStatDefs[0];
        selectedMeleeDef = allStatDefs[0];
        isRangedStatPercentage = IsStatDefPercentage(selectedRangedDef);
        isMeleeStatPercentage = IsStatDefPercentage(selectedMeleeDef);
        allWeapons = Core.GetModWeapons().ToList();
        cachedWeaponTexture = ContentFinder<Texture2D>.Get(allWeapons[selectedWeaponIndex].graphicData.texPath);
        allClasses = new List<string>();
        foreach (var value in ModSettings.classes.Values)
        {
            if (!allClasses.Contains(value))
            {
                allClasses.Add(value);
            }
        }

        selectedRangedMasteryStat = ModSettings.FindStatWithStatDef(allStatDefs[0], false);
        if (selectedRangedMasteryStat != null)
        {
            rangedStatOffset = selectedRangedMasteryStat.GetOffset();
            isRangedStatEnabled = true;
        }
        else
        {
            rangedStatOffset = DEFAULT_STAT_OFFSET;
            isRangedStatEnabled = false;
        }

        selectedMeleeMasteryStat = ModSettings.FindStatWithStatDef(allStatDefs[0], true);
        if (selectedMeleeMasteryStat != null)
        {
            meleeStatOffset = selectedMeleeMasteryStat.GetOffset();
            isMeleeStatEnabled = true;
        }
        else
        {
            meleeStatOffset = DEFAULT_STAT_OFFSET;
            isMeleeStatEnabled = false;
        }

        if (ModSettings.customWeaponNamesPool.Count > 0)
        {
            selectedCustomName = ModSettings.customWeaponNamesPool[0];
        }

        thread = new ModSettingsWindowThread(selectedLevelValue, rangedStatOffset, meleeStatOffset);
        new Thread(thread.Run).Start();
        initSemaphore = false;
    }

    public static void Draw(Rect parent)
    {
        if (!isOpen)
        {
            Init();
        }

        if (initSemaphore)
        {
            return;
        }

        parent.yMin += 15f;
        parent.yMax -= 15f;
        var listing_Standard = new Listing_Standard(GameFont.Medium);
        var rect = new Rect(parent.x, parent.y + 20f, parent.width, parent.height - 20f);
        var outRect = new Rect(rect);
        outRect.x += 5f;
        outRect.width -= 35f;
        outRect.y += 10f;
        outRect.height -= 20f;
        Widgets.DrawBox(rect, 1, Texture2D.whiteTexture);
        var rect2 = new Rect(0f, outRect.y, outRect.width - 20f, (parent.height * 3f) + 50f);
        Widgets.DrawMenuSection(rect);
        Widgets.BeginScrollView(outRect, ref scrollPosition, rect2);
        listing_Standard.Begin(rect2);
        DrawModOptionsSection(listing_Standard);
        DrawExperiencePerLevelSection(listing_Standard);
        DrawStatsSection(listing_Standard);
        DrawCustomNamesSection(listing_Standard);
        DrawGeneralMasterySection(listing_Standard);
        listing_Standard.End();
        Widgets.EndScrollView();
    }

    private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateLevelOptions()
    {
        for (var i = 0; i < ModSettings.maxLevel; i++)
        {
            var localCopyOfI = i;
            yield return new Widgets.DropdownMenuElement<string>
            {
                option = new FloatMenuOption(
                    "SK_WeaponMastery_LevelSectionDropdownOptionLabel".Translate(localCopyOfI + 1), delegate
                    {
                        selectedLevelIndex = localCopyOfI;
                        selectedLevelValue = ModSettings.experiencePerLevel[localCopyOfI];
                        selectedLevelStringBuffer = selectedLevelValue.ToString();
                    }),
                payload = "SK_WeaponMastery_LevelSectionLevelExpLabel".Translate(localCopyOfI + 1)
            };
        }
    }

    private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateRangedStatsOptions()
    {
        for (var i = 0; i < allStatDefs.Count; i++)
        {
            var localCopyOfI = i;
            yield return new Widgets.DropdownMenuElement<string>
            {
                option = new FloatMenuOption(allStatDefs[localCopyOfI].defName, delegate
                {
                    selectedRangedDef = allStatDefs[localCopyOfI];
                    isRangedStatPercentage = IsStatDefPercentage(selectedRangedDef);
                    var masteryStat = ModSettings.FindStatWithStatDef(allStatDefs[localCopyOfI], false);
                    if (masteryStat != null)
                    {
                        selectedRangedMasteryStat = masteryStat;
                        rangedStatOffset = masteryStat.GetOffset();
                        isRangedStatEnabled = true;
                    }
                    else
                    {
                        isRangedStatEnabled = false;
                    }
                }),
                payload = allStatDefs[localCopyOfI].defName
            };
        }
    }

    private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateMeleeStatsOptions()
    {
        for (var i = 0; i < allStatDefs.Count; i++)
        {
            var localCopyOfI = i;
            yield return new Widgets.DropdownMenuElement<string>
            {
                option = new FloatMenuOption(allStatDefs[localCopyOfI].defName, delegate
                {
                    selectedMeleeDef = allStatDefs[localCopyOfI];
                    isMeleeStatPercentage = IsStatDefPercentage(selectedMeleeDef);
                    var masteryStat = ModSettings.FindStatWithStatDef(allStatDefs[localCopyOfI], true);
                    if (masteryStat != null)
                    {
                        selectedMeleeMasteryStat = masteryStat;
                        meleeStatOffset = masteryStat.GetOffset();
                        isMeleeStatEnabled = true;
                    }
                    else
                    {
                        isMeleeStatEnabled = false;
                    }
                }),
                payload = allStatDefs[localCopyOfI].defName
            };
        }
    }

    private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateCustomNameOptions()
    {
        for (var i = 0; i < ModSettings.customWeaponNamesPool.Count; i++)
        {
            var localCopyOfI = i;
            yield return new Widgets.DropdownMenuElement<string>
            {
                option = new FloatMenuOption(ModSettings.customWeaponNamesPool[localCopyOfI],
                    delegate { selectedCustomName = ModSettings.customWeaponNamesPool[localCopyOfI]; }),
                payload = ModSettings.customWeaponNamesPool[localCopyOfI]
            };
        }
    }

    private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateWeaponOptions()
    {
        for (var i = 0; i < allWeapons.Count; i++)
        {
            var localCopyOfI = i;
            yield return new Widgets.DropdownMenuElement<string>
            {
                option = new FloatMenuOption(allWeapons[localCopyOfI].defName, delegate
                {
                    selectedWeaponIndex = localCopyOfI;
                    cachedWeaponTexture =
                        ContentFinder<Texture2D>.Get(allWeapons[localCopyOfI].graphicData.texPath);
                }, allWeapons[localCopyOfI]),
                payload = allWeapons[localCopyOfI].LabelCap
            };
        }
    }

    private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateClassOptions()
    {
        for (var i = 0; i < allClasses.Count; i++)
        {
            var localCopyOfI = i;
            yield return new Widgets.DropdownMenuElement<string>
            {
                option = new FloatMenuOption(allClasses[localCopyOfI], delegate { selectedClassIndex = localCopyOfI; }),
                payload = allClasses[localCopyOfI]
            };
        }
    }

    private static void DrawExperiencePerLevelSection(Listing_Standard list)
    {
        var rect = list.GetRect(100f);
        rect.width -= 30f;
        var listing_Standard = new Listing_Standard();
        listing_Standard.Begin(rect);
        listing_Standard.Label("SK_WeaponMastery_LevelSectionTitle".Translate());
        listing_Standard.Gap();
        var rect2 = listing_Standard.Label("SK_WeaponMastery_LevelSectionDetail".Translate());
        var rect3 = new Rect(rect.x, rect2.y + 30f, 100f, 20f);
        Widgets.Dropdown(rect3, null, null, (string _) => GenerateLevelOptions(),
            "SK_WeaponMastery_LevelSectionDropdownOptionLabel".Translate(selectedLevelIndex + 1));
        var rect4 = new Rect(rect3.x + 110f, rect2.y + 30f, 900f, 30f);
        var listing_Standard2 = new Listing_Standard();
        listing_Standard2.Begin(rect4);
        listing_Standard2.ColumnWidth = 230f;
        listing_Standard2.Label("SK_WeaponMastery_LevelSectionLevelExpLabel".Translate(selectedLevelIndex + 1));
        listing_Standard2.NewColumn();
        listing_Standard2.ColumnWidth = 160f;
        listing_Standard2.TextFieldNumeric(ref selectedLevelValue, ref selectedLevelStringBuffer);
        if (ModSettings.maxLevel != 100)
        {
            listing_Standard2.NewColumn();
            listing_Standard2.ColumnWidth = 100f;
            if (listing_Standard2.ButtonText("SK_WeaponMastery_LevelSectionAddButton".Translate()))
            {
                OnAddLevel();
            }
        }

        if (ModSettings.maxLevel != 1)
        {
            listing_Standard2.NewColumn();
            listing_Standard2.ColumnWidth = 120f;
            if (listing_Standard2.ButtonText("SK_WeaponMastery_LevelSectionRemoveButton".Translate()))
            {
                OnRemoveLevel();
            }
        }

        listing_Standard2.End();
        listing_Standard.End();
        list.GapLine();
    }

    private static void DrawStatsSection(Listing_Standard list)
    {
        var rect = list.GetRect(150f);
        var listing_Standard = new Listing_Standard();
        listing_Standard.Begin(rect);
        listing_Standard.ColumnWidth = 390f;
        listing_Standard.Label("SK_WeaponMastery_RangedStatsSectionTitle".Translate());
        listing_Standard.Gap();
        Widgets.Dropdown(listing_Standard.GetRect(30f), null, null, (string _) => GenerateRangedStatsOptions(),
            selectedRangedDef.defName);
        listing_Standard.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionLabel".Translate());
        if (!isRangedStatPercentage)
        {
            rangedStatOffset = Widgets.HorizontalSlider(listing_Standard.GetRect(22f), rangedStatOffset, MIN_STAT_BONUS,
                MAX_STAT_BONUS, false, ((float)Math.Round(rangedStatOffset, 2)).ToString(), null, null, 0.1f);
        }
        else
        {
            rangedStatOffset = Widgets.HorizontalSlider(listing_Standard.GetRect(22f), rangedStatOffset, -1f, 1f, false,
                ((float)Math.Round(rangedStatOffset, 2)).ToString(), null, null, 0.01f);
        }

        if (isRangedStatEnabled)
        {
            if (listing_Standard.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionDisableButton".Translate()))
            {
                OnDisableStat(false);
            }
        }
        else if (listing_Standard.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionEnableButton".Translate()))
        {
            OnEnableStat(false);
        }

        listing_Standard.NewColumn();
        listing_Standard.ColumnWidth = 390f;
        listing_Standard.Label("SK_WeaponMastery_MeleeStatsSectionTitle".Translate());
        listing_Standard.Gap();
        Widgets.Dropdown(listing_Standard.GetRect(30f), null, null, (string _) => GenerateMeleeStatsOptions(),
            selectedMeleeDef.defName);
        listing_Standard.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionLabel".Translate());
        if (!isMeleeStatPercentage)
        {
            meleeStatOffset = Widgets.HorizontalSlider(listing_Standard.GetRect(22f), meleeStatOffset, MIN_STAT_BONUS,
                MAX_STAT_BONUS, false, ((float)Math.Round(meleeStatOffset, 2)).ToString(), null, null, 0.1f);
        }
        else
        {
            meleeStatOffset = Widgets.HorizontalSlider(listing_Standard.GetRect(22f), meleeStatOffset, -1f, 1f, false,
                ((float)Math.Round(meleeStatOffset, 2)).ToString(), null, null, 0.01f);
        }

        if (isMeleeStatEnabled)
        {
            if (listing_Standard.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionDisableButton".Translate()))
            {
                OnDisableStat(true);
            }
        }
        else if (listing_Standard.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionEnableButton".Translate()))
        {
            OnEnableStat(true);
        }

        listing_Standard.End();
        list.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionNote".Translate());
        list.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionWarning".Translate());
        list.Gap();
        list.GapLine();
    }

    private static void DrawModOptionsSection(Listing_Standard list)
    {
        var rect = list.GetRect(100f);
        var listing_Standard = new Listing_Standard
        {
            ColumnWidth = 300f
        };
        listing_Standard.Begin(rect);
        listing_Standard.Label("SK_WeaponMastery_ModSettingsSectionTitle".Translate());
        listing_Standard.Label("SK_WeaponMastery_ModSettingsSectionWeaponNameChanceLabel".Translate(), -1f,
            "SK_WeaponMastery_ModSettingsSectionWeaponNameChanceTooltip".Translate());
        listing_Standard.Label(
            ModsConfig.RoyaltyActive
                ? "SK_WeaponMastery_ModSettingsSectionBondedWeaponExperienceMultiplierLabel".Translate()
                : "SK_WeaponMastery_ModSettingsSectionRoyaltyRequiredLabel".Translate());

        if (ModsConfig.IdeologyActive)
        {
            listing_Standard.Label("SK_WeaponMastery_ModSettingsSectionRelicBonusStatsNumberLabel".Translate(), -1f,
                "SK_WeaponMastery_ModSettingsSectionRelicBonusStatsNumberTooltip".Translate());
        }
        else
        {
            listing_Standard.Label("SK_WeaponMastery_ModSettingsSectionIdeologyRequiredLabel".Translate());
        }

        listing_Standard.NewColumn();
        listing_Standard.ColumnWidth = 470f;
        listing_Standard.Label("SK_WeaponMastery_ModSettingsSectionNote".Translate());
        ModSettings.chanceToNameWeapon = Widgets.HorizontalSlider(listing_Standard.GetRect(22f),
            ModSettings.chanceToNameWeapon, 0f, 1f, false, ModSettings.chanceToNameWeapon.ToStringPercent(), null, null,
            0.01f);
        if (ModsConfig.RoyaltyActive)
        {
            ModSettings.bondedWeaponExperienceMultipier = Widgets.HorizontalSlider(listing_Standard.GetRect(22f),
                ModSettings.bondedWeaponExperienceMultipier, MIN_BONDED_WEAPON_MULTIPLIER, MAX_BONDED_WEAPON_MULTIPLIER,
                false, $"{ModSettings.bondedWeaponExperienceMultipier:F1}x", null, null, 0.01f);
        }
        else
        {
            Widgets.Label(listing_Standard.GetRect(22f), "");
        }

        if (ModsConfig.IdeologyActive)
        {
            ModSettings.numberOfRelicBonusStats = (int)Widgets.HorizontalSlider(listing_Standard.GetRect(22f),
                ModSettings.numberOfRelicBonusStats, MIN_RELIC_BONUS_STATS, MAX_RELIC_BONUS_STATS, false,
                ModSettings.numberOfRelicBonusStats.ToString(), null, null, 1f);
        }
        else
        {
            Widgets.Label(listing_Standard.GetRect(22f), "");
        }

        listing_Standard.End();
        var rect2 = list.GetRect(ModSettings.masteryOnOutsidePawns ? 230 : 170);
        var listing_Standard2 = new Listing_Standard();
        listing_Standard2.Begin(rect2);
        listing_Standard2.CheckboxLabeled("SK_WeaponMastery_ModSettingsSectionSpecificMasterySystemLabel".Translate(),
            ref ModSettings.useSpecificMasterySystem,
            "SK_WeaponMastery_ModSettingsSectionSpecificMasterySystemTooltip".Translate());
        listing_Standard2.CheckboxLabeled("SK_WeaponMastery_ModSettingsSectionGeneralMasterySystemLabel".Translate(),
            ref ModSettings.useGeneralMasterySystem,
            "SK_WeaponMastery_ModSettingsSectionGeneralMasterySystemTooltip".Translate());
        listing_Standard2.CheckboxLabeled("SK_WeaponMastery_ModSettingsSectionMasteryOutsiderPawnsLabel".Translate(),
            ref ModSettings.masteryOnOutsidePawns,
            "SK_WeaponMastery_ModSettingsSectionMasteryOutsiderTooltip".Translate());
        listing_Standard2.CheckboxLabeled("SK_WeaponMastery_ModSettingsUseMoodsCheckboxLabel".Translate(),
            ref ModSettings.useMoods, "SK_WeaponMastery_ModSettingsUseMoodsCheckboxTooltip".Translate());
        listing_Standard2.CheckboxLabeled("SK_WeaponMastery_ModSettingsSectionDisplayExperienceLabel".Translate(),
            ref ModSettings.displayExperience,
            "SK_WeaponMastery_ModSettingsSectionDisplayExperienceTooltip".Translate());
        listing_Standard2.CheckboxLabeled(
            "SK_WeaponMastery_ModSettingsSectionKeepOriginalNameAndQualityLabel".Translate(),
            ref ModSettings.KeepOriginalWeaponNameQuality,
            "SK_WeaponMastery_ModSettingsSectionKeepOriginalNameAndQualityTooltip".Translate());
        if (ModSettings.masteryOnOutsidePawns)
        {
            var rect3 = listing_Standard2.GetRect(60f);
            var listing_Standard3 = new Listing_Standard();
            listing_Standard3.Begin(rect3);
            listing_Standard3.ColumnWidth = 300f;
            listing_Standard3.Label("SK_WeaponMastery_ModSettingsSectionMasteryGroupPercentage".Translate());
            listing_Standard3.Label("SK_WeaponMastery_ModSettingsSectionMasteryWeaponNameChancePerPawn".Translate());
            listing_Standard3.NewColumn();
            listing_Standard3.ColumnWidth = 470f;
            ModSettings.masteriesPercentagePerEvent = Widgets.HorizontalSlider(listing_Standard3.GetRect(30f),
                ModSettings.masteriesPercentagePerEvent, 0.01f, 1f, false,
                $"{ModSettings.masteriesPercentagePerEvent:F2}%", null, null, 0.01f);
            ModSettings.eventWeaponNameChance = Widgets.HorizontalSlider(listing_Standard3.GetRect(30f),
                ModSettings.eventWeaponNameChance, 0.01f, 1f, false,
                $"{ModSettings.eventWeaponNameChance:F2}%", null, null, 0.01f);
            listing_Standard3.End();
        }

        if (WeaponMasteryMod.currentVersion != null)
        {
            listing_Standard.Gap();
            GUI.contentColor = Color.gray;
            listing_Standard.Label(
                "SK_WeaponMastery_ModSettingsCurrentModVersion".Translate(WeaponMasteryMod.currentVersion));
            GUI.contentColor = Color.white;
        }

        listing_Standard2.End();
        list.GapLine();
    }

    private static void DrawCustomNamesSection(Listing_Standard list)
    {
        var rect = list.GetRect(ModSettings.useCustomNames ? 130 : 50);
        rect.width -= 30f;
        var listing_Standard = new Listing_Standard();
        listing_Standard.Begin(rect);
        listing_Standard.Label("SK_WeaponMastery_CustomNamesSectionTitle".Translate());
        listing_Standard.CheckboxLabeled("SK_WeaponMastery_CustomNamesSectionCheckbox".Translate(),
            ref ModSettings.useCustomNames, "SK_WeaponMastery_CustomNamesSectionCheckboxTooltip".Translate());
        if (ModSettings.useCustomNames)
        {
            var rect2 = new Rect(rect.x, 50f, 620f, 100f);
            var listing_Standard2 = new Listing_Standard();
            listing_Standard2.Begin(rect2);
            listing_Standard2.ColumnWidth = 300f;
            listing_Standard2.Label("SK_WeaponMastery_CustomNamesAddNameLabel".Translate());
            listing_Standard2.Label("SK_WeaponMastery_CustomNamesRemoveNameLabel".Translate());
            if (listing_Standard2.ButtonText("SK_WeaponMastery_CustomNamesAddButton".Translate()))
            {
                OnAddName();
            }

            listing_Standard2.NewColumn();
            listing_Standard2.ColumnWidth = 300f;
            customNameInputStringBuffer = listing_Standard2.TextEntry(customNameInputStringBuffer);
            if (ModSettings.customWeaponNamesPool.Count > 0)
            {
                Widgets.Dropdown(listing_Standard2.GetRect(22f), null, null, (string _) => GenerateCustomNameOptions(),
                    selectedCustomName);
            }
            else
            {
                listing_Standard2.None();
            }

            if (listing_Standard2.ButtonText("SK_WeaponMastery_CustomNamesRemoveButton".Translate()))
            {
                OnRemoveName();
            }

            listing_Standard2.End();
        }

        listing_Standard.End();
        list.GapLine();
    }

    private static void DrawGeneralMasterySection(Listing_Standard list)
    {
        var rect = list.GetRect(300f);
        var listing_Standard = new Listing_Standard();
        listing_Standard.Begin(rect);
        listing_Standard.ColumnWidth = 250f;
        listing_Standard.Label("SK_WeaponMastery_GeneralMasterySectionAddRemoveClassesTitle".Translate());
        listing_Standard.Gap();
        listing_Standard.Label("SK_WeaponMastery_GeneralMasterySectionAddClassLabel".Translate());
        listing_Standard.Gap();
        listing_Standard.Label("SK_WeaponMastery_GeneralMasterySectionRemoveClassLabel".Translate());
        listing_Standard.Gap();
        var rect2 = listing_Standard.Label("SK_WeaponMastery_GeneralMasterySectionSelectClassLabel".Translate());
        var rect3 = new Rect(rect2.x + 100f, rect2.y, 120f, 30f);
        Widgets.Dropdown(rect3, null, null, (string _) => GenerateClassOptions(), allClasses[selectedClassIndex]);
        listing_Standard.Gap();
        var rect4 = listing_Standard.Label("SK_WeaponMastery_GeneralMasterySectionSelectWeaponLabel".Translate());
        var rect5 = new Rect(rect4.x + 100f, rect4.y, 120f, 30f);
        Widgets.Dropdown(rect5, null, null, (string _) => GenerateWeaponOptions(),
            allWeapons[selectedWeaponIndex].LabelCap);
        listing_Standard.NewColumn();
        listing_Standard.ColumnWidth = 250f;
        listing_Standard.Label("");
        listing_Standard.Gap();
        addClassInputStringBuffer = listing_Standard.TextEntry(addClassInputStringBuffer);
        listing_Standard.Gap();
        removeClassInputStringBuffer = listing_Standard.TextEntry(removeClassInputStringBuffer);
        listing_Standard.ButtonImage(cachedWeaponTexture, 100f, 100f);
        listing_Standard.Gap();
        listing_Standard.Label("SK_WeaponMastery_GeneralMasterySectionAssignedClassLabel".Translate(
            ModSettings.classes.ContainsKey(allWeapons[selectedWeaponIndex])
                ? Utils.Capitalize(ModSettings.classes[allWeapons[selectedWeaponIndex]])
                : "SK_WeaponMastery_GeneralMasterySectionNoClassLabel".Translate().ToString()));
        listing_Standard.NewColumn();
        listing_Standard.ColumnWidth = 250f;
        listing_Standard.Label("");
        listing_Standard.Gap();
        var addClass = listing_Standard.ButtonText("SK_WeaponMastery_GeneralMasterySectionAddClassButton".Translate());
        var removeClass =
            listing_Standard.ButtonText("SK_WeaponMastery_GeneralMasterySectionRemoveClassButton".Translate());
        listing_Standard.Gap();
        listing_Standard.Gap();
        var assignClass =
            listing_Standard.ButtonText("SK_WeaponMastery_GeneralMasterySectionAssignClassButton".Translate());
        var clearClass =
            listing_Standard.ButtonText("SK_WeaponMastery_GeneralMasterySectionClearClassButton".Translate());
        listing_Standard.End();
        list.GapLine();
        if (addClass)
        {
            OnClassAdded();
        }

        if (removeClass)
        {
            OnRemoveClass();
        }

        if (assignClass)
        {
            OnAssignClass();
        }

        if (clearClass)
        {
            OnClearClass();
        }
    }

    private static void OnAddLevel()
    {
        ModSettings.experiencePerLevel.Add(DEFAULT_EXP);
        selectedLevelIndex = ModSettings.experiencePerLevel.Count - 1;
        selectedLevelValue = DEFAULT_EXP;
        selectedLevelStringBuffer = selectedLevelValue.ToString();
        ModSettings.maxLevel++;
    }

    private static void OnRemoveLevel()
    {
        if (selectedLevelIndex == ModSettings.experiencePerLevel.Count - 1)
        {
            selectedLevelIndex--;
            selectedLevelValue = ModSettings.experiencePerLevel[selectedLevelIndex];
            selectedLevelStringBuffer = selectedLevelValue.ToString();
        }

        ModSettings.experiencePerLevel.Pop();
        ModSettings.maxLevel--;
    }

    private static void OnDisableStat(bool isMelee)
    {
        if (!isMelee)
        {
            ModSettings.RemoveStat(selectedRangedMasteryStat, false);
            isRangedStatEnabled = false;
            Messages.Message(
                "SK_WeaponMastery_MessageDisabledRangedStat".Translate(selectedRangedMasteryStat.GetStat().defName),
                MessageTypeDefOf.NeutralEvent);
            selectedRangedMasteryStat = null;
        }
        else
        {
            ModSettings.RemoveStat(selectedMeleeMasteryStat, true);
            isMeleeStatEnabled = false;
            Messages.Message(
                "SK_WeaponMastery_MessageDisabledMeleeStat".Translate(selectedMeleeMasteryStat.GetStat().defName),
                MessageTypeDefOf.NeutralEvent);
            selectedMeleeMasteryStat = null;
        }
    }

    private static void OnEnableStat(bool isMelee)
    {
        if (!isMelee)
        {
            selectedRangedMasteryStat = new MasteryStat(selectedRangedDef, rangedStatOffset);
            ModSettings.AddStat(selectedRangedMasteryStat, false);
            isRangedStatEnabled = true;
            Messages.Message("SK_WeaponMastery_MessageEnabledRangedStat".Translate(selectedRangedDef.defName),
                MessageTypeDefOf.NeutralEvent);
        }
        else
        {
            selectedMeleeMasteryStat = new MasteryStat(selectedMeleeDef, meleeStatOffset);
            ModSettings.AddStat(selectedMeleeMasteryStat, true);
            isMeleeStatEnabled = true;
            Messages.Message("SK_WeaponMastery_MessageEnabledMeleeStat".Translate(selectedMeleeDef.defName),
                MessageTypeDefOf.NeutralEvent);
        }
    }

    private static void OnClassAdded()
    {
        if (string.IsNullOrEmpty(addClassInputStringBuffer) ||
            allClasses.Contains(addClassInputStringBuffer))
        {
            return;
        }

        Messages.Message(
            "SK_WeaponMastery_GeneralMasterySectionAddClassMessage".Translate(addClassInputStringBuffer
                .CapitalizeFirst()), MessageTypeDefOf.NeutralEvent);
        allClasses.Add(addClassInputStringBuffer);
    }

    private static void OnAssignClass()
    {
        ModSettings.classes[allWeapons[selectedWeaponIndex]] = allClasses[selectedClassIndex];
        ModSettings.overrideClasses[allWeapons[selectedWeaponIndex].defName] = allClasses[selectedClassIndex];
        Messages.Message(
            "SK_WeaponMastery_GeneralMasterySectionClassAssignedMessage".Translate(
                allWeapons[selectedWeaponIndex].LabelCap, allClasses[selectedClassIndex]),
            MessageTypeDefOf.NeutralEvent);
    }

    private static void OnClearClass()
    {
        ModSettings.classes.Remove(allWeapons[selectedWeaponIndex]);
        ModSettings.overrideClasses.Remove(allWeapons[selectedWeaponIndex].defName);
        Messages.Message("SK_WeaponMastery_GeneralMasterySectionClassClearedMessage".Translate(),
            MessageTypeDefOf.NeutralEvent);
    }

    private static void OnRemoveClass()
    {
        if (string.IsNullOrEmpty(removeClassInputStringBuffer) ||
            !allClasses.Contains(removeClassInputStringBuffer))
        {
            return;
        }

        var list = new List<ThingDef>();
        var list2 = new List<string>();
        foreach (var @class in ModSettings.classes)
        {
            if (@class.Value == removeClassInputStringBuffer)
            {
                list.Add(@class.Key);
            }
        }

        for (var i = 0; i < list.Count; i++)
        {
            ModSettings.classes.Remove(list[i]);
        }

        foreach (var overrideClass in ModSettings.overrideClasses)
        {
            if (overrideClass.Value == removeClassInputStringBuffer)
            {
                list2.Add(overrideClass.Key);
            }
        }

        for (var j = 0; j < list2.Count; j++)
        {
            ModSettings.overrideClasses.Remove(list2[j]);
        }

        allClasses.Remove(removeClassInputStringBuffer);
        Messages.Message(
            "SK_WeaponMastery_GeneralMasterySectionRemoveClassMessage".Translate(removeClassInputStringBuffer),
            MessageTypeDefOf.NeutralEvent);
    }

    private static void OnAddName()
    {
        if (string.IsNullOrEmpty(customNameInputStringBuffer))
        {
            return;
        }

        var text = customNameInputStringBuffer.Trim();
        if (ModSettings.customWeaponNamesPool.Contains(text))
        {
            return;
        }

        ModSettings.customWeaponNamesPool.Add(text);
        Messages.Message("SK_WeaponMastery_CustomNamesAddNameMessage".Translate(text),
            MessageTypeDefOf.NeutralEvent);
        if (ModSettings.customWeaponNamesPool.Count == 1)
        {
            selectedCustomName = text;
        }
    }

    private static void OnRemoveName()
    {
        if (ModSettings.customWeaponNamesPool.Count == 0)
        {
            return;
        }

        ModSettings.customWeaponNamesPool.Remove(selectedCustomName);
        Messages.Message("SK_WeaponMastery_CustomNamesRemoveNameMessage".Translate(selectedCustomName),
            MessageTypeDefOf.NeutralEvent);
        selectedCustomName = ModSettings.customWeaponNamesPool.Count > 0 ? ModSettings.customWeaponNamesPool[0] : null;
    }

    public static void Destroy()
    {
        isOpen = false;
        thread = null;
        allStatDefs = null;
        selectedRangedDef = null;
        selectedMeleeDef = null;
        selectedMeleeMasteryStat = null;
        selectedRangedMasteryStat = null;
        selectedCustomName = null;
        allWeapons = null;
        cachedWeaponTexture = null;
        allClasses = null;
    }

    private static bool IsStatDefPercentage(StatDef def)
    {
        return def.toStringStyle is ToStringStyle.PercentZero or ToStringStyle.PercentOne or ToStringStyle.PercentTwo;
    }
}
