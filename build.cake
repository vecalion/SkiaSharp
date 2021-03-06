#addin "Cake.Xamarin"
#addin "Cake.XCode"
#addin "Cake.FileHelpers"

#load "cake/Utils.cake"

using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

var TARGET = Argument ("t", Argument ("target", Argument ("Target", "Default")));

var NuGetSources = new [] { MakeAbsolute (Directory ("./output")).FullPath, "https://api.nuget.org/v3/index.json" };
var NugetToolPath = GetToolPath ("nuget.exe");
var XamarinComponentToolPath = GetToolPath ("xamarin-component.exe");
var CakeToolPath = GetToolPath ("Cake/Cake.exe");
var NUnitConsoleToolPath = GetToolPath ("NUnit.Console/tools/nunit3-console.exe");
var GenApiToolPath = GetToolPath ("genapi.exe");
var MDocPath = GetToolPath ("mdoc/mdoc.exe");

DirectoryPath ROOT_PATH = MakeAbsolute(Directory("."));
DirectoryPath DEPOT_PATH = MakeAbsolute(ROOT_PATH.Combine("externals/depot_tools"));
DirectoryPath SKIA_PATH = MakeAbsolute(ROOT_PATH.Combine("externals/skia"));
DirectoryPath ANGLE_PATH = MakeAbsolute(ROOT_PATH.Combine("externals/angle"));

#load "cake/UtilsManaged.cake"
#load "cake/UtilsMSBuild.cake"
#load "cake/UtilsNative.cake"
#load "cake/TransformToTvOS.cake"
#load "cake/TransformToUWP.cake"
#load "cake/BuildExternals.cake"

////////////////////////////////////////////////////////////////////////////////////////////////////
// EXTERNALS - the native C and C++ libraries
////////////////////////////////////////////////////////////////////////////////////////////////////

// this builds all the externals
Task ("externals")
    .IsDependentOn ("externals-genapi")
    .IsDependentOn ("externals-native")
    .Does (() => 
{
});

////////////////////////////////////////////////////////////////////////////////////////////////////
// LIBS - the managed C# libraries
////////////////////////////////////////////////////////////////////////////////////////////////////

