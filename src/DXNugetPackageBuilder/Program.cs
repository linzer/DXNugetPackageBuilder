using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using NuGet;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace DXNugetPackageBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            var arguments = ProgramArguments.Create(args);

            if (arguments == null)
            {
                Console.ReadLine();
                return;
            }

            var waringns = new List<Tuple<string, Exception>>();
            var success = new List<string>();

            if (!arguments.NugetPushOnly)
            {

                BuildPackages(arguments, dependency =>
                {
                    if (arguments.Verbose)
                        Console.WriteLine("\t" + dependency);
                },
                    ex =>
                    {
                        var oldColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.ToString());
                        Console.ForegroundColor = oldColor;
                    },
                    waringns.Add,
                    ex =>
                    {
                        throw ex;
                    },
                    success.Add
                    );


                if (waringns.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine("{0} Warnigns occured", waringns.Count);

                    foreach (var warning in waringns)
                    {
                        Console.WriteLine(new string('-', Console.BufferWidth));
                        Console.WriteLine(warning.Item1);
                        Console.WriteLine(new string('-', Console.BufferWidth));

                        Console.WriteLine(warning.Item2);
                    }
                }
            }

            if (arguments.NugetPush)
            {
                Console.WriteLine("Created all packages.");

                if (string.IsNullOrEmpty(arguments.NugetSource))
                {
                    Console.WriteLine("NugetSource is empty, cannot push packages");
                    Console.WriteLine("Please press enter to exit");
                    Console.ReadLine();
                }
                else
                {
                    PushPackages(arguments);
                }
            }
            else
            {
                Console.WriteLine("Created all packages, please press enter to exit");
                Console.ReadLine();
            }
        }

        private static void BuildPackages(ProgramArguments arguments, Action<string> logAction, Action<Exception> logExceptionAction, Action<Tuple<string, Exception>> logLoadAssemblyAction, Action<Exception> unexpectedExceptionAction, Action<string> successAction)
        {
            if (!Directory.Exists(arguments.SourceDirectory))
            {
                logExceptionAction(new DirectoryNotFoundException($"{arguments.SourceDirectory} does not exists"));
                return;
            }

            if (Directory.Exists(arguments.OutputDirectory) && arguments.Kind != 10)
            {
                Directory.Delete(arguments.OutputDirectory, true);
            }

            Directory.CreateDirectory(arguments.OutputDirectory);

            if (!Directory.Exists(arguments.PdbDirectory))
            {
                Directory.CreateDirectory(arguments.PdbDirectory);
            }
            if (arguments.Kind == 0)
                BuildPackages_Xaf(arguments, logAction, logExceptionAction, logLoadAssemblyAction, unexpectedExceptionAction, successAction);
            else if (arguments.Kind == 1)
                BuildPackages_Tiger(arguments, logAction, logExceptionAction, logLoadAssemblyAction, unexpectedExceptionAction, successAction);
            else if (arguments.Kind == 2)
                BuildPackages_Libs(arguments, logAction, logExceptionAction, logLoadAssemblyAction, unexpectedExceptionAction, successAction);
            else if (arguments.Kind == 8)
                BuildPackages_Xaf_DllNames(arguments, logAction, logExceptionAction, logLoadAssemblyAction, unexpectedExceptionAction, successAction);
            else if (arguments.Kind == 9)
                BuildPackages_Tiger_DllNames(arguments, logAction, logExceptionAction, logLoadAssemblyAction, unexpectedExceptionAction, successAction);
            else if (arguments.Kind == 10)
                DeletePackages(arguments);
        }

        private static void BuildPackages_Xaf(ProgramArguments arguments, Action<string> logAction, Action<Exception> logExceptionAction, Action<Tuple<string, Exception>> logLoadAssemblyAction, Action<Exception> unexpectedExceptionAction, Action<string> successAction)
        {
            foreach (var file in Directory.EnumerateFiles(arguments.SourceDirectory, "*.dll")
                .Concat(Directory.EnumerateFiles(arguments.SourceDirectory, "*.exe"))
                .Where(f => Path.GetFileNameWithoutExtension(f).StartsWith("DevExpress")))
            {
                try
                {
                    var packageName = Path.GetFileNameWithoutExtension(file);

                    var package = new PackageBuilder();

                    package.Description = "DevExpress " + packageName;
                    package.Authors.Add("DevExpress and Yesfree");
                    package.IconUrl = new Uri("http://nuget.yesfree.cn/ico/xaf.ico");
                    package.Copyright = "2008-" + DateTime.Today.Year;
                    package.ProjectUrl = new Uri("https://www.Yesfree.cn/xaf");
                    package.Language = "zh-Hans";
                    package.Files.Add(new PhysicalPackageFile
                    {
                        SourcePath = file,
                        TargetPath = "lib/net40/" + Path.GetFileName(file),
                    });

                    try
                    {

                        var assembly = Assembly.LoadFile(file);

                        var pdbFile = Path.ChangeExtension(Path.GetFileName(file), "pdb");

                        pdbFile = Path.Combine(arguments.PdbDirectory, pdbFile);

                        if (File.Exists(pdbFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = pdbFile,
                                TargetPath = "lib/net40/" + Path.GetFileName(pdbFile),
                            });
                        }

                        var xmlFile = Path.ChangeExtension(file, "xml");

                        if (File.Exists(xmlFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = xmlFile,
                                TargetPath = "lib/net40/" + Path.GetFileName(xmlFile),
                            });
                        }

                        var configFile = file + ".config";

                        if (File.Exists(configFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = configFile,
                                TargetPath = "lib/net40/" + Path.GetFileName(configFile),
                            });
                        }


                        var assemblyVersion = assembly.GetName().Version;

                        var dxVersion = ".v" + assemblyVersion.Major + "." + assemblyVersion.Minor;

                        if (arguments.UseAssemblyFileVersion)
                        {
                            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                            var version = fvi.FileVersion;
                            assemblyVersion = new Version(version);
                        }

                        if (packageName.Contains(dxVersion))
                            packageName = packageName.Replace(dxVersion, string.Empty);
                        string packageId = "";
                      
#if nuget
                        packageId = "YshXaf." + packageName;
#else
                        packageId = packageName += "_yesfree";
#endif
                        var targetPackagePath = Path.Combine(arguments.OutputDirectory, packageId + "." + assemblyVersion.ToString(4) + ".nupkg");

                        if (File.Exists(targetPackagePath))
                            File.Delete(targetPackagePath);

                        package.Id = packageId;
                        package.Version = new NuGetVersion(assemblyVersion);

                        var dependencies = new List<PackageDependency>();

                        foreach (var refAssembly in assembly.GetReferencedAssemblies().Where(r => r.Name.StartsWith("DevExpress")))
                        {
                            logAction(refAssembly.Name);

                            var refPackageId = refAssembly.Name;

                            if (refPackageId.Contains(dxVersion))
                                refPackageId = refPackageId.Replace(dxVersion, string.Empty);

#if nuget
                            refPackageId = "YshXaf." + refPackageId;
#else
                            refPackageId += "_yesfree";
#endif
                            var refAssemblyVersion = refAssembly.Version;

                            var minVersion = new NuGetVersion(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build, refAssemblyVersion.Revision);
                            var maxVersion = new NuGetVersion(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build, refAssemblyVersion.Revision + 1);

                            var versionSpec = new VersionRange(minVersion, true, maxVersion, false);

                            var dependency = new PackageDependency(refPackageId, versionSpec);

                            if (!arguments.Strict)
                            {
                                var skippedDependencies = new Dictionary<string, string[]>();

                                skippedDependencies["DevExpress.Persistent.Base"] = new[]
                                {
                                    "DevExpress.Utils",
                                    "DevExpress.XtraReports",
                                    "DevExpress.XtraReports.Extensions",
                                    "DevExpress.Printing.Core",
                                };

                                skippedDependencies["DevExpress.Persistent.BaseImpl"] = new[]
                                {
                                    "DevExpress.Utils",
                                    "DevExpress.ExpressApp.ReportsV2",
                                    "DevExpress.ExpressApp.Reports",
                                    "DevExpress.XtraReports",
                                    "DevExpress.ExpressApp.ConditionalAppearance",
                                    "DevExpress.XtraScheduler.Core",
                                };

                                skippedDependencies["DevExpress.Persistent.BaseImpl.EF"] = new[]
                                {
                                    "DevExpress.Utils",
                                    "DevExpress.ExpressApp.Kpi",
                                    "DevExpress.ExpressApp.ReportsV2",
                                    "DevExpress.ExpressApp.Security",
                                    "DevExpress.ExpressApp.ConditionalAppearance",
                                    "DevExpress.ExpressApp.StateMachine",
                                    "DevExpress.ExpressApp.Chart",
                                    "DevExpress.XtraReports",
                                    "DevExpress.XtraScheduler.Core",
                                    "DevExpress.ExpressApp.Reports"
                                };

                                if (skippedDependencies.Keys.Any(id => package.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    var skippedDependency = skippedDependencies[package.Id];

                                    if (skippedDependency.Any(id => dependency.Id.Equals(id)))
                                    {
                                        logAction($"Skipping Dependency: {dependency.Id} for Package {package.Id} to avoid UI in Persistence");
                                        continue;
                                    }
                                }
                            }

                            dependencies.Add(dependency);

                        }

                        package.DependencyGroups.Add(new PackageDependencyGroup(NuGetFramework.AnyFramework, dependencies));



                        CreateLocalization(file, package, arguments);

                        using (var fs = new FileStream(targetPackagePath, FileMode.CreateNew, FileAccess.ReadWrite))
                        {
                            package.Save(fs);

                            successAction(package.Id);
                        }
                        
                        Console.WriteLine(packageName);
                    }
                    catch (Exception ex)
                    {
                        logExceptionAction(ex);
                        logLoadAssemblyAction(Tuple.Create(package.Id, ex));
                    }
                }
                catch (Exception ex)
                {
                    logExceptionAction(ex);
                    unexpectedExceptionAction(ex);
                }
            }
        }

        private static void BuildPackages_Xaf_DllNames(ProgramArguments arguments, Action<string> logAction, Action<Exception> logExceptionAction, Action<Tuple<string, Exception>> logLoadAssemblyAction, Action<Exception> unexpectedExceptionAction, Action<string> successAction)
        {
            var dlls = arguments.DllNames.Split(';', ',');
            foreach (var dll in dlls)
            {
                foreach (var file in Directory.EnumerateFiles(arguments.SourceDirectory, dll + "*.dll", SearchOption.AllDirectories))
                {
                    try
                    {
                        var packageName = Path.GetFileNameWithoutExtension(file);

                        var package = new PackageBuilder();

                        package.Description = "DevExpress " + packageName;
                        package.Authors.Add("DevExpress and Yesfree");
                        package.IconUrl = new Uri("http://nuget.yesfree.cn/ico/xaf.ico");
                        package.Copyright = "2008-" + DateTime.Today.Year;
                        package.ProjectUrl = new Uri("https://www.Yesfree.cn/xaf");
                        package.Language = "zh-Hans";
                        package.Files.Add(new PhysicalPackageFile
                        {
                            SourcePath = file,
                            TargetPath = "lib/net40/" + Path.GetFileName(file),
                        });

                        try
                        {

                            var assembly = Assembly.LoadFile(file);

                            var pdbFile = Path.ChangeExtension(Path.GetFileName(file), "pdb");

                            pdbFile = Path.Combine(arguments.PdbDirectory, pdbFile);

                            if (File.Exists(pdbFile))
                            {
                                package.Files.Add(new PhysicalPackageFile
                                {
                                    SourcePath = pdbFile,
                                    TargetPath = "lib/net40/" + Path.GetFileName(pdbFile),
                                });
                            }

                            var xmlFile = Path.ChangeExtension(file, "xml");

                            if (File.Exists(xmlFile))
                            {
                                package.Files.Add(new PhysicalPackageFile
                                {
                                    SourcePath = xmlFile,
                                    TargetPath = "lib/net40/" + Path.GetFileName(xmlFile),
                                });
                            }

                            var configFile = file + ".config";

                            if (File.Exists(configFile))
                            {
                                package.Files.Add(new PhysicalPackageFile
                                {
                                    SourcePath = configFile,
                                    TargetPath = "lib/net40/" + Path.GetFileName(configFile),
                                });
                            }


                            var assemblyVersion = assembly.GetName().Version;

                            var dxVersion = ".v" + assemblyVersion.Major + "." + assemblyVersion.Minor;

                            if (arguments.UseAssemblyFileVersion)
                            {
                                var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                                var version = fvi.FileVersion;
                                assemblyVersion = new Version(version);
                            }

                            if (packageName.Contains(dxVersion))
                                packageName = packageName.Replace(dxVersion, string.Empty);
                            string packageId = "";
       
#if nuget
                            packageId = "YshXaf."+ packageName;
#else
                            packageId = packageName += "_yesfree";
#endif
                            var targetPackagePath = Path.Combine(arguments.OutputDirectory, packageId + "." + assemblyVersion.ToString(4) + ".nupkg");

                            if (File.Exists(targetPackagePath))
                                File.Delete(targetPackagePath);

                            package.Id = packageId;
                            package.Version = new NuGetVersion(assemblyVersion);

                            var dependencies = new List<PackageDependency>();

                            foreach (var refAssembly in assembly.GetReferencedAssemblies().Where(r => r.Name.StartsWith("DevExpress")))
                            {
                                logAction(refAssembly.Name);

                                var refPackageId = refAssembly.Name;

                                if (refPackageId.Contains(dxVersion))
                                    refPackageId = refPackageId.Replace(dxVersion, string.Empty);
                   
#if nuget
                                refPackageId = "YshXaf." + refPackageId;
#else
                                refPackageId += "_yesfree";
#endif
                                var refAssemblyVersion = refAssembly.Version;

                                var minVersion = new NuGetVersion(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build, refAssemblyVersion.Revision);
                                var maxVersion = new NuGetVersion(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build, refAssemblyVersion.Revision + 1);

                                var versionSpec = new VersionRange(minVersion, true, maxVersion, false);

                                var dependency = new PackageDependency(refPackageId, versionSpec);

                                if (!arguments.Strict)
                                {
                                    var skippedDependencies = new Dictionary<string, string[]>();

                                    skippedDependencies["DevExpress.Persistent.Base"] = new[]
                                    {
                                    "DevExpress.Utils",
                                    "DevExpress.XtraReports",
                                    "DevExpress.XtraReports.Extensions",
                                    "DevExpress.Printing.Core",
                                };

                                    skippedDependencies["DevExpress.Persistent.BaseImpl"] = new[]
                                    {
                                    "DevExpress.Utils",
                                    "DevExpress.ExpressApp.ReportsV2",
                                    "DevExpress.ExpressApp.Reports",
                                    "DevExpress.XtraReports",
                                    "DevExpress.ExpressApp.ConditionalAppearance",
                                    "DevExpress.XtraScheduler.Core",
                                };

                                    skippedDependencies["DevExpress.Persistent.BaseImpl.EF"] = new[]
                                    {
                                    "DevExpress.Utils",
                                    "DevExpress.ExpressApp.Kpi",
                                    "DevExpress.ExpressApp.ReportsV2",
                                    "DevExpress.ExpressApp.Security",
                                    "DevExpress.ExpressApp.ConditionalAppearance",
                                    "DevExpress.ExpressApp.StateMachine",
                                    "DevExpress.ExpressApp.Chart",
                                    "DevExpress.XtraReports",
                                    "DevExpress.XtraScheduler.Core",
                                    "DevExpress.ExpressApp.Reports"
                                };

                                    if (skippedDependencies.Keys.Any(id => package.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase)))
                                    {
                                        var skippedDependency = skippedDependencies[package.Id];

                                        if (skippedDependency.Any(id => dependency.Id.Equals(id)))
                                        {
                                            logAction($"Skipping Dependency: {dependency.Id} for Package {package.Id} to avoid UI in Persistence");
                                            continue;
                                        }
                                    }
                                }

                                dependencies.Add(dependency);

                            }

                            package.DependencyGroups.Add(new PackageDependencyGroup(NuGetFramework.AnyFramework, dependencies));



                            CreateLocalization(file, package, arguments);

                            using (var fs = new FileStream(targetPackagePath, FileMode.CreateNew, FileAccess.ReadWrite))
                            {
                                package.Save(fs);

                                successAction(package.Id);
                            }

                            Console.WriteLine(packageName);
                        }
                        catch (Exception ex)
                        {
                            logExceptionAction(ex);
                            logLoadAssemblyAction(Tuple.Create(package.Id, ex));
                        }
                    }
                    catch (Exception ex)
                    {
                        logExceptionAction(ex);
                        unexpectedExceptionAction(ex);
                    }
                }
            }
        }


        private static void BuildPackages_Tiger(ProgramArguments arguments, Action<string> logAction, Action<Exception> logExceptionAction, Action<Tuple<string, Exception>> logLoadAssemblyAction, Action<Exception> unexpectedExceptionAction, Action<string> successAction)
        {
            foreach (var file in Directory.EnumerateFiles(arguments.SourceDirectory, "*.dll").
                Concat(Directory.EnumerateFiles(arguments.SourceDirectory, "*.exe")).
                Where(f => Path.GetFileNameWithoutExtension(f).StartsWith("Tiger")))
            {
                try
                {
                    var packageName = Path.GetFileNameWithoutExtension(file);

                    var package = new PackageBuilder();

                    package.Description = "Yesfree " + packageName;
                    package.Authors.Add("Yesfree Inc.");
                    package.IconUrl = new Uri("http://nuget.yesfree.cn/ico/tiger.ico");
                    package.Copyright = "2008-" + DateTime.Today.Year;
                    package.ProjectUrl = new Uri("https://www.Yesfree.cn/");
                    package.Language = "zh-Hans";
                    package.Files.Add(new PhysicalPackageFile
                    {
                        SourcePath = file,
                        TargetPath = "lib/net45/" + Path.GetFileName(file),
                    });

                    try
                    {

                        var assembly = Assembly.LoadFile(file);

                        var pdbFile = Path.ChangeExtension(Path.GetFileName(file), "pdb");

                        pdbFile = Path.Combine(arguments.PdbDirectory, pdbFile);

                        if (File.Exists(pdbFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = pdbFile,
                                TargetPath = "lib/net45/" + Path.GetFileName(pdbFile),
                            });
                        }

                        var xmlFile = Path.ChangeExtension(file, "xml");

                        if (File.Exists(xmlFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = xmlFile,
                                TargetPath = "lib/net45/" + Path.GetFileName(xmlFile),
                            });
                        }

                        var configFile = file + ".config";

                        if (File.Exists(configFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = configFile,
                                TargetPath = "lib/net45/" + Path.GetFileName(configFile),
                            });
                        }


                        var assemblyVersion = assembly.GetName().Version;

                        var dxVersion = ".v" + assemblyVersion.Major + "." + assemblyVersion.Minor;

                        if (arguments.UseAssemblyFileVersion)
                        {
                            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                            var version = fvi.FileVersion;
                            assemblyVersion = new Version(version);
                        }

                        if (packageName.Contains(dxVersion))
                            packageName = packageName.Replace(dxVersion, string.Empty);
                        //packageName += "_yesfree";

                        var targetPackagePath = Path.Combine(arguments.OutputDirectory, packageName + "." + assemblyVersion.ToString(4) + ".nupkg");

                        if (File.Exists(targetPackagePath))
                            File.Delete(targetPackagePath);

                        package.Id = packageName;
                        package.Version = new NuGetVersion(assemblyVersion);

                        var dependencies = new List<PackageDependency>();

                        foreach (var refAssembly in assembly.GetReferencedAssemblies().Where(r => r.Name.StartsWith("Tiger")))
                        {
                            logAction(refAssembly.Name);

                            var refPackageId = refAssembly.Name;

                            if (refPackageId.Contains(dxVersion))
                                refPackageId = refPackageId.Replace(dxVersion, string.Empty);


                            var refAssemblyVersion = refAssembly.Version;

                            var minVersion = new NuGetVersion(new Version(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build, refAssemblyVersion.Revision));
                            var maxVersion = new NuGetVersion(new Version(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build, refAssemblyVersion.Revision + 1));

                            var versionSpec = new VersionRange(minVersion, true, maxVersion, false);

                            var dependency = new PackageDependency(refPackageId, versionSpec);
                            dependencies.Add(dependency);
                        }

                        package.DependencyGroups.Add(new PackageDependencyGroup(NuGetFramework.AnyFramework, dependencies));


                        CreateLocalization(file, package, arguments, "net45");


                        using (var fs = new FileStream(targetPackagePath, FileMode.CreateNew, FileAccess.ReadWrite))
                        {
                            package.Save(fs);

                            successAction(package.Id);
                        }

                        Console.WriteLine(packageName);
                    }
                    catch (Exception ex)
                    {
                        logExceptionAction(ex);
                        logLoadAssemblyAction(Tuple.Create(package.Id, ex));
                    }
                }
                catch (Exception ex)
                {
                    logExceptionAction(ex);
                    unexpectedExceptionAction(ex);
                }
            }
        }

        private static void BuildPackages_Tiger_DllNames(ProgramArguments arguments, Action<string> logAction, Action<Exception> logExceptionAction, Action<Tuple<string, Exception>> logLoadAssemblyAction, Action<Exception> unexpectedExceptionAction, Action<string> successAction)
        {
            var dlls = arguments.DllNames.Split(';', ',');
            foreach (var dll in dlls)
            {
                PackageBuilder package = null;
                string packageName = string.Empty;
                string targetPackagePath = string.Empty;
                foreach (var file in Directory.EnumerateFiles(arguments.SourceDirectory, dll+".dll", SearchOption.AllDirectories))
                {
                    try
                    {
                        var assembly = Assembly.LoadFile(file);
                        var assemblyVersion = assembly.GetName().Version;
                        var dxVersion = ".v" + assemblyVersion.Major + "." + assemblyVersion.Minor;

                        if (arguments.UseAssemblyFileVersion)
                        {
                            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                            var version = fvi.FileVersion;
                            assemblyVersion = new Version(version);
                        }


                        if (package == null)
                        {
                            packageName = Path.GetFileNameWithoutExtension(file);

                            package = new PackageBuilder();

                            package.Description = "Yesfree " + packageName;
                            package.Authors.Add("Yesfree Inc.");
                            package.IconUrl = new Uri("http://nuget.yesfree.cn/ico/tiger.ico");
                            package.Copyright = "2008-" + DateTime.Today.Year;
                            package.ProjectUrl = new Uri("https://www.Yesfree.cn/");
                            package.Language = "zh-Hans";

                            if (packageName.Contains(dxVersion))
                                packageName = packageName.Replace(dxVersion, string.Empty);
                            //packageName += "_yesfree";

                            targetPackagePath = Path.Combine(arguments.OutputDirectory, packageName + "." + assemblyVersion.ToString(4) + ".nupkg");

                            package.Id = packageName;
                            package.Version = new NuGetVersion(assemblyVersion);
                        }
                        string netPath = "net45";
                        if (file.IndexOf("net462") != -1)
                            netPath = "net462";
                        else if (file.IndexOf("net461") != -1)
                            netPath = "net461";
                        else if (file.IndexOf("net452") != -1)
                            netPath = "net45";
                        else if (file.IndexOf("net4") != -1)
                            netPath = "net4";
                        string targetPath = "lib/" + netPath + "/";
                        package.Files.Add(new PhysicalPackageFile
                        {
                            SourcePath = file,
                            TargetPath = targetPath + Path.GetFileName(file),
                        });

                        try
                        {

                            

                            var pdbFile = Path.ChangeExtension(file, "pdb");

                            //pdbFile = Path.Combine(arguments.PdbDirectory, pdbFile);

                            if (File.Exists(pdbFile))
                            {
                                package.Files.Add(new PhysicalPackageFile
                                {
                                    SourcePath = pdbFile,
                                    TargetPath = targetPath + Path.GetFileName(pdbFile),
                                });
                            }

                            var xmlFile = Path.ChangeExtension(file, "xml");

                            if (File.Exists(xmlFile))
                            {
                                package.Files.Add(new PhysicalPackageFile
                                {
                                    SourcePath = xmlFile,
                                    TargetPath = targetPath + Path.GetFileName(xmlFile),
                                });
                            }

                            var configFile = file + ".config";

                            if (File.Exists(configFile))
                            {
                                package.Files.Add(new PhysicalPackageFile
                                {
                                    SourcePath = configFile,
                                    TargetPath = targetPath + Path.GetFileName(configFile),
                                });
                            }


                            var dependencies = new List<PackageDependency>();

                            foreach (var refAssembly in assembly.GetReferencedAssemblies().Where(r => r.Name.StartsWith("Tiger")))
                            {
                                logAction(refAssembly.Name);

                                var refPackageId = refAssembly.Name;

                                if (refPackageId.Contains(dxVersion))
                                    refPackageId = refPackageId.Replace(dxVersion, string.Empty);


                                var refAssemblyVersion = refAssembly.Version;

                                var minVersion = new NuGetVersion(new Version(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build, refAssemblyVersion.Revision));
                                var maxVersion = new NuGetVersion(new Version(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build, refAssemblyVersion.Revision + 1));

                                var versionSpec = new VersionRange(minVersion, true, maxVersion, false);

                                var dependency = new PackageDependency(refPackageId, versionSpec);
                                dependencies.Add(dependency);
                            }

                            package.DependencyGroups.Add(new PackageDependencyGroup(NuGetFramework.Parse(netPath), dependencies));


                            CreateLocalization(file, package, arguments, netPath);
                        }
                        catch (Exception ex)
                        {
                            logExceptionAction(ex);
                            logLoadAssemblyAction(Tuple.Create(package.Id, ex));
                        }
                    }
                    catch (Exception ex)
                    {
                        logExceptionAction(ex);
                        unexpectedExceptionAction(ex);
                    }
                }

                if (!String.IsNullOrEmpty(targetPackagePath))
                {
                    if (File.Exists(targetPackagePath))
                        File.Delete(targetPackagePath);

                    using (var fs = new FileStream(targetPackagePath, FileMode.CreateNew, FileAccess.ReadWrite))
                    {
                        package.Save(fs);
                        Console.WriteLine(packageName);
                        successAction(package.Id);
                    }
                }
                package = null;
                packageName = string.Empty;
                targetPackagePath = string.Empty;
            }

        }


        private static void BuildPackages_Libs(ProgramArguments arguments, Action<string> logAction, Action<Exception> logExceptionAction, Action<Tuple<string, Exception>> logLoadAssemblyAction, Action<Exception> unexpectedExceptionAction, Action<string> successAction)
        {
            var files = Directory.EnumerateFiles(arguments.SourceDirectory, "*.dll")
                .Concat(Directory.EnumerateFiles(arguments.SourceDirectory, "*.exe")).ToList();
            foreach (var file in files)
            {
                try
                {
                    var packageName = Path.GetFileNameWithoutExtension(file);

                    var package = new PackageBuilder();

                    package.Description = "Yesfree Libs " + packageName;
                    package.Authors.Add("Yesfree Reference Libs");
                    package.IconUrl = new Uri("http://nuget.yesfree.cn/ico/libs.ico");
                    package.Copyright = "2008-" + DateTime.Today.Year;
                    package.ProjectUrl = new Uri("https://www.Yesfree.cn/libs");
                    package.Language = "zh-Hans";
                    package.Files.Add(new PhysicalPackageFile
                    {
                        SourcePath = file,
                        TargetPath = "lib/net40/" + Path.GetFileName(file),
                    });

                    try
                    {

                        var assembly = Assembly.LoadFile(file);

                        var pdbFile = Path.ChangeExtension(Path.GetFileName(file), "pdb");

                        pdbFile = Path.Combine(arguments.PdbDirectory, pdbFile);

                        if (File.Exists(pdbFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = pdbFile,
                                TargetPath = "lib/net40/" + Path.GetFileName(pdbFile),
                            });
                        }

                        var xmlFile = Path.ChangeExtension(file, "xml");

                        if (File.Exists(xmlFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = xmlFile,
                                TargetPath = "lib/net40/" + Path.GetFileName(xmlFile),
                            });
                        }

                        var configFile = file + ".config";

                        if (File.Exists(configFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = configFile,
                                TargetPath = "lib/net40/" + Path.GetFileName(configFile),
                            });
                        }


                        var assemblyVersion = assembly.GetName().Version;

                        var dxVersion = ".v" + assemblyVersion.Major + "." + assemblyVersion.Minor;

                        if (arguments.UseAssemblyFileVersion)
                        {
                            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                            var version = fvi.FileVersion;
                            assemblyVersion = new Version(version);
                        }

                        if (packageName.Contains(dxVersion))
                            packageName = packageName.Replace(dxVersion, string.Empty);
       
#if nuget
                        packageName = "YshXaf." + packageName;
#else
                        packageName += "_yesfree";
#endif
                        var targetPackagePath = Path.Combine(arguments.OutputDirectory, packageName + "." + assemblyVersion.ToString(4) + ".nupkg");

                        if (File.Exists(targetPackagePath))
                            File.Delete(targetPackagePath);

                        package.Id = packageName;
                        package.Version = new NuGetVersion(assemblyVersion);

                        var dependencies = new List<PackageDependency>();

                        foreach (var refAssembly in assembly.GetReferencedAssemblies())
                        {


                            var refPackageId = refAssembly.Name;
                            if (refPackageId == "mscorlib")
                                continue;
                            if (refPackageId == "Accessibility")
                                continue;
                            if (refPackageId == "WindowsBase")
                                continue;
                            if (refPackageId == "UIAutomationTypes")
                                continue;
                            if (refPackageId == "UIAutomationProvider")
                                continue;
                            if (refPackageId == "PresentationCore")
                                continue;
                            if (refPackageId.StartsWith("System"))
                                continue;
                            if (refPackageId.Contains(dxVersion))
                                refPackageId = refPackageId.Replace(dxVersion, string.Empty);

                            logAction(refAssembly.Name);

                            if (files.Any(f => f.IndexOf(refPackageId) != -1)) {
             
#if nuget
                                refPackageId = "YshXaf." + refPackageId;
#else
                                refPackageId += "_yesfree";
#endif
                            }

                            var refAssemblyVersion = refAssembly.Version;

                            var minVersion = new NuGetVersion(new Version(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build));
                            //var maxVersion = new NuGetVersion(new Version(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build + 1));

                            //var versionSpec = new VersionRange(minVersion, true, maxVersion, false);
                            var versionSpec = new VersionRange(minVersion);

                            var dependency = new PackageDependency(refPackageId, versionSpec);

                            dependencies.Add(dependency);

                        }

                        package.DependencyGroups.Add(new PackageDependencyGroup(NuGetFramework.AnyFramework, dependencies));


                        CreateLocalization(file, package, arguments);


                        using (var fs = new FileStream(targetPackagePath, FileMode.CreateNew, FileAccess.ReadWrite))
                        {
                            package.Save(fs);

                            successAction(package.Id);
                        }

                        Console.WriteLine(packageName);
                    }
                    catch (Exception ex)
                    {
                        logExceptionAction(ex);
                        logLoadAssemblyAction(Tuple.Create(package.Id, ex));
                    }
                }
                catch (Exception ex)
                {
                    logExceptionAction(ex);
                    unexpectedExceptionAction(ex);
                }
            }
        }

        private static void CreateLocalization(string file, PackageBuilder resourcePackage, ProgramArguments arguments, string netPath = "net40")
        {
            //var assemblyFileName = Path.GetFileName(file);
            var resourceFileName = Path.GetFileName(Path.ChangeExtension(file, "resources.dll"));

            foreach (var lang in arguments.LanguagesAsArray)
            {
                var localizedAssemblyPath = Path.Combine(arguments.SourceDirectory, lang, resourceFileName);
                if (File.Exists(localizedAssemblyPath))
                {
                    resourcePackage.Files.Add(new PhysicalPackageFile
                    {
                        SourcePath = localizedAssemblyPath,
                        TargetPath = "lib/" + netPath + "/" + lang + "/"+ resourceFileName,
                    });
                }

                var xmlFile = Path.ChangeExtension(localizedAssemblyPath, "xml");
                var xmlFileName = Path.GetFileName(xmlFile);
                if (File.Exists(xmlFile))
                {
                    resourcePackage.Files.Add(new PhysicalPackageFile
                    {
                        SourcePath = xmlFile,
                        TargetPath = "lib/" + netPath + "/" + lang + "/" + xmlFileName,
                    });
                }
            }
        }

        //private static void CreateLocalization(string file, PackageBuilder mainPackage, ProgramArguments arguments, string netPath= "net40")
        //{
        //    var assemblyFileName = Path.GetFileName(file);
        //    var resourceFileName = Path.GetFileName(Path.ChangeExtension(file, "resources.dll"));

        //    foreach (var lang in arguments.LanguagesAsArray)
        //    {
        //        var localizedAssemblyPath = Path.Combine(arguments.SourceDirectory, lang, resourceFileName);

        //        if (File.Exists(localizedAssemblyPath))
        //        {
        //            //resourcePackage.Files.Add(new PhysicalPackageFile
        //            //{
        //            //    SourcePath = localizedAssemblyPath,
        //            //    TargetPath = "lib/"+netPath+"/" + lang + "/" + Path.GetFileName(localizedAssemblyPath),
        //            //});
        //            //DevExpress.ExpressApp.Maps.Web_yesfree.18.1.4.0
        //            var packageName = mainPackage.Id;
        //            //{identifier}.{language}.{version}.nupkg
        //            //packageName = packageName.Substring(0, packageName.Length - 8)+lang +"."+packageName.Substring(packageName.Length-8, 8);
        //            var package = new PackageBuilder();

        //            package.Description = "DevExpress resources " + packageName;
        //            package.Authors.Add("DevExpress and Yesfree");
        //            package.IconUrl = new Uri("http://nuget.yesfree.cn/ico/xaf.ico");
        //            package.Copyright = "2008-" + DateTime.Today.Year;
        //            package.ProjectUrl = new Uri("https://www.Yesfree.cn/xaf");
        //            package.Language = lang;
        //            package.Files.Add(new PhysicalPackageFile
        //            {
        //                SourcePath = localizedAssemblyPath,
        //                TargetPath = "lib/"+ netPath + "/"+ lang +"/" + resourceFileName,
        //            });

        //            package.Id = packageName + "." + lang;
        //            package.Version = new NuGetVersion(mainPackage.Version);
        //            var targetPackagePath = Path.Combine(arguments.OutputDirectory, packageName +"."+lang+ "." + mainPackage.Version.Version.ToString(4) + ".nupkg");

        //            if (File.Exists(targetPackagePath))
        //                File.Delete(targetPackagePath);
        //            using (var fs = new FileStream(targetPackagePath, FileMode.CreateNew, FileAccess.ReadWrite))
        //            {
        //                package.Save(fs);
        //            }
        //        }
        //    }
        //}

        private static void PushPackages(ProgramArguments arguments)
        {
            
            if (arguments.Kind == 8 || arguments.Kind == 9)
            {
                var dlls = arguments.DllNames.Split(';', ',');
                Console.WriteLine("Pushing {0} packages to " + arguments.NugetSource, dlls.Length);
                foreach (var dll in dlls)
                {
                    foreach (var file in Directory.EnumerateFiles(arguments.OutputDirectory, dll + "*.nupkg", SearchOption.AllDirectories))
                    {
                        PushPackage(file, arguments);
                    }
                }
            }
            else {
                var packages = Directory.GetFiles(arguments.OutputDirectory, "*.nupkg").ToList();

                Console.WriteLine("Pushing {0} packages to " + arguments.NugetSource, packages.Count());

                foreach (var package in packages)
                {
                    PushPackage(package, arguments);
                }
            }

        }

        private static void PushPackage(string package, ProgramArguments arguments)
        {
            try
            {
                var packageName = "\"" + package + "\"";

                using (var process = new Process())
                {
                    Console.WriteLine(string.Format("push {0} -Source {1} -ApiKey {2}", packageName, arguments.NugetSource, arguments.NugetApiKey));
                    process.StartInfo = new ProcessStartInfo(System.Environment.CurrentDirectory + @"\nuget.exe", string.Format("push {0} -Source {1} -ApiKey {2}", packageName, arguments.NugetSource, arguments.NugetApiKey));
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.ErrorDialog = false;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;

                    process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                    process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

                    process.EnableRaisingEvents = true;

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadKey();
            }
        }

        private static void DeletePackages(ProgramArguments arguments)
        {
            var packages = Directory.GetFiles(arguments.OutputDirectory, "*.nupkg").ToList();

            Console.WriteLine("Deleting {0} packages to " + arguments.NugetSource, packages.Count());

            foreach (var package in packages)
            {
                try
                {
                    //var packageName = "\"" + package + "\"";
                    var packageName = Path.GetFileNameWithoutExtension(package);
                    //DevExpress.Charts.Core_yesfree.18.1.4.0
                    var packageVersion =  packageName.Substring(packageName.Length - 8, 8) ;
                    packageVersion = "18.2.5";
                    var packageId = packageName.Substring(0, packageName.Length - 9) ;
                    using (var process = new Process())
                    {
                        //nuget delete <packageID> <packageVersion> [options]
                        string cmd = string.Format("delete {0} {1} -Source {2} -ApiKey {3} -NonInteractive", packageId, packageVersion, arguments.NugetSource, arguments.NugetApiKey);
                        Console.WriteLine(cmd);
                        Console.WriteLine(System.Environment.CurrentDirectory + @"\nuget.exe");
                        process.StartInfo = new ProcessStartInfo(System.Environment.CurrentDirectory + @"\nuget.exe", cmd);
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.ErrorDialog = false;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;

                        process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                        process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

                        process.EnableRaisingEvents = true;

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Console.ReadKey();
                }

            }
        }
    }
}
