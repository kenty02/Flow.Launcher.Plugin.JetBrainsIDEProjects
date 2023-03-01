using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.JetBrainsIDEProjects;

internal static class ToolboxCacheReader
{
    private static readonly string ToolboxDirectoryPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JetBrains", "Toolbox");

    private static readonly string IntelliJProjectsPath =
        Path.Combine(ToolboxDirectoryPath, "cache", "intellij_projects.json");

    // todo make this configurable
    private static readonly string ToolboxScriptsPath =
        Path.Combine(ToolboxDirectoryPath, "scripts");

    public static List<Project> Read()
    {
        if (!File.Exists(IntelliJProjectsPath))
        {
            throw new FileNotFoundException($"File not found: {IntelliJProjectsPath}. Is toolbox installed?");
        }

        var json = File.ReadAllText(IntelliJProjectsPath);
        var projects = JsonSerializer.Deserialize<List<Project>>(json);

        return projects;
    }

    public static ApplicationInfo GetApplicationInfo(string applicationId, string channelId)
    {
        // we must determine the script name first
        // this would be much simpler if we have all applicationId to script name mappings
        var applicationChannelPath = Path.Combine(ToolboxDirectoryPath, "apps", applicationId, channelId);

        string TryFindIcoFile(string applicationDirectoryPath)
        {
            var applicationBinPath = Path.Combine(applicationDirectoryPath, "bin");

            return !Directory.Exists(applicationBinPath) ? null : Directory.GetFiles(applicationBinPath, "*.ico").FirstOrDefault();
        }

        var icoFile = Directory.GetDirectories(applicationChannelPath).Where(x => char.IsDigit(x[^1]))
            .Select(TryFindIcoFile).FirstOrDefault(x => x != null);
        if (icoFile == null)
        {
            throw new FileNotFoundException("Failed to determine application icon file.");
        }

        var scriptName = Path.GetFileNameWithoutExtension(icoFile);
        var scriptCmdPath = Path.Combine(ToolboxScriptsPath, scriptName + ".cmd");

        // now we can determine the application path of correct version
        var scriptCmdContent = File.ReadAllText(scriptCmdPath);
        const string applicationPathRegex = @"\s\S+\.exe\s";
        var applicationPath = System.Text.RegularExpressions.Regex.Match(scriptCmdContent, applicationPathRegex)
            .Value.Trim();
        var applicationDirPath = Path.GetDirectoryName(applicationPath);
        if (applicationDirPath == null)
        {
            throw new DirectoryNotFoundException($"Directory not found: {applicationPath}");
        }

        var applicationInfo = new ApplicationInfo
        {
            Path = scriptCmdPath,
            IcoFile = icoFile
        };


        return applicationInfo;
    }
}

/// <summary>
/// 
/// </summary>
public class Project
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; }

    /// <summary>
    /// null if application is not installed
    /// </summary>
    [JsonPropertyName("defaultOpenItem")]
    public DefaultOpenItem DefaultOpenItem { get; set; }
}

/// <summary>
/// 
/// </summary>
public class DefaultOpenItem
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("application_id")]
    public string ApplicationId { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("channel_id")]
    public string ChannelId { get; set; }
}

internal class ApplicationInfo
{
    [JsonPropertyName("path")] public string Path { get; init; }
    [JsonPropertyName("icoFile")] public string IcoFile { get; init; }
}