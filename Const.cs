namespace FarGit;

static class Const
{
	public const string ModuleName = "FarGit";
	public const string Prefix = "fg";

	// Menu items
	public const string MenuStatus = "&Status";
	public const string MenuStash = "S&tash";
	public const string MenuTags = "&Tags";
	public const string MenuCommit = "&Commit...";
	public const string MenuAmend = "&Amend commit...";

	// Status panel
	public const string StageFile = "&Stage";
	public const string UnstageFile = "&Unstage";
	public const string StageAll = "Stage &all";
	public const string UnstageAll = "Unstage &all";
	public const string EditFile = "&Edit file";
	public const string DiffFile = "&Diff file";

	// Stash panel
	public const string StashApply = "&Apply";
	public const string StashPop = "&Pop";
	public const string StashDrop = "&Drop";
	public const string StashCreate = "&Create stash...";
	public const string StashShow = "&Show changes";

	// Tag panel
	public const string TagCreate = "&Create tag...";
	public const string TagDelete = "&Delete tag";
	public const string TagShow = "&Show commit";

	// Status categories (shown in Description column)
	public const string CatStaged = "Staged";
	public const string CatUnstaged = "Unstaged";
	public const string CatUntracked = "Untracked";
}
