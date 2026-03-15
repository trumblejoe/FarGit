using FarNet;
using LibGit2Sharp;

namespace FarGit.Panels;

/// <summary>
/// Panel showing git working-tree status.
///
/// Key bindings:
///   Space        – toggle stage/unstage for current file
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
			LibGit2Sharp.Commands.Unstage(repo, file.Name);
		else
			LibGit2Sharp.Commands.Stage(repo, file.Name);

		Update(true);
		Redraw();
	}

	void StageAll()
	{
		using var repo = UseRepository();
		LibGit2Sharp.Commands.Stage(repo, "*");
		Update(true);
		Redraw();
	}

	void UnstageAll()
	{
		using var repo = UseRepository();
		LibGit2Sharp.Commands.Unstage(repo, "*");
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
			var changes = repo.Diff.Compare<Patch>([file.Name], true);
			patch = changes.Content;
		}

		OpenPatchViewer(patch, $"Diff: {file.Name}");
	}

	void OpenCommit(bool amend)
	{
		Commands.Commit.Open(GitDir, amend);
	}

	// --- Helpers -----------------------------------------------------------

	static void OpenPatchViewer(string content, string title)
	{
		content = content.Replace("\uFEFF", string.Empty);
		var tmp = Path.ChangeExtension(Path.GetTempFileName(), ".diff");
		File.WriteAllText(tmp, content, System.Text.Encoding.UTF8);

		var viewer = Far.Api.CreateViewer();
		viewer.FileName = tmp;
		viewer.Title = title;
		viewer.DeleteSource = DeleteSource.File;
		viewer.Open();
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
			case KeyCode.Spacebar when key.Is():
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
