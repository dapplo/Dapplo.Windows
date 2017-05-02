#tool "xunit.runner.console"
#tool "OpenCover"
#tool "docfx.console"
#addin "MagicChunks"
#addin "Cake.FileHelpers"
#addin "Cake.DocFx"

var target = Argument("target", "Build");
var configuration = Argument("configuration", "release");
var nugetApiKey = Argument("nugetApiKey", EnvironmentVariable("NuGetApiKey"));
var solutionFilePath = GetFiles("./**/*.sln").First();
// Used to store the version, which is needed during the build and the packaging
var version = EnvironmentVariable("APPVEYOR_BUILD_VERSION", "1.0.0");

Task("Default")
    .IsDependentOn("Package");

// Publish the Artifact of the Package Task to the Nexus Pro
Task("Publish")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !string.IsNullOrEmpty(nugetApiKey))
    .Does(()=>
{
    var settings = new NuGetPushSettings {
        ApiKey = nugetApiKey
    };

    var packages = GetFiles("./artifacts/*.nupkg");
    NuGetPush(packages, settings);
});

// Package the results of the build, if the tests worked, into a NuGet Package
Task("Package")
    .IsDependentOn("Coverage")
    .IsDependentOn("AssemblyVersion")
    .IsDependentOn("Documentation")
    .Does(()=>
{
    var settings = new NuGetPackSettings 
    {
        OutputDirectory = "./artifacts/",
        Verbosity = NuGetVerbosity.Detailed,
        Symbols = true,
        Properties = new Dictionary<string, string>
        {
            { "Configuration", configuration }
        }
    };

    var projectFilePaths = GetFiles("./**/*.csproj").Where(p => !p.FullPath.Contains("Test") && !p.FullPath.Contains("packages") &&!p.FullPath.Contains("tools"));
    foreach(var projectFilePath in projectFilePaths)
    {
        var nuspecFilename = string.Format("{0}/{1}.nuspec", projectFilePath.GetDirectory(),projectFilePath.GetFilenameWithoutExtension());
        TransformConfig(nuspecFilename, 
            new TransformationCollection {
                { "package/metadata/version", version.NuGetVersion }
            });
        Information("Packaging: " + projectFilePath.FullPath);

        NuGetPack(projectFilePath.FullPath, settings);
    }
});

// Build the DocFX documentation site
Task("Documentation")
    .Does(() =>
{
    DocFx("./doc/docfx.json");
});

// Run the XUnit tests via OpenCover, so be get an coverage.xml report
Task("Coverage")
    .IsDependentOn("Build")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .Does(() =>
{
    CreateDirectory("artifacts");

    var openCoverSettings =new OpenCoverSettings() {
        // Forces error in build when tests fail
        ReturnTargetCodeOffset = 0
    };

    var projectFiles = GetFiles("./**/*.csproj");
    foreach(var projectFile in projectFiles)
    {
        var projectName = projectFile.GetDirectory().GetDirectoryName();
        if (projectName.Contains("Test")) {
           openCoverSettings.WithFilter("-["+projectName+"]*");
        }
        else {
           openCoverSettings.WithFilter("+["+projectName+"]*");
        }
    }

    // Make XUnit 2 run via the OpenCover process
    OpenCover(
        // The test tool Lamdba
        tool => {
            tool.XUnit2("./**/*.Tests.dll",
                new XUnit2Settings {
                    ShadowCopy = false
                });
            },
        // The output path
        new FilePath("./artifacts/coverage.xml"),
        // Settings
       openCoverSettings
    );
});

// This starts the actual MSBuild
Task("Build")
    .IsDependentOn("RestoreNuGetPackages")
    .IsDependentOn("Clean")
    .IsDependentOn("AssemblyVersion")
    .Does(() =>
{
    var settings = new MSBuildSettings {
        Verbosity = Verbosity.Minimal,
        ToolVersion = MSBuildToolVersion.VS2015,
        Configuration = configuration,
        PlatformTarget = PlatformTarget.MSIL
    };

    MSBuild(solutionFilePath.FullPath, settings);
    
    // Make sure the .dlls in the obj path are not found elsewhere
    CleanDirectories("./**/obj");
});

// Load the needed NuGet packages to make the build work
Task("RestoreNuGetPackages")
    .Does(() =>
{
    NuGetRestore(solutionFilePath.FullPath);
});

// Version is written to the AssemblyInfo files when !BuildSystem.IsLocalBuild
Task("AssemblyVersion")
    .Does(() =>
{
    foreach(var assemblyInfoFile in  GetFiles("./**/AssemblyInfo.cs")) {
        var assemblyInfo = ParseAssemblyInfo(assemblyInfoFile.FullPath);
        CreateAssemblyInfo(assemblyInfoFile.FullPath, new AssemblyInfoSettings {
            Version = version,
            InformationalVersion = version,
            FileVersion = version,

            CLSCompliant = assemblyInfo.CLSCompliant,
            Company = assemblyInfo.Company,
            ComVisible = assemblyInfo.ComVisible,
            Configuration = assemblyInfo.Configuration,
            Copyright = assemblyInfo.Copyright,
            CustomAttributes = assemblyInfo.CustomAttributes,
            Description = assemblyInfo.Description,
            Guid = assemblyInfo.Guid,
            InternalsVisibleTo = assemblyInfo.InternalsVisibleTo,
            Product = assemblyInfo.Product,
            Title = assemblyInfo.Title,
            Trademark = assemblyInfo.Trademark
        });
    }
});


// Clean all unneeded files, so we build on a clean file system
Task("Clean")
    .Does(() =>
{
    CleanDirectories("./**/obj");
    CleanDirectories("./**/bin");
    CleanDirectories("./artifacts");	
});

RunTarget(target);