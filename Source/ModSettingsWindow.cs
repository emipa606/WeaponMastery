using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SK_WeaponMastery
{
    // Responsible for drawing modsettings ui window and updating modsettings  
    public static class ModSettingsWindow
    {
        public static int selectedLevelIndex;
        public static int selectedLevelValue;
        private static string selectedLevelStringBuffer;
        private static ModSettingsWindowThread thread;
        private static List<StatDef> allStatDefs;
        private static StatDef selectedRangedDef;
        private static StatDef selectedMeleeDef;
        public static MasteryStat selectedRangedMasteryStat;
        public static MasteryStat selectedMeleeMasteryStat;
        public static float rangedStatOffset;
        public static float meleeStatOffset;
        public static bool isRangedStatEnabled;
        public static bool isMeleeStatEnabled;
        public static bool isRangedStatPercentage;
        public static bool isMeleeStatPercentage;
        public static bool isOpen = false;
        public static bool initSemaphore = false;
        static Vector2 scrollPosition = Vector2.zero;
        public const int MAX_LEVELS = 15;
        private readonly static int DEFAULT_EXP = 15000;
        private readonly static float DEFAULT_STAT_OFFSET = 1f;
        private readonly static float MIN_STAT_BONUS = -5f;
        private readonly static float MAX_STAT_BONUS = 5;

        public static void Init()
        {
            if (initSemaphore) return;
            initSemaphore = true;
            isOpen = true;

            selectedLevelIndex = 0;
            selectedLevelValue = ModSettings.experiencePerLevel[0];
            allStatDefs = DefDatabase<StatDef>.AllDefs.ToList();
            selectedRangedDef = allStatDefs[0];
            selectedMeleeDef = allStatDefs[0];
            isRangedStatPercentage = IsStatDefPercentage(selectedRangedDef);
            isMeleeStatPercentage = IsStatDefPercentage(selectedMeleeDef);

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

            thread = new ModSettingsWindowThread(selectedLevelValue, rangedStatOffset, meleeStatOffset);
            new System.Threading.Thread(new System.Threading.ThreadStart(thread.Run)).Start();
            initSemaphore = false;
        }
        public static void Draw(Rect parent)
        {
            if (!isOpen)
                Init();

            if (initSemaphore) return;

            parent.yMin += 15f;
            parent.yMax -= 15f;
            Listing_Standard list = new Listing_Standard(GameFont.Medium);

            Rect outerRect = new Rect(parent.x, parent.y + 20, parent.width, parent.height - 20);
            Rect scrollRect = new Rect(0f, 150f, parent.width - 16f, parent.height * 3f + 50);
            Widgets.BeginScrollView(outerRect, ref scrollPosition, scrollRect, true);
            list.Begin(scrollRect);

            DrawModOptionsSection(list);
            DrawExperiencePerLevelSection(list);
            DrawStatsSection(list);

            list.End();
            Widgets.EndScrollView();
        }

        private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateLevelOptions()
        {
            for (int i = 0; i < ModSettings.maxLevel; i++)
            {
                int localCopyOfI = i;
                yield return new Widgets.DropdownMenuElement<string>
                {
                    option = new FloatMenuOption("SK_WeaponMastery_LevelSectionDropdownOptionLabel".Translate(localCopyOfI + 1), delegate ()
                    {
                        selectedLevelIndex = localCopyOfI;
                        selectedLevelValue = ModSettings.experiencePerLevel[localCopyOfI];
                        selectedLevelStringBuffer = selectedLevelValue.ToString();
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = "SK_WeaponMastery_LevelSectionLevelExpLabel".Translate(localCopyOfI + 1)
                };
            }
        }

        private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateRangedStatsOptions()
        {
            for (int i = 0; i < allStatDefs.Count; i++)
            {
                int localCopyOfI = i;

                yield return new Widgets.DropdownMenuElement<string>
                {
                    option = new FloatMenuOption(allStatDefs[localCopyOfI].defName, delegate ()
                    {
                        selectedRangedDef = allStatDefs[localCopyOfI];
                        isRangedStatPercentage = IsStatDefPercentage(selectedRangedDef);
                        MasteryStat item = ModSettings.FindStatWithStatDef(allStatDefs[localCopyOfI], false);
                        if (item != null)
                        {
                            selectedRangedMasteryStat = item;
                            rangedStatOffset = item.GetOffset();
                            isRangedStatEnabled = true;
                        }
                        else
                            isRangedStatEnabled = false;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = allStatDefs[localCopyOfI].defName
                };
            }
        }

        private static IEnumerable<Widgets.DropdownMenuElement<string>> GenerateMeleeStatsOptions()
        {
            for (int i = 0; i < allStatDefs.Count; i++)
            {
                int localCopyOfI = i;

                yield return new Widgets.DropdownMenuElement<string>
                {
                    option = new FloatMenuOption(allStatDefs[localCopyOfI].defName, delegate ()
                    {
                        selectedMeleeDef = allStatDefs[localCopyOfI];
                        isMeleeStatPercentage = IsStatDefPercentage(selectedMeleeDef);
                        MasteryStat item = ModSettings.FindStatWithStatDef(allStatDefs[localCopyOfI], true);
                        if (item != null)
                        {
                            selectedMeleeMasteryStat = item;
                            meleeStatOffset = item.GetOffset();
                            isMeleeStatEnabled = true;
                        }
                        else
                            isMeleeStatEnabled = false;
                    }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
                    payload = allStatDefs[localCopyOfI].defName
                };
            }
        }

        private static void DrawExperiencePerLevelSection(Listing_Standard list)
        {
            list.Label("SK_WeaponMastery_LevelSectionTitle".Translate());
            list.Gap();
            list.Label("SK_WeaponMastery_LevelSectionDetail".Translate());

            // This is an incorrect usage of the dropdown's API but I can't
            // wrap my head around how it works exactly. This is good enough
            // for my needs and it works.
            Widgets.Dropdown(list.GetRect(30), null, null, (string s) => GenerateLevelOptions(), "SK_WeaponMastery_LevelSectionDropdownOptionLabel".Translate(selectedLevelIndex + 1));
            list.Label("SK_WeaponMastery_LevelSectionLevelExpLabel".Translate(selectedLevelIndex + 1));
            list.TextFieldNumeric(ref selectedLevelValue, ref selectedLevelStringBuffer);

            if (ModSettings.maxLevel != MAX_LEVELS)
            {
                bool addButtonClicked = list.ButtonText("SK_WeaponMastery_LevelSectionAddButton".Translate());
                if (addButtonClicked)
                    OnAddLevel();
            }
            if (ModSettings.maxLevel != 1)
            {
                bool removeButtonClicked = list.ButtonText("SK_WeaponMastery_LevelSectionRemoveButton".Translate());
                if (removeButtonClicked)
                    OnRemoveLevel();
            }
            list.Gap();
            list.GapLine();
        }

        private static void DrawStatsSection(Listing_Standard list)
        {
            // Ranged Stats Section
            list.Label("SK_WeaponMastery_RangedStatsSectionTitle".Translate());
            list.Gap();
            // Bad dropdown API usage
            Widgets.Dropdown(list.GetRect(30), null, null, (string s) => GenerateRangedStatsOptions(), selectedRangedDef.defName);
            list.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionLabel".Translate());
            // Percentage stats use values from 0 -> 1 which maps to 0% -> 100%
            if (!isRangedStatPercentage)
                rangedStatOffset = Widgets.HorizontalSlider(list.GetRect(22f), rangedStatOffset, MIN_STAT_BONUS, MAX_STAT_BONUS, false, rangedStatOffset.ToString(), null, null, 0.01f);
            else
                rangedStatOffset = Widgets.HorizontalSlider(list.GetRect(22f), rangedStatOffset, 0f, 1f, false, rangedStatOffset.ToString(), null, null, 0.01f);
            if (isRangedStatEnabled)
            {
                bool disableStatButtonClicked = list.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionDisableButton".Translate());
                if (disableStatButtonClicked)
                    OnDisableStat(false);
            }
            else
            {
                bool enableStatButtonClicked = list.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionEnableButton".Translate());
                if (enableStatButtonClicked)
                    OnEnableStat(false);
            }
            list.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionNote".Translate());
            list.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionWarning".Translate());
            list.Gap();
            list.GapLine();

            // Melee Stats Section
            list.Label("SK_WeaponMastery_MeleeStatsSectionTitle".Translate());
            list.Gap();
            // Bad dropdown API usage
            Widgets.Dropdown(list.GetRect(30), null, null, (string s) => GenerateMeleeStatsOptions(), selectedMeleeDef.defName);
            list.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionLabel".Translate());
            // Percentage stats use values from 0 -> 1 which maps to 0% -> 100%
            if (!isMeleeStatPercentage)
                meleeStatOffset = Widgets.HorizontalSlider(list.GetRect(22f), meleeStatOffset, MIN_STAT_BONUS, MAX_STAT_BONUS, false, meleeStatOffset.ToString(), null, null, 0.01f);
            else
                meleeStatOffset = Widgets.HorizontalSlider(list.GetRect(22f), meleeStatOffset, 0f, 1f, false, meleeStatOffset.ToString(), null, null, 0.01f);
            if (isMeleeStatEnabled)
            {
                bool disableStatButtonClicked = list.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionDisableButton".Translate());
                if (disableStatButtonClicked)
                    OnDisableStat(true);
            }
            else
            {
                bool enableStatButtonClicked = list.ButtonText("SK_WeaponMastery_RangedAndMeleeStatsSectionEnableButton".Translate());
                if (enableStatButtonClicked)
                    OnEnableStat(true);
            }
            list.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionNote".Translate());
            list.Label("SK_WeaponMastery_RangedAndMeleeStatsSectionWarning".Translate());
            list.Gap();
            list.GapLine();
        }

        private static void DrawModOptionsSection(Listing_Standard list)
        {
            list.Label("SK_WeaponMastery_ModSettingsSectionTitle".Translate());
            list.Label("SK_WeaponMastery_ModSettingsSectionWeaponNameChanceLabel".Translate(), -1, "SK_WeaponMastery_ModSettingsSectionWeaponNameChanceTooltip".Translate());
            ModSettings.chanceToNameWeapon = Widgets.HorizontalSlider(list.GetRect(22f), ModSettings.chanceToNameWeapon, 0.01f, 1f, false, ModSettings.chanceToNameWeapon.ToStringPercent(), null, null, 0.01f);
            list.GapLine();
        }

        // Directly updates ModSettings
        private static void OnAddLevel()
        {
            ModSettings.experiencePerLevel.Add(DEFAULT_EXP);
            selectedLevelIndex = ModSettings.experiencePerLevel.Count - 1;
            selectedLevelValue = DEFAULT_EXP;
            selectedLevelStringBuffer = selectedLevelValue.ToString();
            ModSettings.maxLevel += 1;
        }

        // Directly updates ModSettings
        private static void OnRemoveLevel()
        {
            if (selectedLevelIndex == ModSettings.experiencePerLevel.Count - 1)
            {
                selectedLevelIndex -= 1;
                selectedLevelValue = ModSettings.experiencePerLevel[selectedLevelIndex];
                selectedLevelStringBuffer = selectedLevelValue.ToString();
            }
            ModSettings.experiencePerLevel.Pop();
            ModSettings.maxLevel -= 1;
        }

        // Directly updates ModSettings
        private static void OnDisableStat(bool isMelee)
        {
            if (!isMelee)
            {
                ModSettings.RemoveStat(selectedRangedMasteryStat, isMelee);
                isRangedStatEnabled = false;
                Messages.Message("SK_WeaponMastery_MessageDisabledRangedStat".Translate(selectedRangedMasteryStat.GetStat().defName), MessageTypeDefOf.NeutralEvent);
                selectedRangedMasteryStat = null;
            }
            else
            {
                ModSettings.RemoveStat(selectedMeleeMasteryStat, isMelee);
                isMeleeStatEnabled = false;
                Messages.Message("SK_WeaponMastery_MessageDisabledMeleeStat".Translate(selectedMeleeMasteryStat.GetStat().defName), MessageTypeDefOf.NeutralEvent);
                selectedMeleeMasteryStat = null;
            }
        }

        // Directly updates ModSettings
        private static void OnEnableStat(bool isMelee)
        {
            if (!isMelee)
            {
                selectedRangedMasteryStat = new MasteryStat(selectedRangedDef, rangedStatOffset);
                ModSettings.AddStat(selectedRangedMasteryStat, isMelee);
                isRangedStatEnabled = true;
                Messages.Message("SK_WeaponMastery_MessageEnabledRangedStat".Translate(selectedRangedDef.defName), MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                selectedMeleeMasteryStat = new MasteryStat(selectedMeleeDef, meleeStatOffset);
                ModSettings.AddStat(selectedMeleeMasteryStat, isMelee);
                isMeleeStatEnabled = true;
                Messages.Message("SK_WeaponMastery_MessageEnabledMeleeStat".Translate(selectedMeleeDef.defName), MessageTypeDefOf.NeutralEvent);
            }
        }

        // Signal gc to clean variables by setting them to null (Possibly?)
        // Since these are statics, they'll always reserve memory unless
        // I set them to null hopefully
        public static void Destroy()
        {
            isOpen = false;
            thread = null;
            allStatDefs = null;
            selectedRangedDef = null;
            selectedMeleeDef = null;
            selectedMeleeMasteryStat = null;
            selectedRangedMasteryStat = null;
        }

        // Checks if StatDef is represented by % or numbers
        private static bool IsStatDefPercentage(StatDef def)
        {
            return def.toStringStyle == ToStringStyle.PercentZero || 
                def.toStringStyle == ToStringStyle.PercentOne || 
                def.toStringStyle == ToStringStyle.PercentTwo;
        }
    }
}
