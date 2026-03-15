using FarNet;
using FarGit.Panels;

namespace FarGit;

[ModuleTool(Name = Const.ModuleName, Options = ModuleToolOptions.Panels, Id = "5f8a3b21-c9d4-4e67-a1f0-8b2e5d0c7f3a")]
public class Tool : ModuleTool
{
	public override void Invoke(object sender, ModuleToolEventArgs e)
	{
		var menu = Far.Api.CreateMenu();
		menu.Title = Const.ModuleName;

		// If an AbcPanel is active, delegate to its own menu additions
		if (Far.Api.Panel is AbcPanel panel)
		{
			panel.AddMenu(menu);
		}
		else
		{
			menu.Add(Const.MenuStatus, (_, _) => OpenStatus());
			menu.Add(Const.MenuStash, (_, _) => OpenStash());
			menu.Add(Const.MenuTags, (_, _) => OpenTags());
			menu.Add(string.Empty).IsSeparator = true;
			menu.Add(Const.MenuCommit, (_, _) => OpenCommit(false));
			menu.Add(Const.MenuAmend, (_, _) => OpenCommit(true));
		}

		menu.Show();
	}

	static void TryOpen(Action open)
	{
		try { open(); }
		catch (Exception ex) { Far.Api.Message(ex.Message, Const.ModuleName, MessageOptions.Warning); }
	}

	static void OpenStatus() => TryOpen(() =>
	{
		var gitDir = Lib.GetGitDir(Far.Api.CurrentDirectory);
		new StatusExplorer(gitDir).CreatePanel().Open();
	});

	static void OpenStash() => TryOpen(() =>
	{
		var gitDir = Lib.GetGitDir(Far.Api.CurrentDirectory);
		new StashExplorer(gitDir).CreatePanel().Open();
	});

	static void OpenTags() => TryOpen(() =>
	{
		var gitDir = Lib.GetGitDir(Far.Api.CurrentDirectory);
		new TagExplorer(gitDir).CreatePanel().Open();
	});

	static void OpenCommit(bool amend) => TryOpen(() =>
	{
		var gitDir = Lib.GetGitDir(Far.Api.CurrentDirectory);
		Commands.Commit.Open(gitDir, amend);
	});
}
