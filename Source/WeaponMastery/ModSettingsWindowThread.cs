using System;
using System.Threading;

namespace WeaponMastery;

internal class ModSettingsWindowThread
{
    private const int THREAD_SLEEP_TIME_MS = 100;

    private float oldMeleeStatOffset;

    private float oldRangedStatOffset;

    private int oldSelectedLevelValue;

    public ModSettingsWindowThread(int oldSelectedLevelValue, float oldRangedStatOffset, float oldMeleeStatOffset)
    {
        this.oldSelectedLevelValue = oldSelectedLevelValue;
        this.oldRangedStatOffset = oldRangedStatOffset;
        this.oldMeleeStatOffset = oldMeleeStatOffset;
    }

    public void Run()
    {
        while (ModSettingsWindow.isOpen)
        {
            if (ModSettingsWindow.selectedLevelValue != oldSelectedLevelValue)
            {
                ModSettings.experiencePerLevel[ModSettingsWindow.selectedLevelIndex] =
                    ModSettingsWindow.selectedLevelValue;
                oldSelectedLevelValue = ModSettingsWindow.selectedLevelValue;
            }

            if (ModSettingsWindow.selectedRangedMasteryStat != null &&
                oldRangedStatOffset != ModSettingsWindow.rangedStatOffset)
            {
                oldRangedStatOffset = ModSettingsWindow.rangedStatOffset;
                ModSettingsWindow.selectedRangedMasteryStat.SetOffset(
                    (float)Math.Round(ModSettingsWindow.rangedStatOffset, 2));
            }

            if (ModSettingsWindow.selectedMeleeMasteryStat != null &&
                oldMeleeStatOffset != ModSettingsWindow.meleeStatOffset)
            {
                oldMeleeStatOffset = ModSettingsWindow.meleeStatOffset;
                ModSettingsWindow.selectedMeleeMasteryStat.SetOffset(
                    (float)Math.Round(ModSettingsWindow.meleeStatOffset, 2));
            }

            if (!ModSettings.useGeneralMasterySystem && !ModSettings.useSpecificMasterySystem)
            {
                ModSettings.useSpecificMasterySystem = true;
            }

            Thread.Sleep(100);
        }
    }
}
