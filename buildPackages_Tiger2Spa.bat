cd /d %~dp0
call defines.bat

set Localization=zh-Hans
set NugetServer=-NugetSource %NugetSrc%
set NugetApiKey=-NugetApiKey %ApiKey%
set NugetPush=-NugetPush

set DllNames=-DllNames Tiger.Core.Spa
set NugetPushOnly=-NugetPushOnly
set TigerTargetNugetFolder=S:\Coding\github\DXNugetPackageBuilder\output\Tiger9Spa

set Builder=output\DXNugetPackageBuilder.exe

%Builder% %TigerBinPath% %TigerBinPath% %TigerTargetNugetFolder% 9 %Localization% %NugetServer% %NugetApiKey% %NugetPush% %DllNames%

pause