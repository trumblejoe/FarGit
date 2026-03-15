using FarNet;
using LibGit2Sharp;

namespace FarGit.Panels;

/// <summary>
/// Panel for managing git remotes and syncing with them.
///
/// Key bindings:
///   F5  – Fetch (download changes without merging)
///   F6  – Pull  (download + merge into current branch)
///   F7  – Push  (upload local commits to remote)
///   F8  – Remove the selected remote
///   Ins – Add a new remote
/// </summary>
public class RemotePanel : BasePanel
{
	public RemotePanel(RemoteExplorer explorer) : base(explorer)
	{
		SortMode = PanelSortMode.Unsorted;

		var cn = new SetColumn { Kind = "N", Name = "Remote", Width = 12 };
		var co = new SetColumn { Kind = "O", Name = "URL" };

		var plan0 = new PanelPlan { Columns = [cn, co] };
		SetPlan(0, plan0);
		SetView(plan0);

		SetKeyBars([
			new KeyBar(KeyCode.F5, ControlKeyStates.None, "Fetch", "Download changes (no merge)"),
			new KeyBar(KeyCode.F6, ControlKeyStates.None, "Pull",  "Download + merge into current branch"),
			new KeyBar(KeyCode.F7, ControlKeyStates.None, "Push",  "Upload local commits"),
			new KeyBar(KeyCode.F8, ControlKeyStates.None, "Remove","Remove this remote"),
		]);
	}

	protected override string HelpTopic => "remote-panel";

	RemoteFile? CurrentRemote => CurrentFile as RemoteFile;

	// ── Fetch ─────────────────────────────────────────────────────────────

	void Fetch()
	{
		var f = CurrentRemote;
		if (f is null) return;

		try
		{
			Commands.RemoteOps.Fetch(GitDir, f.RemoteName);
			Update(true); Redraw();
			Far.Api.Message($"Fetch from '{f.RemoteName}' complete.", Const.ModuleName);
		}
		catch (Exception ex)
		{
			Far.Api.Message(ex.Message, Const.ModuleName, MessageOptions.Warning);
		}
	}

	// ── Pull ──────────────────────────────────────────────────────────────

	void Pull()
	{
		try
		{
			var result = Commands.RemoteOps.Pull(GitDir);

			switch (result.Status)
			{
				case MergeStatus.FastForward:
					Far.Api.Message($"Pull complete — fast-forwarded to {result.Commit?.Sha[..7]}.", Const.ModuleName);
					break;
				case MergeStatus.NonFastForward:
					Far.Api.Message("Pull complete — merge commit created.", Const.ModuleName);
					break;
				case MergeStatus.Conflicts:
					Far.Api.Message(
						"Pull complete but there are merge conflicts.\n\n" +
						"Open the Status panel to see conflicted files.\n" +
						"Edit them, resolve conflict markers, stage, then commit.",
						Const.ModuleName, MessageOptions.Warning);
					break;
				case MergeStatus.UpToDate:
					Far.Api.Message("Already up to date — nothing new to pull.", Const.ModuleName);
					break;
			}

			Update(true); Redraw();
		}
		catch (Exception ex)
		{
			Far.Api.Message(ex.Message, Const.ModuleName, MessageOptions.Warning);
		}
	}

	// ── Push ──────────────────────────────────────────────────────────────

	void PushToRemote()
	{
		var f = CurrentRemote;
		try
		{
			Commands.RemoteOps.Push(GitDir, f?.RemoteName);
			Far.Api.Message("Push complete.", Const.ModuleName);
			Update(true); Redraw();
		}
		catch (Exception ex)
		{
			Far.Api.Message(ex.Message, Const.ModuleName, MessageOptions.Warning);
		}
	}

	// ── Add Remote ────────────────────────────────────────────────────────

