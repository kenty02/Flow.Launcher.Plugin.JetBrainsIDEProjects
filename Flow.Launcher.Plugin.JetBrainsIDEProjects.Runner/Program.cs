// See https://aka.ms/new-console-template for more information

using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.JetBrainsIDEProjects;

var stuff = new JetBrainsIDEProjects();
stuff.Init(new PluginInitContext());
var results = stuff.Query(new Query("jb ", "", default, default, default));
// pretty print List
results.ForEach(r => Console.WriteLine("Title: " + r.Title + ", SubTitle: " + r.SubTitle + ", IcoPath: " + r.IcoPath + ", Score: " + r.Score));

