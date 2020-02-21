cd /d %~dp0
call defines_nuget.bat

set Localization=zh-Hans
set NugetServer=-NugetSource %NugetSrc%
set NugetApiKey=-NugetApiKey %ApiKey_Tiger%
set NugetPush=-NugetPush

set Builder=output\DXNugetPackageBuilder.exe

%Builder% %TigerBinPath% %TigerBinPath% %TigerTargetNugetFolder% 1 %Localization% %NugetServer% %NugetApiKey% %NugetPush%