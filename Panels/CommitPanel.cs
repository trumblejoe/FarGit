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

		try
		{
			using var repo = UseRepository();
			var commit = repo.Lookup<Commit>(f.Sha);
			if (commit is null)
			{
				Far.Api.Message($"Commit {f.Sha[..7]} not found.", Const.ModuleName, MessageOptions.Warning);
				return;
			}

			Patch patch;
			if (commit.Parents.Any())
				patch = repo.Diff.Compare<Patch>(commit.Parents.First().Tree, commit.Tree);
			else
				patch = repo.Diff.Compare<Patch>(null, commit.Tree); // root commit

			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"commit {commit.Sha}");
			if (!string.IsNullOrEmpty(f.Labels))
				sb.AppendLine($"Labels: {f.Labels}");
			sb.AppendLine($"Author: {commit.Author.Name} <{commit.Author.Email}>");
			sb.AppendLine($"Date:   {commit.Author.When:yyyy-MM-dd HH:mm:ss zzz}");
			sb.AppendLine();

			// Full commit message, indented 4 spaces per git convention
			foreach (var line in commit.Message.TrimEnd().Split('\n'))
				sb.AppendLine($"    {line.TrimEnd()}");

			var diffContent = patch.Content.Replace("\uFEFF", string.Empty);
			if (!string.IsNullOrWhiteSpace(diffContent))
			{
				sb.AppendLine();
				sb.AppendLine(new string('─', 72));
				sb.Append(diffContent);
			}
			else
			{
				sb.AppendLine();
				sb.AppendLine("    (no file changes in this commit)");
			}

			var tmp = Path.ChangeExtension(Path.GetTempFileName(), ".diff");
			File.WriteAllText(tmp, sb.ToString(), System.Text.Encoding.UTF8);

			var viewer = Far.Api.CreateViewer();
			viewer.FileName = tmp;
			viewer.Title = $"{commit.Sha[..7]}  {commit.MessageShort}";
			viewer.DeleteSource = DeleteSource.File;
			viewer.Open();
		}
		catch (Exception ex)
		{
			Far.Api.Message(ex.Message, Const.ModuleName, MessageOptions.Warning);
		}
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
