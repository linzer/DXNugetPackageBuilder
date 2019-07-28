@echo off
cd /d %~dp0

call defines.bat

set Localization=zh-Hans
set NugetServer=-NugetSource %NugetSrc%
set NugetApiKey=-NugetApiKey %ApiKey%
set NugetPush=-NugetPush

set Builder=output\DXNugetPackageBuilder.exe
@echo on
%Builder% %XafBinPath% %XafBinPath% %XafTargetNugetFolder% 0 %Localization% %NugetServer% %NugetApiKey% %NugetPush%
pause