Task ("libs")
    .IsDependentOn ("externals")
    .IsDependentOn ("set-versions")
    .Does (() => 
{
    // set the SHA on the assembly info 
    var sha = EnvironmentVariable ("GIT_COMMIT") ?? string.Empty;
    if (!string.IsNullOrEmpty (sha) && sha.Length >= 6) {
        sha = sha.Substring (0, 6);
        Information ("Setting Git SHA to {0}.", sha);
        ReplaceTextInFiles ("./binding/Binding/Properties/SkiaSharpAssemblyInfo.cs", "{GIT_SHA}", sha);
        ReplaceTextInFiles ("./source/SkiaSharp.Views/SkiaSharp.Views.Shared/Properties/SkiaSharpViewsAssemblyInfo.cs", "{GIT_SHA}", sha);
        ReplaceTextInFiles ("./source/SkiaSharp.Views.Forms/SkiaSharp.Views.Forms.Shared/Properties/SkiaSharpViewsFormsAssemblyInfo.cs", "{GIT_SHA}", sha);
    }

    // create all the directories
    if (!DirectoryExists ("./output/windows/")) CreateDirectory ("./output/windows/");
    if (!DirectoryExists ("./output/uwp/")) CreateDirectory ("./output/uwp/");
    if (!DirectoryExists ("./output/android/")) CreateDirectory ("./output/android/");
    if (!DirectoryExists ("./output/ios/")) CreateDirectory ("./output/ios/");
    if (!DirectoryExists ("./output/tvos/")) CreateDirectory ("./output/tvos/");
    if (!DirectoryExists ("./output/osx/")) CreateDirectory ("./output/osx/");
    if (!DirectoryExists ("./output/portable/")) CreateDirectory ("./output/portable/");
    if (!DirectoryExists ("./output/mac/")) CreateDirectory ("./output/mac/");

    if (IsRunningOnWindows ()) {
        // build bindings
        RunNuGetRestore ("binding/SkiaSharp.Windows.sln");
        DotNetBuild ("binding/SkiaSharp.Windows.sln", c => { 
            c.Configuration = "Release"; 
        });

        // copy build output
        CopyFileToDirectory ("./binding/SkiaSharp.Portable/bin/Release/SkiaSharp.dll", "./output/portable/");
        CopyFileToDirectory ("./binding/SkiaSharp.Desktop/bin/Release/SkiaSharp.dll", "./output/windows/");
        CopyFileToDirectory ("./binding/SkiaSharp.Desktop/bin/Release/SkiaSharp.pdb", "./output/windows/");
        CopyFileToDirectory ("./binding/SkiaSharp.Desktop/bin/Release/SkiaSharp.dll.config", "./output/windows/");
        CopyFileToDirectory ("./binding/SkiaSharp.Desktop/bin/Release/SkiaSharp.Desktop.targets", "./output/windows/");
        CopyFileToDirectory ("./binding/SkiaSharp.UWP/bin/Release/SkiaSharp.dll", "./output/uwp/");
        CopyFileToDirectory ("./binding/SkiaSharp.UWP/bin/Release/SkiaSharp.pdb", "./output/uwp/");
        CopyFileToDirectory ("./binding/SkiaSharp.UWP/bin/Release/SkiaSharp.pri", "./output/uwp/");
        CopyFileToDirectory ("./binding/SkiaSharp.UWP/bin/Release/SkiaSharp.UWP.targets", "./output/uwp/");

        // build other source
        RunNuGetRestore ("./source/SkiaSharpSource.Windows.sln");
        DotNetBuild ("./source/SkiaSharpSource.Windows.sln", c => { 
            c.Configuration = "Release"; 
        });

        // copy the managed views
        CopyFileToDirectory ("./source/SkiaSharp.Views/SkiaSharp.Views.UWP/bin/Release/SkiaSharp.Views.UWP.dll", "./output/uwp/");
        CopyFileToDirectory ("./source/SkiaSharp.Views/SkiaSharp.Views.UWP/bin/Release/SkiaSharp.Views.UWP.targets", "./output/uwp/");
        CopyFileToDirectory ("./source/SkiaSharp.Views/SkiaSharp.Views.Desktop/bin/Release/SkiaSharp.Views.Desktop.dll", "./output/windows/");
        CopyFileToDirectory ("./source/SkiaSharp.Views/SkiaSharp.Views.WPF/bin/Release/SkiaSharp.Views.WPF.dll", "./output/windows/");
        CopyFileToDirectory ("./source/SkiaSharp.Views.Forms/SkiaSharp.Views.Forms/bin/Release/SkiaSharp.Views.Forms.dll", "./output/portable/");
        CopyFileToDirectory ("./source/SkiaSharp.Views.Forms/SkiaSharp.Views.Forms.UWP/bin/Release/SkiaSharp.Views.Forms.dll", "./output/uwp/");

    }

    if (IsRunningOnUnix ()) {
        // build
        RunNuGetRestore ("binding/SkiaSharp.Mac.sln");
        DotNetBuild ("binding/SkiaSharp.Mac.sln", c => { 
            c.Configuration = "Release"; 
        });

        // copy build output
        CopyFileToDirectory ("./binding/SkiaSharp.Android/bin/Release/SkiaSharp.dll", "./output/android/");
        CopyFileToDirectory ("./binding/SkiaSharp.iOS/bin/Release/SkiaSharp.dll", "./output/ios/");
        CopyFileToDirectory ("./binding/SkiaSharp.tvOS/bin/Release/SkiaSharp.dll", "./output/tvos/");
        CopyFileToDirectory ("./binding/SkiaSharp.OSX/bin/Release/SkiaSharp.dll", "./output/osx/");
        CopyFileToDirectory ("./binding/SkiaSharp.OSX/bin/Release/SkiaSharp.OSX.targets", "./output/osx/");
        CopyFileToDirectory ("./binding/SkiaSharp.Portable/bin/Release/SkiaSharp.dll", "./output/portable/");
        CopyFileToDirectory ("./binding/SkiaSharp.Desktop/bin/Release/SkiaSharp.dll", "./output/mac/");
        CopyFileToDirectory ("./binding/SkiaSharp.Desktop/bin/Release/SkiaSharp.Desktop.targets", "./output/mac/");
        CopyFileToDirectory ("./binding/SkiaSharp.Desktop/bin/Release/SkiaSharp.dll.config", "./output/mac/");

        // build other source
        RunNuGetRestore ("./source/SkiaSharpSource.Mac.sln");
        DotNetBuild ("./source/SkiaSharpSource.Mac.sln", c => { 
            c.Configuration = "Release"; 
        });

        // copy other outputs
        CopyFileToDirectory ("./source/SkiaSharp.Views/SkiaSharp.Views.Android/bin/Release/SkiaSharp.Views.Android.dll", "./output/android/");
        CopyFileToDirectory ("./source/SkiaSharp.Views/SkiaSharp.Views.iOS/bin/Release/SkiaSharp.Views.iOS.dll", "./output/ios/");
        CopyFileToDirectory ("./source/SkiaSharp.Views/SkiaSharp.Views.tvOS/bin/Release/SkiaSharp.Views.tvOS.dll", "./output/tvos/");
        CopyFileToDirectory ("./source/SkiaSharp.Views/SkiaSharp.Views.Mac/bin/Release/SkiaSharp.Views.Mac.dll", "./output/osx/");
        CopyFileToDirectory ("./source/SkiaSharp.Views.Forms/SkiaSharp.Views.Forms/bin/Release/SkiaSharp.Views.Forms.dll", "./output/portable/");
        CopyFileToDirectory ("./source/SkiaSharp.Views.Forms/SkiaSharp.Views.Forms.Android/bin/Release/SkiaSharp.Views.Forms.dll", "./output/android/");
        CopyFileToDirectory ("./source/SkiaSharp.Views.Forms/SkiaSharp.Views.Forms.iOS/bin/Release/SkiaSharp.Views.Forms.dll", "./output/ios/");
    }
});

