cd /d %~dp0
call defines.bat

set Localization=zh-Hans
set NugetServer=-NugetSource %NugetSrc%
set NugetApiKey=-NugetApiKey %ApiKey%
set NugetPush=-NugetPush

set Builder=output\DXNugetPackageBuilder.exe

%Builder% %LibsBinPath% %LibsBinPath% %LibsTargetNugetFolder% 2 %Localization% %NugetServer% %NugetApiKey% %NugetPush%