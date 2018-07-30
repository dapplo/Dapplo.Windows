#tool "xunit.runner.console"
#tool "OpenCover"
#tool "GitVersion.CommandLine"
#tool "docfx.console"
#tool nuget:?package=coveralls.net&version=0.7.0
#tool "PdbGit"
#addin "Cake.FileHelpers"
#addin "Cake.DocFx"
#addin "Cake.Coveralls"

var target = Argument("target", "Build");
var configuration = Argument("configuration", "release");

// Used to publish NuGet packages
var nugetApiKey = Argument("nugetApiKey", EnvironmentVariable("NuGetApiKey"));

// Used to publish coverage report
var coverallsRepoToken = Argument("coverallsRepoToken", EnvironmentVariable("CoverallsRepoToken"));

// where is our solution located?
var solutionFilePath = GetFiles("src/*.sln").First();
var solutionName = solutionFilePath.GetDirectory().GetDirectoryName();

// Check if we are in a pull request, publishing of packages and coverage should be skipped
var isPullRequest = !string.IsNullOrEmpty(EnvironmentVariable("APPVEYOR_PULL_REQUEST_NUMBER"));

// Check if the commit is marked as release
var isRelease = Argument<bool>("isRelease", string.Compare("[release]", EnvironmentVariable("appveyor_repo_commit_message_extended"), true) == 0);

// Used to store the version, which is needed during the build and the packaging
var version = EnvironmentVariable("APPVEYOR_BUILD_VERSION") ?? "1.0.0";

Task("Default")
    .IsDependentOn("Publish");

// Publish the Artifact of the Package Task to the Nexus Pro
Task("Publish")
	.IsDependentOn("CreateAndUploadCoverageReport")
    .IsDependentOn("PublishPackages")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !string.IsNullOrEmpty(nugetApiKey))
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRelease)
    .Does(()=>
{
});

Task("PublishPackages")
    .IsDependentOn("Package")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
    .WithCriteria(() => !string.IsNullOrEmpty(nugetApiKey))
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRelease)
    .Does(()=>
{
    var settings = new NuGetPushSettings {
        Source = "https://www.nuget.org/api/v2/package",
        ApiKey = nugetApiKey
    };

    var packages = GetFiles("./artifacts/*.nupkg").Where(p => !p.FullPath.ToLower().Contains("symbols"));
    NuGetPush(packages, settings);
});

// Package the results of the build, if the tests worked, into a NuGet Package
Task("Package")
    .IsDependentOn("Coverage")
    .IsDependentOn("Documentation")
    .IsDependentOn("GitLink")
    .Does(()=>
{
    var settings = new NuGetPackSettings 
    {
        OutputDirectory = "./artifacts/",
        Verbosity = NuGetVerbosity.Detailed,
        Symbols = true,
        IncludeReferencedProjects = true,
        Properties = new Dictionary<string, string>
        {
            { "Configuration", configuration },
            { "Platform", "AnyCPU" }
        }
    };

    var projectFilePaths = GetFiles("./**/*.csproj")
		.Where(p => !p.FullPath.ToLower().Contains("test"))
		.Where(p => !p.FullPath.ToLower().Contains("packages"))
		.Where(p => !p.FullPath.ToLower().Contains("tools"))
		.Where(p => !p.FullPath.ToLower().Contains("demo"))
		.Where(p => !p.FullPath.ToLower().Contains("diagnostics"))
		.Where(p => !p.FullPath.ToLower().Contains("power"))
		.Where(p => !p.FullPath.ToLower().Contains("example"));
    foreach(var projectFilePath in projectFilePaths)
    {
        Information("Packaging: " + projectFilePath.FullPath);
        NuGetPack(projectFilePath.FullPath, settings);
    }
});

