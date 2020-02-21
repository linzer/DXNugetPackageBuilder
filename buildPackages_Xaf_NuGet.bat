@echo off
cd /d %~dp0

call defines_nuget.bat

set Localization=zh-Hans
set NugetServer=-NugetSource %NugetSrc%
set NugetApiKey=-NugetApiKey %ApiKey_Xaf%
set NugetPush=

set Builder=output\DXNugetPackageBuilder.exe
@echo on
%Builder% %XafBinPath% %XafBinPath% %XafTargetNugetFolder% 0 %Localization% %NugetServer% %NugetApiKey% %NugetPush%
pause