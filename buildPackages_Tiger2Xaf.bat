cd /d %~dp0
call defines.bat

set Localization=zh-Hans
set NugetServer=-NugetSource %NugetSrc%
set NugetApiKey=-NugetApiKey %ApiKey%
set NugetPush=-NugetPush

set DllNames=-DllNames Tiger.Entity.Attributes;Tiger.Entity.Base;Tiger.Entity.DC;Tiger.Entity.Xpo
set NugetPushOnly=-NugetPushOnly
set TigerTargetNugetFolder=S:\Coding\vsts\Repos\Tiger18_2\Output\MySql

set Builder=output\DXNugetPackageBuilder.exe

%Builder% %TigerBinPath% %TigerBinPath% %TigerTargetNugetFolder% 9 %Localization% %NugetServer% %NugetApiKey% %NugetPush% %DllNames%

echo 	xcopy exe
xcopy "%TigerBinPath%\net452\Tiger.Entity.Attributes.dll" %XafBinPath% /S /Y /H /I
xcopy "%TigerBinPath%\net452\Tiger.Entity.Attributes.dll" "S:\Coding\vsts\Repos\Xaf_18_2\packages\Tiger.Entity.Attributes.18.2.5\lib\net452\" /S /Y /H /I

xcopy "%TigerBinPath%\net462\Tiger.Entity.Base.dll" %XafBinPath% /S /Y /H /I

xcopy "%TigerBinPath%\net462\Tiger.Entity.DC.dll" %XafBinPath% /S /Y /H /I

pause