////////////////////////////////////////////////////////////////////////////////////////////////////
// TESTS - some test cases to make sure it works
////////////////////////////////////////////////////////////////////////////////////////////////////

Task ("tests")
    .IsDependentOn ("libs")
    .Does (() => 
{
    RunNuGetRestore ("./tests/SkiaSharp.Desktop.Tests/SkiaSharp.Desktop.Tests.sln");
    
    // Windows (x86 and x64)
    if (IsRunningOnWindows ()) {
        DotNetBuild ("./tests/SkiaSharp.Desktop.Tests/SkiaSharp.Desktop.Tests.sln", c => { 
            c.Configuration = "Release"; 
            c.Properties ["Platform"] = new [] { "x86" };
        });
        RunTests("./tests/SkiaSharp.Desktop.Tests/bin/x86/Release/SkiaSharp.Desktop.Tests.dll");
        DotNetBuild ("./tests/SkiaSharp.Desktop.Tests/SkiaSharp.Desktop.Tests.sln", c => { 
            c.Configuration = "Release"; 
            c.Properties ["Platform"] = new [] { "x64" };
        });
        RunTests("./tests/SkiaSharp.Desktop.Tests/bin/x64/Release/SkiaSharp.Desktop.Tests.dll");
    }
    // Mac OSX (Any CPU)
    if (IsRunningOnUnix ()) {
        DotNetBuild ("./tests/SkiaSharp.Desktop.Tests/SkiaSharp.Desktop.Tests.sln", c => { 
            c.Configuration = "Release"; 
        });
        RunTests("./tests/SkiaSharp.Desktop.Tests/bin/AnyCPU/Release/SkiaSharp.Desktop.Tests.dll");
    }
});

////////////////////////////////////////////////////////////////////////////////////////////////////
// SAMPLES - the demo apps showing off the work
////////////////////////////////////////////////////////////////////////////////////////////////////

Task ("samples")
    .IsDependentOn ("libs")
    .IsDependentOn ("nuget")
    .Does (() => 
{
    ClearSkiaSharpNuGetCache ();

    if (IsRunningOnUnix ()) {
        RunNuGetRestore ("./samples/MacSample/MacSample.sln");
        DotNetBuild ("./samples/MacSample/MacSample.sln", c => { 
            c.Configuration = "Release"; 
        });
        RunNuGetRestore ("./samples/FormsSample/FormsSample.Mac.sln");
        DotNetBuild ("./samples/FormsSample/FormsSample.Mac.sln", c => { 
            c.Configuration = "Release"; 
            c.Properties ["Platform"] = new [] { "iPhone" };
        });
        RunNuGetRestore ("./samples/TvSample/TvSample.sln");
        DotNetBuild ("./samples/TvSample/TvSample.sln", c => { 
            c.Configuration = "Release"; 
            c.Properties ["Platform"] = new [] { "iPhoneSimulator" };
        });
    }
    
    if (IsRunningOnWindows ()) {
        RunNuGetRestore ("./samples/WPFSample/WPFSample.sln");
        DotNetBuild ("./samples/WPFSample/WPFSample.sln", c => { 
            c.Configuration = "Release";
            c.Properties ["Platform"] = new [] { "x86" };
        });
        RunNuGetRestore ("./samples/UWPSample/UWPSample.sln");
        DotNetBuild ("./samples/UWPSample/UWPSample.sln", c => { 
            c.Configuration = "Release"; 
        });
        RunNuGetRestore ("./samples/FormsSample/FormsSample.Windows.sln");
        DotNetBuild ("./samples/FormsSample/FormsSample.Windows.sln", c => { 
            c.Configuration = "Release"; 
        });
        RunNuGetRestore ("./samples/WindowsSample/WindowsSample.sln");
        DotNetBuild ("./samples/WindowsSample/WindowsSample.sln", c => { 
            c.Configuration = "Release"; 
            c.Properties ["Platform"] = new [] { "x86" };
        });
    }
});

