#tool "xunit.runner.console"
#tool "OpenCover"
#tool "docfx.console"
#tool "coveralls.io"
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

Task("Default")
	.IsDependentOn("Publish");

// Publish taks depends on publish specifics
Task("Publish")
	.IsDependentOn("PublishPackages")
	.IsDependentOn("PublishCoverage")
	.IsDependentOn("Documentation")
	.WithCriteria(() => !BuildSystem.IsLocalBuild);

// Publish the coveralls report to Coveralls.NET
Task("PublishCoverage")
	.IsDependentOn("Coverage")
	.WithCriteria(() => !BuildSystem.IsLocalBuild)
	.WithCriteria(() => !string.IsNullOrEmpty(coverallsRepoToken))
	.WithCriteria(() => !isPullRequest)
	.Does(()=>
{
	CoverallsIo("./artifacts/coverage.xml", new CoverallsIoSettings
	{
		RepoToken = coverallsRepoToken
	});
});

// Publish the Artifacts of the Package Task to NuGet
Task("PublishPackages")
	.IsDependentOn("Coverage")
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

	var packages = GetFiles("./src/**/*.nupkg").Where(p => !p.FullPath.ToLower().Contains("symbols"));
	NuGetPush(packages, settings);
});

// Build the DocFX documentation site
Task("Documentation")
	.Does(() =>
{
	// Run DocFX
	DocFxMetadata("./doc/docfx.json");
	DocFxBuild("./doc/docfx.json");
	
	CreateDirectory("artifacts");
	// Archive the generated site
	Zip("./doc/_site", "./artifacts/site.zip");
});

// Run the XUnit tests via OpenCover, so be get an coverage.xml report
Task("Coverage")
	.IsDependentOn("Build")
	.Does(() =>
{
	CreateDirectory("artifacts");

	var openCoverSettings = new OpenCoverSettings() {
		// Forces error in build when tests fail
		ReturnTargetCodeOffset = 0
	};
	
	var projectFilePaths = GetFiles("./**/*.csproj")
		.Where(p => !p.FullPath.ToLower().Contains("demo"))
		.Where(p => !p.FullPath.ToLower().Contains("packages"))
		.Where(p => !p.FullPath.ToLower().Contains("tools"))
		.Where(p => !p.FullPath.ToLower().Contains("example"));
	foreach(var projectFile in projectFilePaths)
	{
		var projectName = projectFile.GetDirectory().GetDirectoryName();
		if (projectName.ToLower().Contains("test")) {
			openCoverSettings.WithFilter("-["+projectName+"]*");
		}
		else {
			openCoverSettings.WithFilter("+["+projectName+"]*");
		}
	}
	
	var xunit2Settings = new XUnit2Settings {
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
	};

	// Make XUnit 2 run via the OpenCover process
	OpenCover(
		// The test tool Lamdba
		tool => tool.XUnit2("./**/bin/**/*.Tests.dll", xunit2Settings),
		// The output path
		new FilePath("./artifacts/coverage.xml"),
		// Settings
		openCoverSettings
	);
});

// This starts the actual MSBuild
Task("Build")
	.IsDependentOn("Clean")
	.Does(() =>
{
	MSBuild(solutionFilePath.FullPath, new MSBuildSettings {
		Verbosity = Verbosity.Minimal,
		ToolVersion = MSBuildToolVersion.VS2017,
		Configuration = "Release",
		Restore = true,
		PlatformTarget = PlatformTarget.MSIL
	});
});

// Clean all unneeded files, so we build on a clean file system
Task("Clean")
	.Does(() =>
{
	CleanDirectories("./**/obj");
	CleanDirectories("./**/bin");
	CleanDirectories("./artifacts");
});

Task("EnableDNC30")
    .Does(() =>
{
    ReplaceRegexInFiles("./**/*.csproj", "<TargetFrameworks>.*</TargetFrameworks><!-- net471;netcoreapp3.0 -->", "<TargetFrameworks>net471;netcoreapp3.0</TargetFrameworks>");
    ReplaceRegexInFiles("./**/*.csproj", "<TargetFrameworks>.*</TargetFrameworks><!-- net471;netcoreapp3.0;netstandard2.0 -->", "<TargetFrameworks>net471;netcoreapp3.0;netstandard2.0</TargetFrameworks>");
    ReplaceRegexInFiles("./**/*.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><!-- Microsoft.NET.Sdk.WindowsDesktop -->", "<Project Sdk=\"Microsoft.NET.Sdk.WindowsDesktop\">");
});

Task("DisableDNC30")
    .Does(() =>
{
    ReplaceRegexInFiles("./**/*.csproj", "<TargetFrameworks>net471;netcoreapp3.0</TargetFrameworks>", "<TargetFrameworks>net471</TargetFrameworks><!-- net471;netcoreapp3.0 -->");
    ReplaceRegexInFiles("./**/*.csproj", "<TargetFrameworks>net471;netcoreapp3.0;netstandard2.0</TargetFrameworks>", "<TargetFrameworks>net471</TargetFrameworks><!-- net471;netcoreapp3.0;netstandard2.0 -->");
    ReplaceRegexInFiles("./**/*.csproj", "<Project Sdk=\"Microsoft.NET.Sdk.WindowsDesktop\">", "<Project Sdk=\"Microsoft.NET.Sdk\"><!-- Microsoft.NET.Sdk.WindowsDesktop -->");
});

RunTarget(target);