using FarNet;
using LibGit2Sharp;

namespace FarGit.Panels;

/// <summary>
/// Explorer for <see cref="DashboardPanel"/>.
/// Shows a repo-at-a-glance summary: branch, staged/unstaged/untracked counts, stash, tags.
/// </summary>
public class DashboardExplorer(string gitDir)
	: BaseExplorer(gitDir, new Guid("e4f5a6b7-c8d9-4e0f-1a2b-3c4d5e6f7a8b"))
{
	public override Panel CreatePanel() => new DashboardPanel(this);

	public override IEnumerable<FarFile> GetFiles(GetFilesEventArgs args)
	{
		using var repo = new Repository(GitDir);

		// --- Branch row ---
		var head = repo.Head;
		var tip = head.Tip;
		var branchLabel = head.FriendlyName;

		if (head.IsTracking && head.TrackingDetails is { } td)
		{
			if (td.AheadBy > 0) branchLabel += $"  +{td.AheadBy} to push";
			if (td.BehindBy > 0) branchLabel += $"  -{td.BehindBy} to pull";
		}

		var tipInfo = tip is not null
			? $"{tip.Sha[..7]}  {tip.MessageShort}"
			: "(no commits yet)";

		yield return new DashboardFile("Branch", branchLabel, tipInfo, DashboardSection.Branch);

		// --- Working tree status ---
		if (!repo.Info.IsBare)
		{
			var status = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = true });

			var stagedCount = status.Count(e => Lib.StagedSymbol(e.State) != " ");
			var unstagedCount = status.Count(e => Lib.UnstagedSymbol(e.State) != " ");
			var untrackedCount = status.Untracked.Count();

			yield return new DashboardFile(
				"Staged",
				stagedCount > 0 ? $"{stagedCount} file{(stagedCount == 1 ? "" : "s")}" : "nothing",
				stagedCount > 0 ? "ready to commit  (F7)" : "nothing staged — use F5 to stage all",
				DashboardSection.Staged);

			yield return new DashboardFile(
				"Unstaged",
				unstagedCount > 0 ? $"{unstagedCount} file{(unstagedCount == 1 ? "" : "s")}" : "nothing",
				unstagedCount > 0 ? "modified, not yet staged  (Enter to review)" : "working tree clean",
				DashboardSection.Unstaged);

			yield return new DashboardFile(
				"Untracked",
				untrackedCount > 0 ? $"{untrackedCount} file{(untrackedCount == 1 ? "" : "s")}" : "none",
				untrackedCount > 0 ? "new files not yet tracked  (Enter to review)" : "",
				DashboardSection.Untracked);
		}

		// --- Stash row ---
		var stashCount = repo.Stashes.Count();
		yield return new DashboardFile(
			"Stash",
			stashCount > 0 ? $"{stashCount} saved" : "empty",
			stashCount > 0 ? "temporarily saved states  (Enter to manage)" : "no stashes",
			DashboardSection.Stash);

		// --- Tags row ---
		var tagCount = repo.Tags.Count();
		yield return new DashboardFile(
			"Tags",
			tagCount > 0 ? $"{tagCount} tag{(tagCount == 1 ? "" : "s")}" : "none",
			tagCount > 0 ? "Enter to manage" : "no tags",
			DashboardSection.Tags);

		// Update panel title
		if (args.Panel is DashboardPanel panel && panel.Title is null)
		{
			var repoName = Path.GetFileName(
				Path.TrimEndingDirectorySeparator(repo.Info.WorkingDirectory ?? GitDir));
			panel.Title = $"FarGit  ·  {repoName}";
			panel.CurrentLocation = repo.Info.WorkingDirectory ?? GitDir;
		}
	}
}
