using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.JetBrainsIDEProjects
{
    /// <inheritdoc />
    public class JetBrainsIDEProjects : IPlugin
    {
        private PluginInitContext _context;

        /// <inheritdoc />
        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public List<Result> Query(Query query)
        {
            var projects = ToolboxCacheReader.Read();
            var results = new List<Result>();
            var applicationInfoCache = new Dictionary<string, ApplicationInfo>();
            foreach (var project in projects)
            {
                var applicationId = project.DefaultOpenItem.ApplicationId;
                var channelId = project.DefaultOpenItem.ChannelId;
                if (!applicationInfoCache.ContainsKey(applicationId))
                {
                    applicationInfoCache[applicationId] = ToolboxCacheReader.GetApplicationInfo(applicationId, channelId);
                }

                var applicationInfo = applicationInfoCache[applicationId];
                var result = new Result
                {
                    Title = project.Name,
                    SubTitle = project.Path,
                    IcoPath = applicationInfo.IcoFile,
                    Action = _ =>
                    {
                        _context.API.ShellRun(applicationInfo.Path + " " + project.Path);
                        return true;
                    },
                    Score = _context.API.FuzzySearch(query.Search, project.Name).Score
                };
                results.Add(result);
            }

            return results;
        }
    }
}