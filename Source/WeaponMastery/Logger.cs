using HarmonyLib;
using Verse;

namespace WeaponMastery;

public static class Logger
{
    public static void WriteToHarmonyFile(string message)
    {
        if (WeaponMasteryMod.ShouldPrintLOG)
        {
            FileLog.Log(message);
        }
    }

    public static void WriteToGameConsole(string message)
    {
        if (WeaponMasteryMod.ShouldPrintLOG)
        {
            Log.Message(message);
        }
    }
}