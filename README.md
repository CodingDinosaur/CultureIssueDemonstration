# Demonstration of Issues with ICU Aliased Cultures in .NET Core on Linux

This repo is a sample for dotnet/coreclr issue here: (TBD)

1. [Issue Description](#issue-description)
1. [Issue Cause Summarized](#issue-cause-summarized)
1. [Issue Symptoms](#issue-symptoms)
    - [Resource files not published during *dotnet publish*](#resource-files-not-published-during-dotnet-publish)
    - [Resource files not utilized even when present](#resource-files-not-utilized-even-when-present)
    - [Culture data incorrect](#culture-data-incorrect)
    - [Cultures missing from list of available cultures](#cultures-missing-from-list-of-available-cultures)
1. [Apparent Root Cause](#apparent-root-cause)
    - [Starting Point - Resource File Publishing](#starting-point---resource-file-publishing)
    - [CultureInfo & The Culture List](#cultureinfo--the-culture-list)
    - [ICU's Data Source](#icus-data-source)
    - [ICU Locale Aliases](#icu-locale-aliases)
      - [ICU Locale Alias List](#icu-locale-alias-list)
1. [Partial Failure of GetCultureInfo](#partial-failure-of-getcultureinfo)
1. [Running the Test Application](#running-the-test-application)
    - [What to look for](#what-to-look-for)

# Issue Description
Certain valid locales cannot be used for localization in .NET Core on Unix-based environments, because they are not recognized by CultureInfo and its surrounding classes.  Although not the only affected locale, this is most easily reproduced with zh-TW (Chinese, Taiwan).

This affects any locale which is an "aliased" locale in ICU [ICU (International Components for Unicode)](http://site.icu-project.org/home).

# Issue Cause Summarized
To summarize what appears to be the cause of the issue:
- Microsoft's mscorlib in dotnet/coreclr uses the 3rd party library [ICU (International Components for Unicode)](http://site.icu-project.org/home) to support non-Windows, platform agnostic localization features such as knowing if a given locale is valid and details about the locale (collation, numeric formatting, etc).
  -  ICU is only used in non-Windows runtimes of .NET Core
- To save on data space while providing a robust parenting algorithm, ICU defines certain locales as "aliases" of others.
- ICU aliases are intentionally not returned by ICU when requesting a list of locales.
- Some Culture concerns in mscorlib currently depend on getting an up-front list of all available cultures.
- **`CultureData.EnumCultures`, which in turn via native interop calls the ICU C API `uloc_getAvailable`, therefore fails to obtain any cultures which are defined as aliases in ICU, and these cultures become invalid or are populated with incorrect data in various contexts.**

Further details can be found in the root cause analysis section below.

# Issue Symptoms
There are a number of symptoms that led to the discovery, including but not limited to the following - all of which are demonstrated in the test application further below:

## Resource files not published during *dotnet publish*
The issue was first noticed when zh-TW resource files were missing from our published applications.

This involves a .NET Core 2.1 project which uses localized resource (.resx) files to localize strings.  As per the usual resx pattern, a default document exists (e.g., *MyStrings.resx*), and locale-specific files are named with the locale name before the extension, and one of these is zg-TW (e.g., *MyStrings.zh-TW.resx*).  During the build & publish process, these files become compiled into *MyApp.resources.dll*, and the localized versions are copied into sub-folders based on the names of the locale.

When building on Windows, this functions correctly.  However, when running *dotnet publish* on a Linux environment, the *zh-TW* folder will be missing.

## Resource files not utilized even when present
Our first attempt at a quick fix involved a workaround to our *.csproj* file to force the zh-TW resources file to be copied into the appropriate folder during a publish.  This ultimately didn't solve anything, however, because while this worked when tested in Windows, when the app was running under Linux, and a string was requested with Culture *zh-TW*, the english strings were returned.

## Culture data incorrect
When trying to get a CultureInfo object for *zh-TW*, an object with invalid data is returned.  Most notably, the display name and parent culture are both incorrect.  It appears that much of the CultureInfo data is in a "default" state for an unknown / custom locale, whereas certain other properties such as the ASCIICodePage give correct information.

## Cultures missing from list of available cultures
The affected cultures are completely missing from the list of available cultures, obtained when calling:
```csharp
var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
```

# Apparent Root Cause
After a great deal of investigation, I have narrowed down what I believe to be the root cause.

## Starting Point - Resource File Publishing
My starting point for uncovering a problem here began with the failure to publish zh-TW resource files.  A look at a verbose build log during a *dotnet publish* showed the following:

```
Added Item(s): 
  ResxWithCulture=
    Resources/MyNetCoreProject.MyResources.de.resx
        Culture=de
        OriginalItemSpec=Resources/MyNetCoreProject.MyResources.de.resx
        TargetPath=Resources/MyNetCoreProject.MyResources.de.resx
        WithCulture=true
    Resources/MyNetCoreProject.MyResources.ja-JP.resx
        Culture=ja-JP
        OriginalItemSpec=Resources/MyNetCoreProject.MyResources.ja-JP.resx
        TargetPath=Resources/MyNetCoreProject.MyResources.ja-JP.resx
        WithCulture=true
Removed Item(s): 
  _MixedResourceWithNoCulture=
    Resources/MyNetCoreProject.MyResources.zh-TW.resx
        OriginalItemSpec=Resources/MyNetCoreProject.MyResources.zh-TW.resx
        TargetPath=Resources/MyNetCoreProject.MyResources.zh-TW.resx
        WithCulture=false
```

As you can see, the zh-TW resource file is removed because it is placed into the "WithNoCulture" bucket.  Back-tracking how this functions, we start from the task named in the log *SplitResourceByCulture*:

[Microsoft/msbuild/src/Tasks/Microsoft.Common.CurrentVersion.targets - SplitResourceByCulture](https://github.com/Microsoft/msbuild/blob/master/src/Tasks/Microsoft.Common.CurrentVersion.targets#L2891) ->
[Microsoft.Build.Tasks.AssignCulture.Execute](https://github.com/Microsoft/msbuild/blob/master/src/Tasks/AssignCulture.cs#L115)

Inside *AssignCulture.Execute()*, we can see that *Culture.GetItemCultureInfo* is used to get culture info:

```csharp
Culture.ItemCultureInfo info = Culture.GetItemCultureInfo
```

[Microsoft.Build.Tasks.Culture.GetItemCultureInfo](https://github.com/Microsoft/msbuild/blob/master/src/Tasks/Culture.cs#L29)

```csharp
validCulture = CultureInfoCache.IsValidCultureString(cultureName);
```

[Microsoft.Build.Tasks.CultureInfoCache.IsValidCultureString](https://github.com/Microsoft/msbuild/blob/master/src/Tasks/CultureInfoCache.cs#L45-L48)

The CultureInfoCache is populated as such:
```csharp
foreach (CultureInfo cultureName in AssemblyUtilities.GetAllCultures())
{
    ValidCultureNames.Add(cultureName.Name);
}
```
[Microsoft.Build.Shared.AssemblyUtilities.GetAllCultures](https://github.com/Microsoft/msbuild/blob/master/src/Shared/AssemblyUtilities.cs#L105-L119)

At this point, we finally get out of MSBuild code and into coreclr:
```csharp
return CultureInfo.GetCultures(CultureTypes.AllCultures);
```

## CultureInfo & The Culture List

All roads lead to the CultureData class in [dotnet/coreclr](https://github.com/dotnet/coreclr).

When calling CultureInfo.GetCultures:

[CultureInfo.GetCultures](https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/src/System/Globalization/CultureInfo.cs#L536-L545) ->
[CultureData.GetCultures](https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/shared/System/Globalization/CultureData.cs#L407-L440) ->
[CultureData(Unix).EnumCultures](https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/shared/System/Globalization/CultureData.Unix.cs#L359-L408) ->
[System.Globalization.Native/locale.cpp:GlobalizationNative_GetLocales](https://github.com/dotnet/coreclr/blob/master/src/corefx/System.Globalization.Native/locale.cpp#L158-L199)

Ultimately ending here in *System.Globalization.Native/locale.cpp*:
```cpp
    int32_t localeCount = uloc_countAvailable();
    
    if (localeCount <=  0)
        return -1; // failed
    
    for (int32_t i = 0; i < localeCount; i++)
    {
        const char *pLocaleName = uloc_getAvailable(i);
        if (pLocaleName[0] == 0) // unexpected empty name
            return -2;
```

This is when the stack enters the C API for the [ICU - the International Components for Unicode](http://site.icu-project.org/home) library.

## ICU's Data Source

The *uloc_countAvailable* and *uloc_getAvailable* calls are part of the ICU C API.  (Mostly functionally equivalent calls exist in the C++ API in the Locale class, however in my testing the exhibit the same behavior as the C API calls.)

We can directly test that ICU isn't returning zh-TW in both the C API method used above and the C++ API, as I have done in this repo:
https://ghosthub.corp.blizzard.net/gsamuelian/CultureIcuTest

To better understand why ICU isn't returning zh_TW and some other locales, we need to better understand how ICU's data works.

ICU's data comes from the [CLDR - Unicode Common Locale Data Repository](http://cldr.unicode.org/).  This is part of what makes ICU attractive -- it is intentionally completely platform agnostic and does not depend on the hosting environment for locale information in any way.  [From the ICU FAQ](http://userguide.icu-project.org/icufaq#TOC-What-is-the-relationship-between-ICU-locale-data-and-system-locale-data-):

>**What is the relationship between ICU locale data and system locale data?**<br/>There is no relationship. ICU is not dependent on the operating system for the locale data.<br/>This also means that uloc_setDefault() does not affect the operating system. The function uloc_setDefault() only sets ICU's default locale. Normally the default locale for ICU is whatever the operating system says is the default locale.

For that reason, ICU includes all of its own data from CLDR.  However, database size has been an ongoing concern, and some locales which shared the same collation data but didn't have a parent / child relationship meant that said collation data would be duplicated.  To de-duplicate this data, ICU added the concept of locale "aliases".  [From ICU's documentation on ICU resource bundles](http://userguide.icu-project.org/locale/localizing#TOC-.txt-resource-bundles):

>A value can also be an "alias", which is simply a reference to another bundle's item. This is to save space by storing large data pieces only once when they cannot be inherited along the locale ID hierarchy (e.g., collation data in ICU shared among zh_HK and zh_TW).

## ICU Locale Aliases
When a locale name is defined as an alias in ICU, then from the standpoint of ICU it isn't a first-class locale -- merely a pointer to a "real" locale when requested.  As a result, **ICU does not return aliases when getting a list of locales** -- whether with `uloc_getAvailable` or `Locale::getAvailableLocales` (and `uloc_countAvailable` does not include them in its count).

That ICU does not return the aliases in this manner **appears to be intentional**, both based on the numerous references to [a lack of alias mapping in the uloc documentation](http://www.icu-project.org/apiref/icu4c/uloc_8h.html), and the following bug:  

https://unicode-org.atlassian.net/browse/ICU-4309
> uloc_getAvailable returns sr_YU, even though it is an %%ALIAS locale. None of the other %%ALIAS locales are returned.

> TracBot made changes - 01/Jul/18 1:59 PM<br/>Resolution		Fixed [ 10004 ]<br/>Status	Done [ 10002 ]	Done [ 10002 ]

**That this bug was fixed is a further and very strong indication that ICU not returning locale aliases is intentional.**

### ICU Locale Alias List
These are all the locales that have aliases for the purposes of locale identification.  Other data types in ICU (such as collation and rule-based number formatting) have additional aliases.

|Locale|Aliases|
---|---
ar_SA|ars
az_Latn_AZ|az_AZ
bs_Latn_BA|bs_BA
en_VU|en_NH
en_ZW|en_RH
fil|tl
fil_PH|tl_PH
he|iw
he_IL|iw_IL
id|in
id_ID|in_ID
nb|no
nb_NO|no_NO
nn_NO|no_NO_NY
pa_Arab_PK|pa_PK
pa_Guru_IN|pa_IN
ro_MD|mo
shi_Tfng_MA|shi_MA
sr_Cyrl_BA|sr_BA
sr_Cyrl_RS|sr_CS, sr_Cyrl_CS, sr_Cyrl_YU, sr_RS, sr_YU
sr_Cyrl_XK|sr_XK
sr_Latn|sh
sr_Latn_BA|sh_BA
sr_Latn_ME|sr_ME
sr_Latn_RS|sh_CS, sh_YU, sr_Latn_CS, sr_Latn_YU
uz_Arab_AF|uz_AF
uz_Latn_UZ|uz_UZ
vai_Vaii_LR|vai_LR
yue_Hans_CN|yue_CN
yue_Hant_HK|yue_HK
zh_Hans_CN|zh_CN
zh_Hans_SG|zh_SG
zh_Hant_HK|zh_HK
zh_Hant_MO|zh_MO
zh_Hant_TW|zh_TW

# Partial Failure of GetCultureInfo
When one of the affected Cultures is obtained using `CultureInfo.GetCultureInfo`, the resulting `CultureInfo` object contains a mixture of expected and unexpected data.  For example, the culture name and display name are incorrect, but the ANSICodePage is correct.

I'm not certain why this is the case, but I believe it is because `CultureInfo` objects contain a complex weave of data, not all of which is fetched up front, and some of which may only 


When calling GetCultureInfo, the code path ultimately leads to native calls to get the specific locale here:
[System.Globalization.Native/locale.cpp:GlobalizationNative_GetLocaleName](https://github.com/dotnet/coreclr/blob/master/src/corefx/System.Globalization.Native/locale.cpp#L201-L219) ->
[System.Globalization.Native/locale.cpp:GlobalizationNative_GetLocale](https://github.com/dotnet/coreclr/blob/master/src/corefx/System.Globalization.Native/locale.cpp#L31-L83)

Unlike the previous example of getting all locales, ICU appears to return correct data for zh-TW, as can be see in the ICU test app.  However, it is likely that some of the numerous properties initializers, and constructors for CultureInfo and its child objects utilize or otherwise depend on the data previously obtained via `CultureData.EnumCultures`.

(Among other places, the `CultureInfoConverter` in `System.ComponentModel` depends on the full culture list.)

# Running the Test Application
To run the test, you will need:
- .NET Core SDK 2.1+
- Docker

There are two test scripts, with both .sh and .bat versions of both.
- **To test on your current OS** run *test-CurrentPlatform.bat*
- **To test in Linux via Docker** run *test-dockerLinux.bat*
  - The docker image is based on the .NET SDK docker image.

My recommendation is that you run both tests from Windows.  This will give you the opportunity to see the differences in the results under either platform.

## What to look for

When running the test, take note of the following for zh-TW, which function correctly under Windows but not under Linux:
- Because it is an unknown culture when running under Linux, the zh-TW culture data incorrect, showing an incorrect display name and no parent.
- The zh-TW resource file is missing, because it wasn't published during the *dotnet publish*
- When checking all available cultures with *CultureInfo.GetCultures*, zh-TW is missing.
- Attempting to get a localized string from a resource file in zh-TW fails and the default string is obtained instead.  This is partly because the resource file is missing, but even if copied manually, the file is ignored since zh-TW is not a "valid" culture while running dotnet under Linux.