////////////////////////////////////////////////////////////////////////////////////////////////////
// DOCS - building the API documentation
////////////////////////////////////////////////////////////////////////////////////////////////////

Task ("docs")
    .IsDependentOn ("set-versions")
    .IsDependentOn ("externals-genapi")
    .Does (() => 
{
    RunMdocUpdate ("./binding/SkiaSharp.Generic/bin/Release/SkiaSharp.dll", "./docs/en/");
    
    if (!DirectoryExists ("./output/docs/msxml/")) CreateDirectory ("./output/docs/msxml/");
    RunMdocMSXml ("./docs/en/", "./output/docs/msxml/SkiaSharp.xml");
    
    if (!DirectoryExists ("./output/docs/mdoc/")) CreateDirectory ("./output/docs/mdoc/");
    RunMdocAssemble ("./docs/en/", "./output/docs/mdoc/SkiaSharp");
});

////////////////////////////////////////////////////////////////////////////////////////////////////
// NUGET - building the package for NuGet.org
////////////////////////////////////////////////////////////////////////////////////////////////////

Task ("nuget")
    .IsDependentOn ("libs")
    .IsDependentOn ("docs")
    .Does (() => 
{
    // we can only build the combined package on CI
    if (TARGET == "CI") {
        PackageNuGet ("./nuget/SkiaSharp.nuspec", "./output/");
        PackageNuGet ("./nuget/SkiaSharp.Views.nuspec", "./output/");
        PackageNuGet ("./nuget/SkiaSharp.Views.Forms.nuspec", "./output/");
    } else {
        if (IsRunningOnWindows ()) {
            PackageNuGet ("./nuget/SkiaSharp.Windows.nuspec", "./output/");
            PackageNuGet ("./nuget/SkiaSharp.Views.Windows.nuspec", "./output/");
            PackageNuGet ("./nuget/SkiaSharp.Views.Forms.Windows.nuspec", "./output/");
        }
        if (IsRunningOnUnix ()) {
            PackageNuGet ("./nuget/SkiaSharp.Mac.nuspec", "./output/");
            PackageNuGet ("./nuget/SkiaSharp.Views.Mac.nuspec", "./output/");
            PackageNuGet ("./nuget/SkiaSharp.Views.Forms.Mac.nuspec", "./output/");
        }
    }
});

////////////////////////////////////////////////////////////////////////////////////////////////////
// COMPONENT - building the package for components.xamarin.com
////////////////////////////////////////////////////////////////////////////////////////////////////

Task ("component")
    .IsDependentOn ("nuget")
    .Does (() => 
{
    // TODO: Not yet ready
    
    // if (!DirectoryExists ("./output/")) {
    //     CreateDirectory ("./output/");
    // }
    
    // FilePath yaml = "./component/component.yaml";
    // var yamlDir = yaml.GetDirectory ();
    // PackageComponent (yamlDir, new XamarinComponentSettings { 
    //     ToolPath = XamarinComponentToolPath
    // });

    // MoveFiles (yamlDir.FullPath.TrimEnd ('/') + "/*.xam", "./output/");
});

////////////////////////////////////////////////////////////////////////////////////////////////////
// VERSIONS - update all packages and references to the new version
////////////////////////////////////////////////////////////////////////////////////////////////////

