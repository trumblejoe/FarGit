using FarNet;
using LibGit2Sharp;

namespace FarGit.Panels;

/// <summary>
/// Explorer for <see cref="StatusPanel"/>. Provides the working-tree status
/// (staged changes, unstaged changes, and untracked files) as panel entries.
/// </summary>
public class StatusExplorer(string gitDir)
	: BaseExplorer(gitDir, new Guid("b1c2d3e4-f5a6-4b7c-8d9e-0f1a2b3c4d5e"))
{
	public override Panel CreatePanel() => new StatusPanel(this);

	public override IEnumerable<FarFile> GetFiles(GetFilesEventArgs args)
	{
		using var repo = new Repository(GitDir);
		var status = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = true });

		// 1. Staged changes (index vs HEAD)
		foreach (var entry in status)
		{
			var sym = Lib.StagedSymbol(entry.State);
			if (sym != " ")
				yield return new StatusFile(entry.FilePath, Const.CatStaged, sym, entry);
		}

		// 2. Unstaged changes (workdir vs index)
		foreach (var entry in status)
		{
			var sym = Lib.UnstagedSymbol(entry.State);
			if (sym != " ")
				yield return new StatusFile(entry.FilePath, Const.CatUnstaged, sym, entry);
		}

		// 3. Untracked files
		foreach (var entry in status.Untracked)
		{
			yield return new StatusFile(entry.FilePath, Const.CatUntracked, "?", entry);
		}

		// Update panel title when first loaded
		if (args.Panel is StatusPanel panel && panel.Title is null)
		{
			var work = repo.Info.WorkingDirectory;
			var branch = repo.Head.FriendlyName;
			panel.Title = $"Git Status [{branch}] {work}";
			panel.CurrentLocation = work;
		}
	}
}
