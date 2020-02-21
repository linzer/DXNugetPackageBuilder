call defines_nuget.bat

set Localization=zh-Hans
set NugetServer=-NugetSource %NugetSrc%
set NugetApiKey=-NugetApiKey %ApiKey_Xaf%
set NugetPush=

set Builder=output\DXNugetPackageBuilder.exe

%Builder% %XafBinPath% %XafBinPath% %XafTargetNugetFolder% 10 %Localization% %NugetServer% %NugetApiKey% %NugetPush% 