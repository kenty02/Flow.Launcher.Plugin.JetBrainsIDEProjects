using System;
using System.Collections.Generic;
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

    private static readonly string FallbackIconPath = Path.Combine("app.png");

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
        var applicationChannelPath = Path.Combine(ToolboxDirectoryPath, "apps", applicationId, channelId);
        // find folder whose name ends with digit
        var applicationDirectoryPath = Directory.GetDirectories(applicationChannelPath)
            .FirstOrDefault(x => char.IsDigit(x[^1]));
        if (applicationDirectoryPath == null)
        {
            throw new DirectoryNotFoundException($"Directory not found: {applicationChannelPath}");
        }
        var applicationBinPath = Path.Combine(applicationDirectoryPath, "bin");

        var icoFile = Directory.GetFiles(applicationBinPath, "*.ico").FirstOrDefault();
        if (icoFile == null)
        {
            throw new FileNotFoundException(
                $"ico file not found for {applicationId}: {applicationBinPath}");
        }

        // rewrite icoFile extension to bat
        var applicationBatPath = Path.ChangeExtension(icoFile, ".bat");

        var applicationInfo = new ApplicationInfo
        {
            Path = applicationBatPath,
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
    /// 
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