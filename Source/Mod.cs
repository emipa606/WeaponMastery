using HarmonyLib;
using UnityEngine;
using Verse;

namespace SK_WeaponMastery
{
    // Main mod file
    public class WeaponMasteryMod : Mod
    {
        public static bool SHOULD_PRINT_LOG = true;
        // Mod name in about.xml
        public static string modName;
        private static string rootDirectory;
        public WeaponMasteryMod(ModContentPack content) : base(content)
        {
            rootDirectory = this.Content.RootDir;
            modName = this.Content.Name;
            Harmony instance = new Harmony("rimworld.sk.weaponmastery");

            HarmonyPatcher.PatchVanillaMethods(instance);

            // Fires when all Defs are loaded
            LongEventHandler.ExecuteWhenFinished(Init);
        }

        public void Init()
        {
            GetSettings<ModSettings>();
            if (!ModSettings.initialLoad) 
                ModSettings.SetSensibleDefaults(); 
            else 
                ModSettings.ResolveStats();
            Core.AddMasteryCompToWeaponDefs();
            Core.InjectStatPartIntoStatDefs();
            ModSettings.LoadWeaponNames();
            ModSettings.InitMessageKeys();
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
}
