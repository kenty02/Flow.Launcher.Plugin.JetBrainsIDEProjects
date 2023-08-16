// See https://aka.ms/new-console-template for more information

using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.JetBrainsIDEProjects;

var stuff = new JetBrainsIDEProjects();
stuff.Init(new PluginInitContext());
stuff.Query(new Query("", default, default, default, default));