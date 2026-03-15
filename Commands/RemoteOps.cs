using FarNet;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace FarGit.Commands;

/// <summary>
/// Helpers for network operations: fetch, pull, push, clone.
/// Prompts for credentials on demand (HTTPS).
/// </summary>
static class RemoteOps
{
	// ── Credential prompt ─────────────────────────────────────────────────

	/// <summary>
	/// Returns a CredentialsHandler that prompts the user once per call site.
	/// </summary>
	public static CredentialsHandler MakeHandler()
	{
		string? user = null, pass = null;
		return (url, fromUrl, types) =>
		{
			user ??= Far.Api.Input(
				$"Username or email for:\n{url}",
				"FarGit-username",
				"Git Credentials") ?? string.Empty;

			pass ??= Far.Api.Input(
				$"Password or Personal Access Token (PAT) for:\n{url}\n\n" +
				"Tip: For GitHub, use a Personal Access Token,\n" +
				"not your account password.",
				"FarGit-password",
				"Git Credentials") ?? string.Empty;

			return new UsernamePasswordCredentials { Username = user, Password = pass };
		};
	}

	// ── Fetch ─────────────────────────────────────────────────────────────

	public static void Fetch(string gitDir, string remoteName)
	{
		using var repo = new Repository(gitDir);
		var remote = repo.Network.Remotes[remoteName]
			?? throw new InvalidOperationException($"Remote '{remoteName}' not found.");

		var refSpecs = remote.FetchRefSpecs.Select(r => r.Specification);
		LibGit2Sharp.Commands.Fetch(repo, remoteName, refSpecs,
			new FetchOptions { CredentialsProvider = MakeHandler() }, null);
	}

	// ── Pull ─────────────────────────────────────────────────────────────

	public static MergeResult Pull(string gitDir)
	{
		using var repo = new Repository(gitDir);
		var sig = Lib.BuildSignature(repo);
		return LibGit2Sharp.Commands.Pull(repo, sig, new PullOptions
		{
			FetchOptions = new FetchOptions { CredentialsProvider = MakeHandler() },
			MergeOptions = new MergeOptions { FastForwardStrategy = FastForwardStrategy.Default },
		});
	}

	// ── Push ─────────────────────────────────────────────────────────────

	public static void Push(string gitDir, string? remoteName = null)
	{
		using var repo = new Repository(gitDir);
		var branch = repo.Head;

		if (!branch.IsTracking)
		{
			// No upstream set — push to origin (or specified remote) and set tracking
			remoteName ??= Far.Api.Input(
				$"Remote to push '{branch.FriendlyName}' to:",
				"FarGit-remote",
				"Push — Choose Remote") ?? "origin";

			if (string.IsNullOrWhiteSpace(remoteName)) return;

			var remote = repo.Network.Remotes[remoteName]
				?? throw new InvalidOperationException($"Remote '{remoteName}' not found.");

			var pushRefSpec = $"refs/heads/{branch.FriendlyName}:refs/heads/{branch.FriendlyName}";
			repo.Network.Push(remote, pushRefSpec,
				new PushOptions { CredentialsProvider = MakeHandler() });

			// Set tracking so future pushes work without prompting for remote
			repo.Branches.Update(branch,
				b => b.Remote = remoteName,
				b => b.UpstreamBranch = branch.CanonicalName);
		}
		else
		{
			repo.Network.Push(branch,
				new PushOptions { CredentialsProvider = MakeHandler() });
		}
	}

	// ── Clone ─────────────────────────────────────────────────────────────

	public static string Clone(string url, string localPath)
	{
		return Repository.Clone(url, localPath,
			new CloneOptions { FetchOptions = { CredentialsProvider = MakeHandler() } });
	}
}
