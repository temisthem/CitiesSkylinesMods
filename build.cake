var target = Argument("target", "Install");
var configuration = Argument("configuration", "Release");

var modsRoot = Directory((EnvironmentVariable("LOCALAPPDATA")) + "/Colossal Order/Cities_Skylines/Addons/Mods");
var peInstallFolder = modsRoot + Directory("PrecisionEngineering");
var pssInstallFolder = modsRoot + Directory("PedestrianStreetServices");

//////////////////////////////////////////////////////////////////////
// TASKS — PRECISION ENGINEERING
//////////////////////////////////////////////////////////////////////

Task("Clean-PE")
    .Does(() =>
{
    CleanDirectory($"./src/PrecisionEngineering/bin/{configuration}");
    CleanDirectory($"./src/PrecisionEngineering/obj/{configuration}");
});

Task("Build-PE")
    .IsDependentOn("Clean-PE")
    .Does(() =>
{
    MSBuild("./src/PrecisionEngineering/PrecisionEngineering.csproj", configurator =>
        configurator.SetConfiguration(configuration).WithRestore());
});

Task("Install-PE")
    .IsDependentOn("Build-PE")
    .Does(() =>
{
    CopyFileToDirectory($"./src/PrecisionEngineering/bin/{configuration}/PrecisionEngineering.dll", peInstallFolder);
});

//////////////////////////////////////////////////////////////////////
// TASKS — PEDESTRIAN STREET SERVICES
//////////////////////////////////////////////////////////////////////

Task("Clean-PSS")
    .Does(() =>
{
    CleanDirectory($"./src/PedestrianStreetServices/bin/{configuration}");
    CleanDirectory($"./src/PedestrianStreetServices/obj/{configuration}");
});

Task("Build-PSS")
    .IsDependentOn("Clean-PSS")
    .Does(() =>
{
    MSBuild("./src/PedestrianStreetServices/PedestrianStreetServices.csproj", configurator =>
        configurator.SetConfiguration(configuration).WithRestore());
});

Task("Install-PSS")
    .IsDependentOn("Build-PSS")
    .Does(() =>
{
    CopyFileToDirectory($"./src/PedestrianStreetServices/bin/{configuration}/PedestrianStreetServices.dll", pssInstallFolder);
});

//////////////////////////////////////////////////////////////////////
// AGGREGATE TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .IsDependentOn("Clean-PE")
    .IsDependentOn("Clean-PSS");

Task("Build")
    .IsDependentOn("Build-PE")
    .IsDependentOn("Build-PSS");

Task("Install")
    .IsDependentOn("Install-PE")
    .IsDependentOn("Install-PSS");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);