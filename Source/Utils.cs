using System.Globalization;

namespace SK_WeaponMastery
{
    public static class Utils
    {
        public static string Capitalize(string input)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(input);
        }
    }
}
