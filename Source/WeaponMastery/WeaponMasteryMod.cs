using HarmonyLib;
using Mlie;
using RimWorld;
using UnityEngine;
using Verse;

namespace WeaponMastery;

public class WeaponMasteryMod : Mod
{
    public static bool SHOULD_PRINT_LOG = false;

    public static string modName;

    private static string rootDirectory;
    public static string currentVersion;

    public WeaponMasteryMod(ModContentPack content)
        : base(content)
    {
        rootDirectory = Content.RootDir;
        modName = Content.Name;
        currentVersion = VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
        var instance = new Harmony("rimworld.sk.weaponmastery");
        HarmonyPatcher.SetInstance(instance);
        LongEventHandler.ExecuteWhenFinished(Init);
    }

    public void Init()
    {
        GetSettings<ModSettings>();
        if (!ModSettings.initialLoad)
        {
            ModSettings.SetSensibleDefaults();
        }
        else
        {
            ModSettings.ResolveStats();
        }

        Core.MasteredWeaponUnequipped =
            DefDatabase<ThoughtDef>.AllDefsListForReading.Find(def => def.defName == "SK_WM_MasteredWeaponUnequipped");
        Core.MasteredWeaponEquipped =
            DefDatabase<HediffDef>.AllDefsListForReading.Find(def =>
                def.defName == "SK_WM_MasteredWeaponEquippedBonusMood");
        Core.AddMasteryWeaponCompToWeaponDefsAndSetClasses();
        Core.OverrideClasses();
        if (ModSettings.useGeneralMasterySystem)
        {
            Core.AddMasteryPawnCompToHumanoidDefs();
        }

        Core.InjectStatPartIntoStatDefs();
        ModSettings.LoadWeaponNames();
        ModSettings.InitMessageKeys();
        HarmonyPatcher.PatchVanillaMethods();
        DualWieldCompat.Init();
        if (ModSettings.useMoods)
        {
            SimpleSidearmsCompat.Init();
        }
    }

    public override void DoSettingsWindowContents(Rect rect)
    {
        ModSettingsWindow.Draw(rect);
        base.DoSettingsWindowContents(rect);
    }

    public override string SettingsCategory()
    {
        return "SK_WeaponMastery_SettingsListItemName".Translate();
    }

    public static string GetRootDirectory()
    {
        return rootDirectory;
    }
}
