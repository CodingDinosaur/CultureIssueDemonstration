using System;
using System.Globalization;
using System.Linq;

namespace CultureIssueDemonstration
{
    internal class Program
    {
        internal static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine(@"==CultureIssueDemonstration==");
            Console.WriteLine(@"Test App for aliased cultures in .NET Core on linux");
            Console.WriteLine(@"This affects locales which are aliased in ICU, notably zh-TW and in-ID");

            Console.WriteLine($@"Current platform is: {Environment.OSVersion.Platform.ToString()}");
            Console.WriteLine(@"Setting CurrentCulture to en-US...");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            CheckAvailableCultures();

            GetStringByCultureTest();

            Console.WriteLine(@"Complete! (Press any key to exit)");
            Console.ReadKey();
        }

        private static void CheckAvailableCultures()
        {
            Console.WriteLine();
            Console.WriteLine(@"Checking all cultures...");
            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            // ReSharper disable InconsistentNaming
            var enUSPresent = allCultures.Any(c => c.Name == "en-US");
            var frFRPresent = allCultures.Any(c => c.Name == "fr-FR");
            var zhTwPresent = allCultures.Any(c => c.Name == "zh-TW");
            // ReSharper restore InconsistentNaming

            Console.WriteLine($@"Current Culture: {CultureInfo.CurrentCulture.Name}");
            Console.WriteLine($@"Cultures found: {allCultures.Length}");
            Console.WriteLine($@"en-US: {enUSPresent} | fr-FR: {frFRPresent} | zh-TW: {zhTwPresent}");
            Console.WriteLine();
        }

        private static void ShowResourceTestStringByCurrentCulture()
        {
            MyStrings.Culture = CultureInfo.CurrentCulture;
            Console.WriteLine($@"Current Culture: {CultureInfo.CurrentCulture}");
            Console.WriteLine($@"Test String Value: {MyStrings.TestResourceString}");
        }

        private static void GetStringByCultureTest()
        {
            Console.WriteLine();
            Console.WriteLine($@"Testing resource lookup via default culture...");
            ShowResourceTestStringByCurrentCulture();

            Console.WriteLine();
            Console.WriteLine($@"Attempting to set current culture to fr-FR...");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            ShowResourceTestStringByCurrentCulture();

            Console.WriteLine();
            Console.WriteLine($@"Attempting to set current culture to zh-TW...");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zh-TW");
            ShowResourceTestStringByCurrentCulture();
        }
    }
}
