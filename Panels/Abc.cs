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
