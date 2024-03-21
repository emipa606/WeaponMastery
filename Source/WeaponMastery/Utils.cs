using System.Globalization;

namespace WeaponMastery;

public static class Utils
{
    public static string Capitalize(string input)
    {
        var textInfo = new CultureInfo("en-US", false).TextInfo;
        return textInfo.ToTitleCase(input);
    }
}