Task ("set-versions")
    .Does (() => 
{
    // set the SHA on the assembly info 
    var sha = EnvironmentVariable ("GIT_COMMIT") ?? string.Empty;
    if (!string.IsNullOrEmpty (sha) && sha.Length >= 6) {
        sha = sha.Substring (0, 6);
    } else {
        sha = "{GIT_SHA}";
    }

    // the versions
    var version = "1.54.0.0";
    var fileVersion = "1.54.1.0";
    var versions = new Dictionary<string, string> {
        { "SkiaSharp", "1.54.1" },
        { "SkiaSharp.Views", "1.54.1-beta1" },
        { "SkiaSharp.Views.Forms", "1.54.1-beta1" },
    };

    var files = new List<string> ();
    var add = new Action<string> (glob => {
        files.AddRange (GetFiles (glob).Select (p => MakeAbsolute (p).ToString ()));
    });
    // nuspecs
    add ("./nuget/*.nuspec");
    // packages files
    add ("./source/*/*/packages.config");
    add ("./source/*/*/project.json");
    // project files
    add ("./source/*/*/*.nuget.targets");
    add ("./source/*/*/*.csproj");
    // sample packages files
    add ("./samples/*/*/packages.config");
    add ("./samples/*/*/project.json");
    // sample project files
    add ("./samples/*/*/*.nuget.targets");
    add ("./samples/*/*/*.csproj");
    // update
    foreach (var file in files) {
        UpdateSkiaSharpVersion (file, versions);
    }

    // assembly infos
    UpdateAssemblyInfo (
        "./binding/Binding/Properties/SkiaSharpAssemblyInfo.cs",
        version, fileVersion, sha);
    UpdateAssemblyInfo (
        "./source/SkiaSharp.Views/SkiaSharp.Views.Shared/Properties/SkiaSharpViewsAssemblyInfo.cs",
        version, fileVersion, sha);
    UpdateAssemblyInfo (
        "./source/SkiaSharp.Views.Forms/SkiaSharp.Views.Forms.Shared/Properties/SkiaSharpViewsFormsAssemblyInfo.cs",
        version, fileVersion, sha);
});

////////////////////////////////////////////////////////////////////////////////////////////////////
// CLEAN - remove all the build artefacts
////////////////////////////////////////////////////////////////////////////////////////////////////

Task ("clean")
    .IsDependentOn ("clean-externals")
    .IsDependentOn ("clean-managed")
    .Does (() => 
{
});
Task ("clean-managed").Does (() => 
{
    CleanDirectories ("./binding/*/bin");
    CleanDirectories ("./binding/*/obj");

    CleanDirectories ("./samples/*/bin");
    CleanDirectories ("./samples/*/obj");
    CleanDirectories ("./samples/*/AppPackages");
    CleanDirectories ("./samples/*/*/bin");
    CleanDirectories ("./samples/*/*/obj");
    CleanDirectories ("./samples/*/*/AppPackages");
    CleanDirectories ("./samples/*/packages");

    CleanDirectories ("./tests/**/bin");
    CleanDirectories ("./tests/**/obj");

    CleanDirectories ("./source/*/*/bin");
    CleanDirectories ("./source/*/*/obj");
    CleanDirectories ("./source/*/*/Generated Files");
    CleanDirectories ("./source/packages");

    if (DirectoryExists ("./output"))
        DeleteDirectory ("./output", true);
});

////////////////////////////////////////////////////////////////////////////////////////////////////
// DEFAULT - target for common development
////////////////////////////////////////////////////////////////////////////////////////////////////

Task ("Default")
    .IsDependentOn ("externals")
    .IsDependentOn ("libs");

Task ("Everything")
    .IsDependentOn ("externals")
    .IsDependentOn ("libs")
    .IsDependentOn ("docs")
    .IsDependentOn ("nuget")
    .IsDependentOn ("component")
    .IsDependentOn ("tests")
    .IsDependentOn ("samples");

////////////////////////////////////////////////////////////////////////////////////////////////////
// CI - the master target to build everything
////////////////////////////////////////////////////////////////////////////////////////////////////

Task ("CI")
    .IsDependentOn ("externals")
    .IsDependentOn ("libs")
    .IsDependentOn ("docs")
    .IsDependentOn ("nuget")
    .IsDependentOn ("component")
    .IsDependentOn ("tests")
    .IsDependentOn ("samples");

Task ("Windows-CI")
    .IsDependentOn ("externals")
    .IsDependentOn ("libs")
    .IsDependentOn ("docs")
    .IsDependentOn ("nuget")
    .IsDependentOn ("component")
    .IsDependentOn ("tests")
    .IsDependentOn ("samples");

////////////////////////////////////////////////////////////////////////////////////////////////////
// BUILD NOW 
////////////////////////////////////////////////////////////////////////////////////////////////////

Information ("Cake.exe ToolPath: {0}", CakeToolPath);
Information ("Cake.exe NUnitConsoleToolPath: {0}", NUnitConsoleToolPath);
Information ("NuGet.exe ToolPath: {0}", NugetToolPath);
Information ("Xamarin-Component.exe ToolPath: {0}", XamarinComponentToolPath);
Information ("genapi.exe ToolPath: {0}", GenApiToolPath);

ListEnvironmentVariables ();

RunTarget (TARGET);
