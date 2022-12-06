using System;
using System.IO;

namespace WeaponMastery;

public static class Files
{
    public static string[] GetLinesFromTextFile(string path, bool relative)
    {
        try
        {
            switch (relative)
            {
                case false when File.Exists(path):
                    return File.ReadAllLines(path);
                case true when File.Exists(Path.Combine(WeaponMasteryMod.GetRootDirectory(), path)):
                    return File.ReadAllLines(Path.Combine(WeaponMasteryMod.GetRootDirectory(), path));
                default:
                    return null;
            }
        }
        catch (Exception)
        {
            return null;
        }
    }
}
