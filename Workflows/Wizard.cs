using FarNet;
using LibGit2Sharp;

namespace FarGit.Workflows;

/// <summary>
/// Guided step-by-step workflows for git newcomers.
/// Each workflow explains what git is doing at every step in plain English.
/// </summary>
static class Wizard
{
	// ── Entry point ──────────────────────────────────────────────────────────

	public static void Show(string gitDir, Panels.DashboardPanel panel)
	{
		var menu = Far.Api.CreateMenu();
		menu.Title = "Git Guide  —  What do you want to do?";
		menu.Add("💾  Save my changes  (stage + commit)");
		menu.Add("↩  Undo changes to a file");
		menu.Add("📦  Set work aside temporarily  (stash)");
		menu.Add("🔖  Mark a release version  (tag)");
		menu.Add("🔍  See what has changed  (status)");

		if (!menu.Show()) return;

		switch (menu.Selected)
		{
			case 0: SaveChanges(gitDir, panel); break;
			case 1: UndoChanges(gitDir, panel); break;
			case 2: StashWork(gitDir, panel); break;
			case 3: MarkVersion(gitDir, panel); break;
			case 4: ReviewStatus(gitDir, panel); break;
		}
	}

	// ── Helper ───────────────────────────────────────────────────────────────

	/// <summary>
	/// Shows a multi-line info/step message with Continue / Cancel buttons.
	/// Returns false if the user pressed Cancel or Escape.
	/// </summary>
	static bool Step(string body, string title = "Git Guide")
		=> 0 == Far.Api.Message(body, title, (MessageOptions)0, ["Continue", "Cancel"]);

	// ── Workflow 1: Save My Changes ──────────────────────────────────────────

