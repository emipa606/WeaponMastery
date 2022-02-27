using HarmonyLib;
using Verse;

namespace SK_WeaponMastery
{
    public static class Logger
    {
        // Harmony Log File
        public static void WriteToHarmonyFile(string message)
        {
            if (WeaponMasteryMod.SHOULD_PRINT_LOG)
                FileLog.Log(message);
        }

        public static void WriteToGameConsole(string message)
        {
            if (WeaponMasteryMod.SHOULD_PRINT_LOG)
                Log.Message(message);
        }
    }
}
