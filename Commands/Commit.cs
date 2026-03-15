using FarNet;
using LibGit2Sharp;
using System.Text;

namespace FarGit.Commands;

/// <summary>
/// Opens the FAR editor with a COMMIT_EDITMSG template.
/// When the editor closes the file is committed (unless the message is empty).
/// </summary>
static class Commit
{
	const char CommentChar = '#';

	public static void Open(string gitDir, bool amend)
	{
		using var repo = new Repository(gitDir);
		var commitFile = Path.Join(repo.Info.Path, "COMMIT_EDITMSG");

		File.WriteAllText(commitFile, BuildTemplate(repo, amend), Encoding.UTF8);

		var editor = Far.Api.CreateEditor();
		editor.FileName = commitFile;
		editor.CodePage = 65001;
		editor.DisableHistory = true;
		editor.Caret = new Point(0, 0);
		editor.Title = (amend ? "Amend commit" : "Commit")
			+ $" on branch {repo.Head.FriendlyName} — empty message aborts";

		editor.Closed += (_, _) => CommitOnClose(gitDir, commitFile, amend);
		editor.Open();
	}

	static string BuildTemplate(Repository repo, bool amend)
	{
		var sb = new StringBuilder();

		if (amend && repo.Head.Tip is { } tip)
		{
			sb.AppendLine(tip.Message.TrimEnd());
			sb.AppendLine();
		}

		sb.AppendLine($"{CommentChar} Changes to be committed:");

		var staged = repo.Diff.Compare<TreeChanges>(repo.Head.Tip?.Tree, DiffTargets.Index);
		foreach (var c in staged)
			sb.AppendLine($"{CommentChar}   {c.Status,-16} {c.Path}");

		if (!staged.Any())
			sb.AppendLine($"{CommentChar}   (nothing staged)");

		sb.AppendLine($"{CommentChar}");
		sb.AppendLine($"{CommentChar} Changes not staged:");
		var unstaged = repo.Diff.Compare<TreeChanges>();
		foreach (var c in unstaged)
			sb.AppendLine($"{CommentChar}   {c.Status,-16} {c.Path}");

		if (!unstaged.Any())
			sb.AppendLine($"{CommentChar}   (clean)");

		return sb.ToString();
	}

	static void CommitOnClose(string gitDir, string commitFile, bool amend)
	{
		if (!File.Exists(commitFile))
			return;

		var raw = File.ReadAllText(commitFile, Encoding.UTF8);
		File.Delete(commitFile);

		// Strip comment lines and trim
		var message = string.Join('\n',
			raw.Split('\n').Where(l => !l.StartsWith(CommentChar))).Trim();

		if (string.IsNullOrWhiteSpace(message))
		{
			Far.Api.UI.WriteLine("Aborting commit due to empty commit message.");
			return;
		}

		try
		{
			using var repo = new Repository(gitDir);
			var sig = Lib.BuildSignature(repo);
			var options = new CommitOptions { AmendPreviousCommit = amend };
			repo.Commit(message, sig, sig, options);

			// Refresh any open FarGit panels
			UpdatePanels();
		}
		catch (Exception ex)
		{
			Far.Api.Message(ex.Message, Const.ModuleName, MessageOptions.Warning);
		}
	}

	static void UpdatePanels()
	{
		RefreshPanel(Far.Api.Panel);
		RefreshPanel(Far.Api.Panel2);
	}

	static void RefreshPanel(IPanel? panel)
	{
		if (panel is Panels.AbcPanel)
		{
			panel.Update(true);
			panel.Redraw();
		}
	}
}