	static void SaveChanges(string gitDir, Panels.DashboardPanel panel)
	{
		if (!Step(
			"SAVE MY CHANGES\n\n" +
			"Git saves work in two steps:\n\n" +
			"  1. STAGE  — choose which changed files to include\n" +
			"  2. COMMIT — seal them with a short description\n\n" +
			"Think of staging like putting items into a box\n" +
			"before you seal and label it.\n\n" +
			"Press Enter to continue, Esc to cancel."))
			return;

		int stagedCount, changedCount;
		using (var repo = new Repository(gitDir))
		{
			var status = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = true });
			stagedCount  = status.Count(e => Lib.StagedSymbol(e.State)   != " ");
			changedCount = status.Count(e => Lib.UnstagedSymbol(e.State) != " ")
			             + status.Untracked.Count();
		}

		if (stagedCount == 0 && changedCount == 0)
		{
			Step("Your working tree is already clean — nothing to save!\n\n" +
			     "All your changes have already been committed.");
			return;
		}

		if (stagedCount == 0)
		{
			// Nothing staged — guide user through staging
			if (!Step(
				$"STEP 1 of 2: STAGE\n\n" +
				$"You have {changedCount} changed file(s) that are not yet staged.\n\n" +
				$"Staging means: tell git 'include these changes in my next save'.\n\n" +
				$"Press Enter to stage ALL changed files now.\n\n" +
				$"Tip: If you only want to save some files, press Esc here,\n" +
				$"open Status (Enter on the Dashboard), select files with\n" +
				$"the Space bar, then press F5 to stage just those files."))
				return;

			using var repo = new Repository(gitDir);
			LibGit2Sharp.Commands.Stage(repo, "*");
			stagedCount = changedCount;
		}
		else
		{
			Step($"STEP 1 of 2: STAGE\n\n" +
			     $"You already have {stagedCount} file(s) staged and ready to commit.\n\n" +
			     $"Skipping staging — moving on to commit.");
		}

		// Step 2: Commit
		if (!Step(
			"STEP 2 of 2: COMMIT\n\n" +
			$"You are about to commit {stagedCount} file(s).\n\n" +
			"Next you will write a short commit message. Good examples:\n\n" +
			"  • Fix login button on mobile\n" +
			"  • Add dark mode support\n" +
			"  • Update README with install instructions\n\n" +
			"Keep it short but meaningful — future-you will thank you!\n\n" +
			"Press Enter to open the commit editor."))
			return;

		Commands.Commit.Open(gitDir, false);
		panel.Update(true);
		panel.Redraw();
	}

	// ── Workflow 2: Undo Changes ─────────────────────────────────────────────

	static void UndoChanges(string gitDir, Panels.DashboardPanel panel)
	{
		if (!Step(
			"UNDO CHANGES TO A FILE\n\n" +
			"Git can restore any file to the state it was in\n" +
			"at your last commit. This is called a 'revert'.\n\n" +
			"⚠ WARNING: This permanently discards any changes\n" +
			"you have made since your last commit. There is no\n" +
			"undo for this operation.\n\n" +
			"If you want to keep your changes but just set them\n" +
			"aside temporarily, use 'Set work aside (stash)' instead.\n\n" +
			"Press Enter to open the Status panel."))
			return;

		if (!Step(
			"HOW TO UNDO A FILE\n\n" +
			"In the Status panel that is about to open:\n\n" +
			"  1. Use the arrow keys to highlight the file you want to undo\n" +
			"  2. Press F8  (labeled 'Revert!' at the bottom of the screen)\n" +
			"  3. Confirm when prompted\n\n" +
			"To undo several files at once:\n" +
			"  • Press Space on each file to mark it (a bullet appears)\n" +
			"  • Then press F8 to revert all marked files\n\n" +
			"Press Enter to open Status now."))
			return;

		new Panels.StatusExplorer(gitDir).CreatePanel().OpenChild(panel);
	}

	// ── Workflow 3: Stash Work ───────────────────────────────────────────────

	static void StashWork(string gitDir, Panels.DashboardPanel panel)
	{
		if (!Step(
			"SET WORK ASIDE TEMPORARILY  (Stash)\n\n" +
			"A stash is like a clipboard for your work-in-progress.\n\n" +
			"Use it when you need to:\n" +
			"  • Switch tasks without losing what you're working on\n" +
			"  • Pull updates from a colleague without conflicts\n" +
			"  • Try something else and come back later\n\n" +
			"Your changes are saved privately and your files go back\n" +
			"to the last commit state. You can restore them any time.\n\n" +
			"Press Enter to create a stash now, Esc to cancel."))
			return;

		int changedCount;
		using (var repo = new Repository(gitDir))
		{
			var status = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = true });
			changedCount = status.Count(e => Lib.StagedSymbol(e.State)   != " ")
			             + status.Count(e => Lib.UnstagedSymbol(e.State) != " ")
			             + status.Untracked.Count();
		}

		if (changedCount == 0)
		{
			Step("Your working tree is clean — there is nothing to stash!\n\n" +
			     "Make some changes first, then come back here.");
			return;
		}

		var description = Far.Api.Input(
			$"Give this stash a short description (optional):\n(e.g. 'WIP: login refactor')",
			"FarGit-stash",
			"Create Stash");

		if (description is null) return; // user pressed Esc

		if (string.IsNullOrWhiteSpace(description))
			description = "WIP";

		try
		{
			using var repo = new Repository(gitDir);
			var sig = Lib.BuildSignature(repo);
			repo.Stashes.Add(sig, description,
				StashModifiers.IncludeUntracked | StashModifiers.KeepIndex);

			Step($"✓ Stash created: \"{description}\"\n\n" +
			     $"Your {changedCount} changed file(s) are safely stored.\n" +
			     $"Your working directory is now clean.\n\n" +
			     $"To restore your work later:\n" +
			     $"  • Open the Stash panel from the Dashboard\n" +
			     $"  • Highlight your stash and press F5 (Apply) or F6 (Pop)");

			panel.Update(true);
			panel.Redraw();
		}
		catch (Exception ex)
		{
			Far.Api.Message(ex.Message, Const.ModuleName, MessageOptions.Warning);
		}
	}

	// ── Workflow 4: Mark a Version ───────────────────────────────────────────

	static void MarkVersion(string gitDir, Panels.DashboardPanel panel)
	{
		if (!Step(
			"MARK A RELEASE VERSION  (Tag)\n\n" +
			"A tag is a named bookmark on a specific commit.\n" +
			"Use tags to mark important moments like releases.\n\n" +
			"Common conventions:\n" +
			"  • v1.0   — version 1.0 release\n" +
			"  • v2.3.1 — patch release\n" +
			"  • beta-1 — pre-release\n\n" +
			"The tag will point to your most recent commit (HEAD).\n\n" +
			"Press Enter to create a tag, Esc to cancel."))
			return;

		string tipSha, tipMsg;
		using (var repo = new Repository(gitDir))
		{
			if (repo.Head.Tip is null)
			{
				Step("There are no commits yet — make your first commit before tagging.");
				return;
			}
			tipSha = repo.Head.Tip.Sha[..7];
			tipMsg = repo.Head.Tip.MessageShort;
		}

		var tagName = Far.Api.Input(
			$"Tag name for commit {tipSha} \"{tipMsg}\":\n(e.g. v1.0)",
			"FarGit-tag",
			"Create Tag");

		if (string.IsNullOrWhiteSpace(tagName)) return;
		tagName = tagName.Trim();

		try
		{
			using var repo = new Repository(gitDir);
			repo.ApplyTag(tagName);

			Step($"✓ Tag '{tagName}' created on commit {tipSha}\n\n" +
			     $"\"{tipMsg}\"\n\n" +
			     $"You can view and manage all tags from the Dashboard\n" +
			     $"by pressing Enter on the Tags row.");

			panel.Update(true);
			panel.Redraw();
		}
		catch (Exception ex)
		{
			Far.Api.Message(ex.Message, Const.ModuleName, MessageOptions.Warning);
		}
	}

	// ── Workflow 5: Review Status ────────────────────────────────────────────

	static void ReviewStatus(string gitDir, Panels.DashboardPanel panel)
	{
		int stagedCount, unstagedCount, untrackedCount;
		string branch;
		using (var repo = new Repository(gitDir))
		{
			branch = repo.Head.FriendlyName;
			var status = repo.RetrieveStatus(new StatusOptions { IncludeUntracked = true });
			stagedCount   = status.Count(e => Lib.StagedSymbol(e.State)   != " ");
			unstagedCount = status.Count(e => Lib.UnstagedSymbol(e.State) != " ");
			untrackedCount = status.Untracked.Count();
		}

		var summary = new System.Text.StringBuilder();
		summary.AppendLine("WHAT HAS CHANGED?\n");
		summary.AppendLine($"Branch: {branch}\n");

		if (stagedCount == 0 && unstagedCount == 0 && untrackedCount == 0)
		{
			summary.AppendLine("Everything is clean — no pending changes.");
		}
		else
		{
			if (stagedCount > 0)
				summary.AppendLine($"  Staged:    {stagedCount} file(s)  ← ready to commit");
			if (unstagedCount > 0)
				summary.AppendLine($"  Unstaged:  {unstagedCount} file(s)  ← changed, not yet staged");
			if (untrackedCount > 0)
				summary.AppendLine($"  Untracked: {untrackedCount} file(s)  ← new, not yet tracked");

			summary.AppendLine();
			if (stagedCount > 0)
				summary.AppendLine("You have staged changes ready to commit  (press F7).");
			else
				summary.AppendLine("Nothing staged yet. Use 'Save my changes' to stage and commit.");
		}

		summary.Append("\nPress Enter to open the Status panel for details.");

		if (!Step(summary.ToString())) return;

		if (!Step(
			"READING THE STATUS PANEL\n\n" +
			"Files are grouped into three sections:\n\n" +
			"  Staged    — changes included in your next commit\n" +
			"  Unstaged  — changes NOT yet included\n" +
			"  Untracked — brand-new files git doesn't know about yet\n\n" +
			"Useful keys in the Status panel:\n" +
			"  F3  — see exactly what changed in a file (diff)\n" +
			"  F4  — open the file in the editor\n" +
			"  F5  — stage the highlighted file\n" +
			"  F7  — commit staged changes\n" +
			"  F8  — revert (undo) changes — cannot be undone!\n\n" +
			"Press Enter to open Status now."))
			return;

		new Panels.StatusExplorer(gitDir).CreatePanel().OpenChild(panel);
	}
}
