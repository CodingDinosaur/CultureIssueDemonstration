# Demonstration of Issues with ICU Aliased Cultures in .NET Core on Linux

This repo is a sample for dotnet/coreclr issue here: (TBD)

## Issue Description
Certain valid locales cannot be used for localization in .NET Core on Unix-based environments, because they are not recognized by CultureInfo and its surrounding classes.  Although not the only affected locale, this is most easily reproduced with zh-TW (Chinese, Taiwan).

This affects any locale which is an "aliased" locale in ICU (International Components for Unicode)

## Running the test

To run the test, you will need:
- .NET Core SDK 2.1+
- Docker

There are two test scripts, with both .sh and .bat versions of both.
- **To test on your current OS** run *test-CurrentPlatform.bat*
- **To test in Linux via Docker** run *test-dockerLinux.bat*
  - The docker image is based on the .NET SDK docker image.

My recommendation is that you run both tests from Windows.  This will give you the opportunity to see the differences in the results under either platform.

(More info coming soon...)