using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CultureIssueDemonstration
{
    internal class Program
    {
        private const string ResourceFileName = "CultureIssueDemonstration.resources.dll";

        internal static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine(@"==CultureIssueDemonstration==");
            Console.WriteLine(@"Test App for aliased cultures in .NET Core on linux");
            Console.WriteLine(@"This affects locales which are aliased in ICU, notably zh-TW and in-ID");

            Console.WriteLine($@"Current platform is: {Environment.OSVersion.Platform.ToString()}");
            Console.WriteLine(@"Setting CurrentCulture to en-US...");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            // 1 - Check the list of cultures for the presence of zh-TW (an ICU aliased locale)       
            CheckAvailableCultures();

            /* 2 - Check for the presence of and ability to obtain strings from resource files for:
             *     2.1 - The default culture (enUS).  Works on both Windows and Linux.
             *     2.2 - fr-FR, a standard non-aliased culture.  Works on both Windows and Linux.
             *     2.3 - zh-TW, an ICU aliased culture.  Works on Windows only.  On Linux, the file has failed to publish and does not exist.
             *     2.4 - zh-CN, an ICU aliased culture in which we have forcibly copied over the resource file using a custom build step.
             *           On Linux, the localized string lookup still fails (and falls back to default) despite the presence of the file. */
            ResourceFilePublishTests();
            ResourceFilePublishTests("fr-FR");
            ResourceFilePublishTests("zh-TW");
            ResourceFilePublishTests("zh-CN");

            Console.WriteLine();
            CheckParentCulture("en-US");
            CheckParentCulture("fr-FR");
            CheckParentCulture("zh-TW");
            CheckParentCulture("zh-CN");

            Console.WriteLine(@"Complete! (Press enter to exit)");
            Console.Read();
        }

        // A check to see if en-US, fr-FR, and zh-TW cultures can be obtained from GetCultures
        // The expected result is that all three exist, but on Linux environments zh-TW will be missing
        private static void CheckAvailableCultures()
        {
            Console.WriteLine();
            Console.WriteLine(@"Checking all cultures...");
            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            // ReSharper disable InconsistentNaming
            var enUSPresent = allCultures.Any(c => c.Name == "en-US");
            var frFRPresent = allCultures.Any(c => c.Name == "fr-FR");
            var zhTWPresent = allCultures.Any(c => c.Name == "zh-TW");
            var zhCNPresent = allCultures.Any(c => c.Name == "zh-CN");
            // ReSharper restore InconsistentNaming

            Console.WriteLine($@"Current Culture: {CultureInfo.CurrentCulture.Name}");
            Console.WriteLine($@"Cultures found: {allCultures.Length}");
            Console.WriteLine($@"en-US: {enUSPresent} | fr-FR: {frFRPresent} | zh-TW: {zhTWPresent} | zh-CN: {zhCNPresent}");
        }

        private static void CheckParentCulture(string locale)
        {
            var ci = CultureInfo.GetCultureInfo(locale);
            Console.WriteLine($@"Culture {ci.Name} | Display Name: {ci.DisplayName} | Parent Culture Name: {ci.Parent.Name}");
        }

        private static void GetStringByCultureTest(string locale = null)
        {
            Console.WriteLine();
            if (locale != null)
            {
                Console.WriteLine($@"Setting current culture to {locale}...");
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
            }
            else
            {
                Console.WriteLine(@"Testing resource lookup starting with default culture...");
            }

            var culture = CultureInfo.CurrentCulture;
            MyStrings.Culture = culture;
            Console.WriteLine($@"Current Culture: {CultureInfo.CurrentCulture}");
            Console.WriteLine($@"Test String Value (Current Culture): {MyStrings.TestResourceString}");
            Console.WriteLine($@"Test String Value (Explicit Culture): {MyStrings.ResourceManager.GetString(nameof(MyStrings.TestResourceString), culture)}");
        }

        private static void ResourceFilePublishTests(string locale = null)
        {
            if (locale != null)
            {
                CheckForResourceFile(locale);
            }

            GetStringByCultureTest(locale);
        }

        private static void CheckForResourceFile(string localeName)
        {
            Console.WriteLine();
            var fullPath = Path.Combine(Environment.CurrentDirectory, localeName, ResourceFileName);
            Console.WriteLine($@"Checking for presence of localized resources for {localeName}...");
            Console.WriteLine($@"Expected path is: {fullPath}");

            var fileExists = File.Exists(fullPath);
            Console.WriteLine($@"File {(fileExists ? "exists" : "DOES NOT exist")}!");
        }
    }
}
