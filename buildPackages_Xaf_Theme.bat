@echo off
cd /d %~dp0

call defines.bat

set Localization=zh-Hans
set NugetServer=-NugetSource %NugetSrc%
set NugetApiKey=-NugetApiKey %ApiKey%
set NugetPush=-NugetPush

set Builder=output\DXNugetPackageBuilder.exe

set XafBinPath="D:\temp\themeA"
set XafTargetNugetFolder=D:\temp\themeB

%Builder% %XafBinPath% %XafBinPath% %XafTargetNugetFolder% 0 %Localization% %NugetServer% %NugetApiKey% %NugetPush%