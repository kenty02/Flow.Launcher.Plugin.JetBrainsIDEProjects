using System.Collections.Generic;
using System.Windows.Controls;
using Flow.Launcher.Plugin.JetBrainsIDEProjects.Settings;

namespace Flow.Launcher.Plugin.JetBrainsIDEProjects
{
    /// <inheritdoc cref="Flow.Launcher.Plugin.IPlugin" />
    public class JetBrainsIDEProjects : IPlugin, ISettingProvider
    {
        private PluginInitContext _context;
        private Settings.Settings _settings;

        /// <inheritdoc />
        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = context.API.LoadSettingJsonStorage<Settings.Settings>();
        }

        /// <inheritdoc />
        public List<Result> Query(Query query)
        {
            List<RecentProject> projects;
            try {
                var applications = RecentProjectsReader.GetApplications();
                projects = RecentProjectsReader.GetRecentProjects(applications);
            } catch (System.Exception e) {
                return new List<Result>(
                    new Result[] {
                        new Result {
                            Title = "Error reading JetBrains IDE projects",
                            SubTitle = e.Message,
                            Action = _ => false,
                            IcoPath = "icon.png",
                            Score = 0
                        }
                    }
                );
            }

            var results = new List<Result>();

            foreach (var project in projects)
            {
                var stringToSearchIn = project.Name;
                if (_settings.IncludePathInSearch)
                {
                    stringToSearchIn += " " + project.Path;
                }
                
                var score = string.IsNullOrWhiteSpace(query.Search)
                    ? 100
                    : _context.API.FuzzySearch(query.Search, stringToSearchIn).Score;

                if (score > 0)
                {
                    results.Add(new Result
                    {
                        Title = project.Name,
                        SubTitle = project.Path,
                        IcoPath = project.Application?.IcoFile ?? "icon.png",
                        Action = _ =>
                        {
                            _context.API.ShellRun($"\"{project.Path}\"", project.Application.ExePath);
                            return true;
                        },
                        Score = score
                    });
                }
            }

            return results;
        }

        /// <inheritdoc />
        public Control CreateSettingPanel()
        {
            return new SettingsControl(_settings);
        }
    }
}