// Build the DocFX documentation site
Task("Documentation")
    .Does(() =>
{
    DocFxMetadata("./doc/docfx.json");
    DocFxBuild("./doc/docfx.json");

    CreateDirectory("artifacts");
    // Archive the generated site
	Zip("./doc/_site", "./artifacts/site.zip");
});

Task("CreateAndUploadCoverageReport")
    .IsDependentOn("Coverage")
    .WithCriteria(() => !string.IsNullOrEmpty(coverallsRepoToken))
    .IsDependentOn("UploadCoverageReport")
    .Does(() =>
{
});

Task("UploadCoverageReport")
    .WithCriteria(() => FileExists("./artifacts/coverage.xml"))
    .WithCriteria(() => !string.IsNullOrEmpty(coverallsRepoToken))
    .Does(() =>
{
	CoverallsNet("./artifacts/coverage.xml", CoverallsNetReportType.OpenCover, new CoverallsNetSettings()
    {
        RepoToken = coverallsRepoToken
    });
});

// Run the XUnit tests via OpenCover, so be get an coverage.xml report
Task("Coverage")
    .IsDependentOn("Build")
    .WithCriteria(() => !BuildSystem.IsLocalBuild)
	.WithCriteria(() => GetFiles("./**/*.csproj").Where(csprojFile => csprojFile.FullPath.Contains("Test")).Any())
    .Does(() =>
{
    CreateDirectory("artifacts");

    var openCoverSettings = new OpenCoverSettings() {
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
                    // Add AppVeyor output, this "should" take care of a report inside AppVeyor
                    ArgumentCustomization = args => {
                        if (!BuildSystem.IsLocalBuild) {
                            args.Append("-appveyor");
                        }
                        return args;
                    },
                    ShadowCopy = false,
                    XmlReport = true,
                    HtmlReport = true,
                    ReportName = solutionName,
                    OutputDirectory = "./artifacts",
                    WorkingDirectory = "./src"
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
        ToolVersion = MSBuildToolVersion.VS2017,
        Configuration = configuration,
        PlatformTarget = PlatformTarget.MSIL
    };

    MSBuild(solutionFilePath.FullPath, settings);
    
    // Make sure the .dlls in the obj path are not found elsewhere
    CleanDirectories("./**/obj");
});

// Generate Git links in the PDB files
Task("GitLink")
    .IsDependentOn("Build")
    .Does(() =>
{
	FilePath pdbGitPath = Context.Tools.Resolve("PdbGit.exe");
	var pdbFiles = GetFiles("./**/*.pdb")
		.Where(p => !p.FullPath.ToLower().Contains("test"))
		.Where(p => !p.FullPath.ToLower().Contains("tools"))
		.Where(p => !p.FullPath.ToLower().Contains("packages"))
		.Where(p => !p.FullPath.ToLower().Contains("example"));
    foreach(var pdbFile in pdbFiles)
    {
		Information("Processing: " + pdbFile.FullPath);
		StartProcess(pdbGitPath, new ProcessSettings { Arguments = new ProcessArgumentBuilder().Append(pdbFile.FullPath)});
	}
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
    foreach(var assemblyInfoFile in GetFiles("./**/AssemblyInfo.cs").Where(p => !p.FullPath.ToLower().Contains("test")).Where(p => p.FullPath.Contains(solutionName))) {
        var assemblyInfo = ParseAssemblyInfo(assemblyInfoFile.FullPath);
        CreateAssemblyInfo(assemblyInfoFile.FullPath, new AssemblyInfoSettings {
            Version = version,
            InformationalVersion = version,
            FileVersion = version,

            CLSCompliant = assemblyInfo.ClsCompliant,
            Company = assemblyInfo.Company,
            ComVisible = assemblyInfo.ComVisible,
            Configuration = assemblyInfo.Configuration,
            Copyright = assemblyInfo.Copyright,
            //CustomAttributes = assemblyInfo.CustomAttributes,
            Description = assemblyInfo.Description,
            //Guid = assemblyInfo.Guid,
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