	void AddRemote()
	{
		var name = Far.Api.Input("Remote name (e.g. 'origin'):", "FarGit-remote-name", "Add Remote");
		if (string.IsNullOrWhiteSpace(name)) return;

		var url = Far.Api.Input($"URL for '{name}':\n(e.g. https://github.com/user/repo.git)", "FarGit-remote-url", "Add Remote");
		if (string.IsNullOrWhiteSpace(url)) return;

		try
		{
			using var repo = UseRepository();
			repo.Network.Remotes.Add(name.Trim(), url.Trim());
			Update(true); Redraw();
		}
		catch (Exception ex)
		{
			Far.Api.Message(ex.Message, Const.ModuleName, MessageOptions.Warning);
		}
	}

	// ── Remove Remote ─────────────────────────────────────────────────────

	void RemoveRemote()
	{
		var f = CurrentRemote;
		if (f is null) return;

		if (0 != Far.Api.Message(
			$"Remove remote '{f.RemoteName}' ({f.RemoteUrl})?\n\n" +
			"This removes the connection but does not delete any local commits.",
			Const.ModuleName, MessageOptions.YesNo))
			return;

		try
		{
			using var repo = UseRepository();
			repo.Network.Remotes.Remove(f.RemoteName);
			Update(true); Redraw();
		}
		catch (Exception ex)
		{
			Far.Api.Message(ex.Message, Const.ModuleName, MessageOptions.Warning);
		}
	}

	// ── Credentials ───────────────────────────────────────────────────────

	void SetCredentials()
	{
		var f    = CurrentRemote;
		var host = f is not null ? Commands.Credentials.HostOf(f.RemoteUrl) : "";

		host = Far.Api.Input("Host (e.g. github.com):", "FarGit-cred-host", "Save Credentials", host) ?? "";
		if (string.IsNullOrWhiteSpace(host)) return;

		var user = Far.Api.Input($"Username for {host}:", "FarGit-username", "Save Credentials") ?? "";
		if (string.IsNullOrWhiteSpace(user)) return;

		var token = Far.Api.Input(
			$"Personal Access Token for {host}:\n\n" +
			"For GitHub: Settings → Developer settings → Personal access tokens",
			"FarGit-password", "Save Credentials") ?? "";
		if (string.IsNullOrWhiteSpace(token)) return;

		Commands.Credentials.Set(host, user, token);
		Far.Api.Message($"Credentials saved for '{host}' (encrypted with Windows DPAPI).", Const.ModuleName);
	}

	void ClearCredentials()
	{
		var f    = CurrentRemote;
		var host = f is not null ? Commands.Credentials.HostOf(f.RemoteUrl) : "";

		host = Far.Api.Input("Host to clear:", "FarGit-cred-host", "Clear Credentials", host) ?? "";
		if (string.IsNullOrWhiteSpace(host)) return;

		if (Commands.Credentials.Remove(host))
			Far.Api.Message($"Credentials for '{host}' removed.", Const.ModuleName);
		else
			Far.Api.Message($"No stored credentials found for '{host}'.", Const.ModuleName);
	}

	// ── Menu / key handling ───────────────────────────────────────────────

	internal override void AddMenu(IMenu menu)
	{
		menu.Add(Const.RemoteFetch,           (_, _) => Fetch());
		menu.Add(Const.RemotePull,            (_, _) => Pull());
		menu.Add(Const.RemotePush,            (_, _) => PushToRemote());
		menu.Add(string.Empty).IsSeparator = true;
		menu.Add(Const.RemoteAdd,             (_, _) => AddRemote());
		menu.Add("&Remove remote",            (_, _) => RemoveRemote());
		menu.Add(string.Empty).IsSeparator = true;
		menu.Add("&Save credentials (PAT…)",  (_, _) => SetCredentials());
		menu.Add("&Clear credentials",        (_, _) => ClearCredentials());
	}

	public override bool UIKeyPressed(KeyInfo key)
	{
		switch (key.VirtualKeyCode)
		{
			case KeyCode.F5 when key.Is():
				Fetch();
				return true;
			case KeyCode.F6 when key.Is():
				Pull();
				return true;
			case KeyCode.F7 when key.Is():
				PushToRemote();
				return true;
			case KeyCode.F8 when key.Is():
				RemoveRemote();
				return true;
			case KeyCode.Insert when key.Is():
				AddRemote();
				return true;
		}
		return base.UIKeyPressed(key);
	}
}
