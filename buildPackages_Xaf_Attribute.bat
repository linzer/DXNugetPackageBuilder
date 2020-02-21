@echo off
cd /d %~dp0

call defines.bat

set Localization=zh-Hans
set NugetServer=-NugetSource %NugetSrc%
set NugetApiKey=-NugetApiKey %ApiKey%
set NugetPush=-NugetPush
set DllNames=-DllNames DevExpress.ExpressApp;DevExpress.ExpressApp.Xpo;DevExpress.ExpressApp.ConditionalAppearance;DevExpress.Persistent.Base;DevExpress.ExpressApp.Dashboards;DevExpress.ExpressApp.Reports;DevExpress.ExpressApp.ReportsV2;DevExpress.ExpressApp.Validation;DevExpress.ExpressApp.Win;DevExpress.Persistent.BaseImpl;DevExpress.ExpressApp.Design;DevExpress.ExpressApp.Spa

set Builder=output\DXNugetPackageBuilder.exe
@echo on
%Builder% %XafBinPath% %XafBinPath% %XafTargetNugetFolder% 8 %Localization% %NugetServer% %NugetApiKey% %NugetPush% %DllNames%
pause