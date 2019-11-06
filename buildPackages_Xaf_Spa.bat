@echo off
cd /d %~dp0

call defines.bat

set Localization=zh-Hans
set NugetServer=-NugetSource %NugetSrc%
set NugetApiKey=-NugetApiKey %ApiKey%
set NugetPush=-NugetPush
set DllNames=-DllNames DevExpress.ExpressApp.Spa

set Builder=output\DXNugetPackageBuilder.exe
@echo on
%Builder% %XafBinPath% %XafBinPath% %XafTargetNugetFolder% 8 %Localization% %NugetServer% %NugetApiKey% %NugetPush% %DllNames%
pause