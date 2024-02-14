using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Flow.Launcher.Plugin.JetBrainsIDEProjects;

internal static class RecentProjectsReader
{
    private static readonly string ToolboxDirectoryPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JetBrains", "Toolbox");

    private static readonly string StatePath =
        Path.Combine(ToolboxDirectoryPath, "state.json");

    private static readonly string[] BlacklistedToolIds = { "Space" };

    private static String ConvertDisplayNameToProduct(string displayName)
    {
        // handle like IntelliJ IDEA Ultimate
        if (displayName.StartsWith("IntelliJ IDEA"))
        {
            return "IntelliJIdea";
        } else if (displayName.StartsWith("PyCharm"))
        {
            return "PyCharm";
        } else if (displayName.StartsWith("Android Studio"))
        {
            return "AndroidStudio";
        }
        return displayName;
    }


    public static List<ApplicationInfo> GetApplications()
    {
        if (!File.Exists(StatePath))
        {
            throw new FileNotFoundException($"File not found: {StatePath}. Is toolbox V2 installed?");
        }

        using var stateStream = File.OpenRead(StatePath);
        var state = JsonSerializer.Deserialize<State>(stateStream);

        var applications = new List<ApplicationInfo>();
        foreach (var tool in state.Tools)
        {
            if (BlacklistedToolIds.Contains(tool.ToolId) || tool.LaunchCommand == "")
            {
                continue;
            }

            var path = Path.GetFullPath(Path.Combine(tool.InstallLocation, tool.LaunchCommand));
            var icoFile =
                Directory.GetParent(path)
                    ?.GetFiles("*.ico")
                    .FirstOrDefault()
                    ?.FullName
                ?? throw new FileNotFoundException("Failed to determine application icon file.");

            applications.Add(new ApplicationInfo
            {
                InstallLocation = tool.InstallLocation,
                ExePath = Path.Combine(tool.InstallLocation, tool.LaunchCommand),
                ApplicationId = tool.ToolId,
                ChannelId = tool.ChannelId,
                BuildNumber = tool.BuildNumber,
                DisplayVersion = tool.DisplayVersion,
                DisplayName = tool.DisplayName,
                IcoFile = icoFile
            });
        }

        return applications;
    }

    public static List<RecentProject> GetRecentProjects(List<ApplicationInfo> applications)
    {
        var projects = new List<RecentProject>();

        foreach (var application in applications)
        {
            if (!Directory.Exists(application.InstallLocation) || !File.Exists(application.ExePath))
            {
                Console.WriteLine($"Skipping {application.DisplayName} ({application.DisplayVersion}): Install location or exe not found");
                continue;
            }
            // ref. https://www.jetbrains.com/help/idea/directories-used-by-the-ide-to-store-settings-caches-plugins-and-logs.html#config-directory
            // version:like 2023.3
            // convert <number>.<number><whatsoever> to <number>.<number>
            var regex = new Regex(@"\d+\.\d+");
            var match = regex.Match(application.DisplayVersion);
            if (!match.Success)
            {
                Console.WriteLine($"Skipping {application.DisplayName} ({application.DisplayVersion}): Version not found");
                continue;
            }
            var version = match.Value;

            // %APPDATA%\(JetBrains|Google)\<product><version>
            var configDirectoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                application.DisplayName == "Android Studio" ? "Google" : "JetBrains",
                ConvertDisplayNameToProduct(application.DisplayName) + version
            );
            if (!Directory.Exists(configDirectoryPath))
            {
                Console.WriteLine($"Skipping {application.DisplayName} ({application.DisplayVersion}): Config directory not found or invalid: {configDirectoryPath}");
                continue;
            }


            // application.Path + "options/recent(Projects|Solutions).xml"
            var recentProjectsXMLPath = Path.Combine(
                configDirectoryPath,
                "options",
                "recentProjects.xml"
            );
            var recentSolutionsXMLPath = Path.Combine(
                configDirectoryPath,
                "options",
                "recentSolutions.xml"
            );
            String recentProjectsXMLPathFinal =
                File.Exists(recentProjectsXMLPath) ? recentProjectsXMLPath : recentSolutionsXMLPath;
            if (!File.Exists(recentProjectsXMLPathFinal))
            {
                Console.WriteLine($"Skipping {application.DisplayName} ({application.DisplayVersion}): RecentProjects.xml or RecentSolutions.xml not found");
                continue;
            }

            var recentProjectsXML = new XmlDocument();
            recentProjectsXML.Load(recentProjectsXMLPathFinal);
            var entries = recentProjectsXML.SelectNodes(
                "/application/component[@name='RecentProjectsManager']/option[@name='additionalInfo']/map/entry");

            if (entries is null || entries.Count == 0)
            {
                // try again with RiderRecentProjectsManager
                entries = recentProjectsXML.SelectNodes(
                "/application/component[@name='RiderRecentProjectsManager']/option[@name='additionalInfo']/map/entry");
                if (entries is null || entries.Count == 0)
                {
                    Console.WriteLine($"Skipping {application.DisplayName} ({application.DisplayVersion}): No recent projects found");
                    continue;
                }
            }

            foreach (XmlNode entry in entries)
            {
                var entryKey = entry.Attributes?["key"]?.Value;
                if (entryKey is null)
                {
                    continue;
                }

                var name = entryKey.Split("/").Last();
                var metaInfoAttributes = entry.SelectSingleNode("value/RecentProjectMetaInfo")?.Attributes;
                if (metaInfoAttributes?["displayName"] != null)
                {
                    name = metaInfoAttributes["displayName"].Value;
                }
                // replace $USER_HOME$ with the actual user home directory
                var path = entryKey.Replace("$USER_HOME$", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                //convert timestamp to DateTime
                var timestamp =entry.SelectSingleNode("value/RecentProjectMetaInfo/option[@name='projectOpenTimestamp']")?.Attributes?["value"]?.Value;
                if (timestamp is null)
                {
                    continue;
                }
                var lastOpened = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(timestamp)).DateTime;

                projects.Add(new RecentProject
                {
                    Name = name,
                    Path = path,
                    Application = application,
                    LastOpened = lastOpened
                });
            }
        }

        projects.Sort((x, y) => y.LastOpened.CompareTo(x.LastOpened));
        return projects;
    }
}

public class ApplicationInfo
{
    public string InstallLocation { get; init; }
    public string ExePath { get; init; }
    public string BuildNumber { get; init; }
    public string DisplayVersion { get; init; }
    public string DisplayName { get; init; }
    public string ChannelId { get; init; }
    public string ApplicationId { get; init; }
    public string IcoFile { get; init; }
}

public class RecentProject
{
    public string Name { get; init; }
    public string Path { get; init; }
    public ApplicationInfo Application { get; init; }
    public DateTime LastOpened { get; init; }
}

/// <summary>
///
/// </summary>
public class NewOpenItem
{
    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("toolId")]
    public string ToolId { get; set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; }
}

internal class State
{
    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("tools")]
    public List<Tool> Tools { get; set; }
}

internal class Tool
{
    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("toolId")]
    public string ToolId { get; set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("buildNumber")]
    public string BuildNumber { get; set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("installLocation")]
    public string InstallLocation { get; set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("launchCommand")]
    public string LaunchCommand { get; set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("displayVersion")]
    public string DisplayVersion { get; set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; }
}
