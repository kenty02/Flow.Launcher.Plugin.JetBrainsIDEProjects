using System.Collections.Generic;
using System.Diagnostics;

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
                var score = string.IsNullOrWhiteSpace(query.Search)
                    ? 100
                    : _context.API.FuzzySearch(query.Search, project.Name).Score;

                if (score > 0)
                {
                    results.Add(new Result
                    {
                        Title = project.Name,
                        SubTitle = project.Path,
                        IcoPath = project.Application?.IcoFile ?? "icon.png",
                        Action = _ =>
                        {
                            _context.API.ShellRun(project.Path, project.Application.ExePath);
                            return true;
                        },
                        Score = score
                    });
                }
            }

            return results;
        }
    }
}
