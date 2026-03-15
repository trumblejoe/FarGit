using FarNet;
using LibGit2Sharp;

namespace FarGit.Panels;

/// <summary>
/// Panel showing the commit timeline for the current branch.
/// Most recent commit is at the top.
/// Branch and tag labels appear on the commits they point to.
///
/// Key bindings:
///   Enter / F3  – view what changed in this commit (diff)
/// </summary>
public class CommitPanel : BasePanel
{
	public CommitPanel(CommitExplorer explorer) : base(explorer)
	{
		SortMode = PanelSortMode.Unsorted;

		var co = new SetColumn { Kind = "O", Name = "Author",  Width = 16 };
		var cd = new SetColumn { Kind = "D", Name = "Date",    Width = 12 };
		var cn = new SetColumn { Kind = "N", Name = "Commit" };
		var cz = new SetColumn { Kind = "Z", Name = "Branches / Tags", Width = 30 };

		var plan0 = new PanelPlan { Columns = [co, cd, cn, cz] };
		SetPlan(0, plan0);
		SetView(plan0);

		SetKeyBars([
			new KeyBar(KeyCode.F3, ControlKeyStates.None, "Diff", "View changes introduced by this commit"),
		]);
	}

	protected override string HelpTopic => "history-panel";

	// ── Show diff ─────────────────────────────────────────────────────────

	void ShowCommitDiff()
	{
		if (CurrentFile is not CommitFile f) return;

		using var repo = UseRepository();
		var commit = f.Commit;

		Patch patch;
		if (commit.Parents.Any())
			patch = repo.Diff.Compare<Patch>(commit.Parents.First().Tree, commit.Tree);
		else
			patch = repo.Diff.Compare<Patch>(null, commit.Tree); // root commit

		var content = patch.Content.Replace("\uFEFF", string.Empty);
		if (string.IsNullOrWhiteSpace(content))
		{
			Far.Api.Message("This commit has no file changes (empty diff).", Const.ModuleName);
			return;
		}

		var tmp = Path.ChangeExtension(Path.GetTempFileName(), ".diff");
		File.WriteAllText(tmp, content, System.Text.Encoding.UTF8);

		var viewer = Far.Api.CreateViewer();
		viewer.FileName = tmp;
		viewer.Title = $"{commit.Sha[..7]}  {commit.MessageShort}  by {commit.Author.Name}";
		viewer.DeleteSource = DeleteSource.File;
		viewer.Open();
	}

	// ── Menu / key handling ───────────────────────────────────────────────

	internal override void AddMenu(IMenu menu)
	{
		menu.Add("&View commit diff", (_, _) => ShowCommitDiff());
	}

	public override void UIOpenFile(FarFile file)
	{
		if (file is CommitFile) ShowCommitDiff();
	}

	public override bool UIKeyPressed(KeyInfo key)
	{
		switch (key.VirtualKeyCode)
		{
			case KeyCode.F3 when key.Is():
				ShowCommitDiff();
				return true;
		}
		return base.UIKeyPressed(key);
	}
}
