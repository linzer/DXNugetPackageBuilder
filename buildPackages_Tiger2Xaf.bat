cd /d %~dp0
call defines.bat

set Localization=zh-Hans
set NugetServer=-NugetSource %NugetSrc%
set NugetApiKey=-NugetApiKey %ApiKey%
set NugetPush=-NugetPush

set DllNames=-DllNames Tiger.Entity.Attributes;Tiger.Entity.Base;Tiger.Entity.DC;Tiger.Entity.Xpo
set NugetPushOnly=-NugetPushOnly
set TigerTargetNugetFolder=S:\Coding\github\DXNugetPackageBuilder\output\Tiger9Xaf

set Builder=output\DXNugetPackageBuilder.exe

%Builder% %TigerBinPath% %TigerBinPath% %TigerTargetNugetFolder% 9 %Localization% %NugetServer% %NugetApiKey% %NugetPush% %DllNames%

echo 	xcopy exe
rem xcopy "%TigerBinPath%\net452\Tiger.Entity.Attributes.dll" %XafBinPath% /S /Y /H /I
xcopy "%TigerBinPath%\net452\Tiger.Entity.Attributes.dll" "D:\Program Files (x86)\DevExpress 18.2\Components\Sources\packages\Tiger.Entity.Attributes.18.2.5.1\lib\net45\" /S /Y /H /I
xcopy "%TigerBinPath%\net452\Tiger.Entity.Attributes.pdb" "D:\Program Files (x86)\DevExpress 18.2\Components\Sources\packages\Tiger.Entity.Attributes.18.2.5.1\lib\net45\" /S /Y /H /I


xcopy "%TigerBinPath%\net462\Tiger.Entity.Attributes.dll" "D:\nuget\packages\tiger.entity.attributes\18.2.5.1\lib\net45" /S /Y /H /I
xcopy "%TigerBinPath%\net462\Tiger.Entity.Attributes.pdb" "D:\nuget\packages\tiger.entity.attributes\18.2.5.1\lib\net45" /S /Y /H /I
xcopy "%TigerBinPath%\net462\Tiger.Entity.Attributes.xml" "D:\nuget\packages\tiger.entity.attributes\18.2.5.1\lib\net45" /S /Y /H /I


xcopy "%TigerBinPath%\net462\Tiger.Entity.Attributes.dll" "S:\Coding\svn\Feature\EndApi\packages\Tiger.Entity.Attributes.18.2.5.1\lib\net45\" /S /Y /H /I
xcopy "%TigerBinPath%\net462\Tiger.Entity.Attributes.pdb" "S:\Coding\svn\Feature\EndApi\packages\Tiger.Entity.Attributes.18.2.5.1\lib\net45\" /S /Y /H /I
xcopy "%TigerBinPath%\net462\Tiger.Entity.Attributes.xml" "S:\Coding\svn\Feature\EndApi\packages\Tiger.Entity.Attributes.18.2.5.1\lib\net45\" /S /Y /H /I

xcopy "%TigerBinPath%\net462\Tiger.Entity.Attributes.dll" "S:\Coding\svn\HG\EndApi\Yesfree.Shell.HG_18_2\packages\Tiger.Entity.Attributes.18.2.5.1\lib\net45\" /S /Y /H /I
xcopy "%TigerBinPath%\net462\Tiger.Entity.Attributes.pdb" "S:\Coding\svn\HG\EndApi\Yesfree.Shell.HG_18_2\packages\Tiger.Entity.Attributes.18.2.5.1\lib\net45\" /S /Y /H /I
xcopy "%TigerBinPath%\net462\Tiger.Entity.Attributes.xml" "S:\Coding\svn\HG\EndApi\Yesfree.Shell.HG_18_2\packages\Tiger.Entity.Attributes.18.2.5.1\lib\net45\" /S /Y /H /I

rem xcopy "%TigerBinPath%\net462\Tiger.Entity.Base.dll" %XafBinPath% /S /Y /H /I

rem xcopy "%TigerBinPath%\net462\Tiger.Entity.DC.dll" %XafBinPath% /S /Y /H /I

pause