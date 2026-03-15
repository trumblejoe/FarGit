using FarNet;
using LibGit2Sharp;

namespace FarGit.Panels;

/// <summary>
/// A file entry in the <see cref="StatusPanel"/>.
/// Name: relative file path
/// Description: category (Staged / Unstaged / Untracked)
/// Owner: one-letter status symbol (A / M / D / R / ?)
/// </summary>
public class StatusFile(string path, string category, string symbol, StatusEntry entry) : FarFile
{
	public override string Name => path;
	public override string Description => category;
	public override string? Owner => symbol;
	public StatusEntry Entry => entry;

	public bool IsStaged => category == Const.CatStaged;
	public bool IsUntracked => category == Const.CatUntracked;
}

/// <summary>
/// A file entry in the <see cref="StashPanel"/>.
/// Name: stash message
/// Owner: "stash@{n}" label
/// LastWriteTime: when the stash was created
/// </summary>
public class StashFile(int index, Stash stash) : FarFile
{
	public override string Name => stash.Message;
	public override string? Owner => $"stash@{{{index}}}";
	public override DateTime LastWriteTime => stash.WorkTree.Author.When.LocalDateTime;
	public override FileAttributes Attributes => FileAttributes.Directory;

	public Stash Stash => stash;
	public int Index => index;
}

/// <summary>
/// A file entry in the <see cref="TagPanel"/>.
/// Name: tag name
/// Description: abbreviated SHA + short message of the tagged commit (if any)
/// LastWriteTime: tagger/commit date
/// </summary>
public class TagFile(string name, string description, DateTime when, Tag tag) : FarFile
{
	public override string Name => name;
	public override string Description => description;
	public override DateTime LastWriteTime => when;
	public override FileAttributes Attributes => FileAttributes.Directory;

	public Tag Tag => tag;
}

/// <summary>
/// A file entry in the <see cref="BranchPanel"/>.
/// Name: branch friendly name   Owner: "*" if current / remote name if remote
/// Description: tracking ahead/behind info for local branches
/// </summary>
public class BranchFile(Branch branch) : FarFile
{
	public override string Name => branch.FriendlyName;

	public override string? Owner => branch.IsCurrentRepositoryHead ? "*" :
	                                 branch.IsRemote ? RemoteName(branch) : "";

	public override string Description
	{
		get
		{
			if (branch.IsRemote || !branch.IsTracking) return string.Empty;
			var td = branch.TrackingDetails;
			var parts = new List<string>();
			if (td.AheadBy  > 0) parts.Add($"+{td.AheadBy} to push");
			if (td.BehindBy > 0) parts.Add($"-{td.BehindBy} to pull");
			return parts.Count > 0 ? string.Join("  ", parts) : "up to date";
		}
	}

	public override FileAttributes Attributes =>
		branch.IsCurrentRepositoryHead ? FileAttributes.Directory : FileAttributes.Normal;

	public Branch Branch => branch;
	public bool IsCurrent => branch.IsCurrentRepositoryHead;

	static string RemoteName(Branch b)
	{
		var n = b.FriendlyName;
		var slash = n.IndexOf('/');
		return slash >= 0 ? n[..slash] : "remote";
	}
}

/// <summary>
/// A file entry in the <see cref="RemotePanel"/>.
/// Name: remote name   Owner: URL
/// </summary>
public class RemoteFile(Remote remote) : FarFile
{
	public override string Name => remote.Name;
	public override string? Owner => remote.Url;
	public override string Description => "fetch + push";
	public override FileAttributes Attributes => FileAttributes.Directory;
	public Remote Remote => remote;
}

/// <summary>
/// A read-only entry in the <see cref="GuidePanel"/>.
/// Name: concept or command   Owner: category   Description: one-line summary
/// </summary>
public class GuideFile(string category, string name, string summary, string detail) : FarFile
{
	public override string Name => name;
	public override string? Owner => category;
	public override string Description => summary;
	public override FileAttributes Attributes => FileAttributes.ReadOnly;
	public string Detail => detail;
}

/// <summary>
/// A file entry in the <see cref="CommitPanel"/>.
/// Name: abbreviated SHA + commit message   Owner: author name
/// Description: branch/tag labels pointing to this commit (e.g. "HEAD → main, origin/main")
/// </summary>
public class CommitFile(Commit commit, string labels) : FarFile
{
	public override string Name => $"{commit.Sha[..7]}  {commit.MessageShort}";
	public override string? Owner => commit.Author.Name;
	public override DateTime LastWriteTime => commit.Author.When.LocalDateTime;
	public override string Description => labels;
	public override FileAttributes Attributes =>
		string.IsNullOrEmpty(labels) ? FileAttributes.Normal : FileAttributes.Directory;

	public Commit Commit => commit;
}

/// <summary>
/// Which section a <see cref="DashboardFile"/> represents.
/// </summary>
public enum DashboardSection { Branch, Branches, History, Staged, Unstaged, Untracked, Stash, Tags, Remote, Guide }

/// <summary>
/// A row in the <see cref="DashboardPanel"/> summary view.
/// Name: section label (e.g. "Staged")
/// Owner: count/value (e.g. "3 files")
/// Description: human-readable detail
/// </summary>
public class DashboardFile(string name, string owner, string description, DashboardSection section) : FarFile
{
	public override string Name => name;
	public override string? Owner => owner;
	public override string Description => description;
	public override FileAttributes Attributes => FileAttributes.Directory;
	public DashboardSection Section => section;
}
