using FarNet;
using FarGit.Commands;
using LibGit2Sharp;

namespace FarGit.Panels;

/// <summary>
/// Panel showing git working-tree status.
///
/// Key bindings:
///   Space / Ins  – toggle stage/unstage for current file
///   F4           – open file in editor
///   Enter        – view diff patch for current file
///   ShiftF2      – open commit dialog
///   ShiftF7      – amend last commit
/// </summary>
public class StatusPanel : BasePanel
{
	public StatusPanel(StatusExplorer explorer) : base(explorer)
	{
		SortMode = PanelSortMode.Unsorted;

		// Columns: symbol | category | path
		var co = new SetColumn { Kind = "O", Name = " ", Width = 2 };
		var cd = new SetColumn { Kind = "Z", Name = "Status", Width = 10 };
		var cn = new SetColumn { Kind = "N", Name = "File" };

		var plan0 = new PanelPlan { Columns = [co, cd, cn] };
		SetPlan(0, plan0);
		SetView(plan0);
	}

	protected override string HelpTopic => "status-panel";

	// --- Actions -----------------------------------------------------------

	void ToggleStage()
	{
		var file = CurrentFile as StatusFile;
		if (file is null) return;

		using var repo = UseRepository();
		if (file.IsStaged)
			Commands.Unstage(repo, file.Name);
		else
			Commands.Stage(repo, file.Name);

		Update(true);
		Redraw();
	}

	void StageAll()
	{
		using var repo = UseRepository();
		Commands.Stage(repo, "*");
		Update(true);
		Redraw();
	}

	void UnstageAll()
	{
		using var repo = UseRepository();
		Commands.Unstage(repo, "*");
		Update(true);
		Redraw();
	}

	void EditFile()
	{
		var file = CurrentFile as StatusFile;
		if (file is null) return;

		using var repo = UseRepository();
		var work = repo.Info.WorkingDirectory;
		if (work is null) return;

		var editor = Far.Api.CreateEditor();
		editor.FileName = Path.Join(work, file.Name);
		editor.Open();
	}

	void ShowDiff()
	{
		var file = CurrentFile as StatusFile;
		if (file is null) return;

		using var repo = UseRepository();

		string patch;
		if (file.IsStaged)
		{
			// diff HEAD..index for this file
			var changes = repo.Diff.Compare<Patch>([file.Name], false);
			patch = changes.Content;
		}
		else if (file.IsUntracked)
		{
			Far.Api.Message("No diff available for untracked files.", Const.ModuleName);
			return;
		}
		else
		{
			// diff index..workdir for this file
			var changes = repo.Diff.Compare<Patch>([file.Name], true);
			patch = changes.Content;
		}

		patch = patch.Replace("\uFEFF", string.Empty);

		var viewer = Far.Api.CreateViewer();
		viewer.Text = patch;
		viewer.Title = $"Diff: {file.Name}";
		viewer.Open();
	}

	void OpenCommit(bool amend)
	{
		Commands.Commit.Open(GitDir, amend);
	}

	// --- Menu / key handling -----------------------------------------------

	internal override void AddMenu(IMenu menu)
	{
		menu.Add(Const.StageFile, (_, _) => ToggleStage());
		menu.Add(Const.StageAll, (_, _) => StageAll());
		menu.Add(Const.UnstageAll, (_, _) => UnstageAll());
		menu.Add(Const.EditFile, (_, _) => EditFile());
		menu.Add(Const.DiffFile, (_, _) => ShowDiff());
		menu.Add(Const.MenuCommit, (_, _) => OpenCommit(false));
		menu.Add(Const.MenuAmend, (_, _) => OpenCommit(true));
	}

	public override void UIOpenFile(FarFile file)
	{
		ShowDiff();
	}

	public override bool UIKeyPressed(KeyInfo key)
	{
		switch (key.VirtualKeyCode)
		{
			case KeyCode.Space when key.Is():
				ToggleStage();
				return true;

			case KeyCode.F4 when key.Is():
				EditFile();
				return true;

			case KeyCode.F2 when key.IsShift():
				OpenCommit(false);
				return true;

			case KeyCode.F7 when key.IsShift():
				OpenCommit(true);
				return true;
		}

		return base.UIKeyPressed(key);
	}
}
