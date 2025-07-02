using System;
using System.Threading;

namespace WeaponMastery;

internal class ModSettingsWindowThread(int oldSelectedLevelValue, float oldRangedStatOffset, float oldMeleeStatOffset)
{
    private const int ThreadSleepTimeMS = 100;
    private float oldMeleeStatOffset = oldMeleeStatOffset;
    private float oldRangedStatOffset = oldRangedStatOffset;
    private int oldSelectedLevelValue = oldSelectedLevelValue;

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

            Thread.Sleep(ThreadSleepTimeMS);
        }
    }
}