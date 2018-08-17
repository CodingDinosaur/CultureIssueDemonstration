# Demonstration of Issues with ICU Aliased Cultures in .NET Core on Linux

This repo is a sample for dotnet/coreclr issue here: (TBD)

## Issue Description
Certain valid locales cannot be used for localization in .NET Core on Unix-based environments, because they are not recognized by CultureInfo and its surrounding classes.  Although not the only affected locale, this is most easily reproduced with zh-TW (Chinese, Taiwan).

This affects any locale which is an "aliased" locale in ICU (International Components for Unicode)