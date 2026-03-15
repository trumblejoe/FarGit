using FarNet;

namespace FarGit.Panels;

/// <summary>
/// The main FarGit entry panel.
/// Shows a repo-at-a-glance: branch, staged/unstaged/untracked counts, stash, tags.
///
/// Key bindings:
///   Enter        – open the relevant sub-panel (status, stash, tags)
///   F5           – stage all changes
///   F7           – open commit dialog
///   ShiftF7      – amend last commit
/// </summary>
public class DashboardPanel : BasePanel
{
	public DashboardPanel(DashboardExplorer explorer) : base(explorer)
	{
		SortMode = PanelSortMode.Unsorted;

		// Columns: section name | value | description
		var cn = new SetColumn { Kind = "N", Name = "Section", Width = 10 };
		var co = new SetColumn { Kind = "O", Name = "Value", Width = 22 };
		var cz = new SetColumn { Kind = "Z", Name = "Details" };

		var plan0 = new PanelPlan { Columns = [cn, co, cz] };
		SetPlan(0, plan0);
		SetView(plan0);

		SetKeyBars([
			new KeyBar(KeyCode.F3, ControlKeyStates.None, "", ""),
			new KeyBar(KeyCode.F4, ControlKeyStates.None, "", ""),
			new KeyBar(KeyCode.F5, ControlKeyStates.None, "StageAll", "Stage all changes"),
			new KeyBar(KeyCode.F6, ControlKeyStates.None, "", ""),
			new KeyBar(KeyCode.F7, ControlKeyStates.None, "Commit", "Commit staged changes"),
			new KeyBar(KeyCode.F8, ControlKeyStates.None, "", ""),
			new KeyBar(KeyCode.F7, ControlKeyStates.ShiftPressed, "Amend", "Amend last commit"),
		]);
	}

	protected override string HelpTopic => "dashboard";

	// --- Navigation --------------------------------------------------------

	void OpenStatus() => new StatusExplorer(GitDir).CreatePanel().OpenChild(this);
	void OpenStash() => new StashExplorer(GitDir).CreatePanel().OpenChild(this);
	void OpenTags() => new TagExplorer(GitDir).CreatePanel().OpenChild(this);
	void OpenCommit(bool amend) => Commands.Commit.Open(GitDir, amend);

	// --- Actions -----------------------------------------------------------

	void StageAll()
	{
		using var repo = UseRepository();
		LibGit2Sharp.Commands.Stage(repo, "*");
		Update(true);
		Redraw();
	}

	// --- Menu / key handling -----------------------------------------------

	internal override void AddMenu(IMenu menu)
	{
		menu.Add(Const.MenuStatus, (_, _) => OpenStatus());
		menu.Add(Const.MenuStash, (_, _) => OpenStash());
		menu.Add(Const.MenuTags, (_, _) => OpenTags());
		menu.Add(string.Empty).IsSeparator = true;
		menu.Add(Const.MenuCommit, (_, _) => OpenCommit(false));
		menu.Add(Const.MenuAmend, (_, _) => OpenCommit(true));
		menu.Add(string.Empty).IsSeparator = true;
		menu.Add(Const.StageAll, (_, _) => StageAll());
	}

	public override void UIOpenFile(FarFile file)
	{
		if (file is not DashboardFile df) return;

		switch (df.Section)
		{
			case DashboardSection.Staged:
			case DashboardSection.Unstaged:
			case DashboardSection.Untracked:
				OpenStatus();
				break;

			case DashboardSection.Stash:
				OpenStash();
				break;

			case DashboardSection.Tags:
				OpenTags();
				break;

			case DashboardSection.Branch:
				OpenStatus();
				break;
		}
	}

	public override bool UIKeyPressed(KeyInfo key)
	{
		switch (key.VirtualKeyCode)
		{
			case KeyCode.F5 when key.Is():
				StageAll();
				return true;

			case KeyCode.F6 when key.Is():
			case KeyCode.F8 when key.Is():
				return true; // suppress default copy/move/delete — they don't apply here

			case KeyCode.F7 when key.Is():
				OpenCommit(false);
				return true;

			case KeyCode.F7 when key.IsShift():
				OpenCommit(true);
				return true;
		}

		return base.UIKeyPressed(key);
	}
}
