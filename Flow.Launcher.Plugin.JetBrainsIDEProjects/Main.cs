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
            var applications = ToolboxCacheReader.GetApplications();
            var projects = ToolboxCacheReader.GetProjects(applications);

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
                            _context.API.ShellRun(project.Path, project.Application.Path);
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
