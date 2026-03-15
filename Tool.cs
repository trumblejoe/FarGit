using FarNet;
using FarGit.Panels;

namespace FarGit;

[ModuleTool(Name = Const.ModuleName, Options = ModuleToolOptions.Panels, Id = "5f8a3b21-c9d4-4e67-a1f0-8b2e5d0c7f3a")]
public class Tool : ModuleTool
{
	public override void Invoke(object sender, ModuleToolEventArgs e)
	{
		// If a FarGit panel is already open, show its context menu
		if (Far.Api.Panel is AbcPanel panel)
		{
			var menu = Far.Api.CreateMenu();
			menu.Title = Const.ModuleName;
			panel.AddMenu(menu);
			menu.Show();
			return;
		}

		// Otherwise open the Dashboard directly — no extra menu step
		OpenDashboard();
	}

	static void TryOpen(Action open)
	{
		try { open(); }
		catch (Exception ex) { Far.Api.Message(ex.Message, Const.ModuleName, MessageOptions.Warning); }
	}

	static void OpenDashboard() => TryOpen(() =>
	{
		var gitDir = Lib.GetGitDir(Far.Api.CurrentDirectory);
		new DashboardExplorer(gitDir).CreatePanel().Open();
	});
}
