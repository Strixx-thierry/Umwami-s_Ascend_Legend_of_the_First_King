/// <summary>The telling in progress. Lives across scene loads.</summary>
public static class GameSession
{
    public static SaveData Current;
    public static bool ReturnToMenu;   // Frontend skips the boot screens when coming back from the game

    public static void NewGame(string slot)
    {
        Current = new SaveData { slot = slot };
    }

    public static bool Has(string flag) => Current != null && Current.flags.Contains(flag);

    /// <summary>Records a story flag and the line shown on save cards / the Tellings.</summary>
    public static void Choose(string flag, string description)
    {
        Current ??= new SaveData { slot = "auto" };
        if (!Current.flags.Contains(flag)) Current.flags.Add(flag);
        if (!string.IsNullOrEmpty(description)) Current.choices.Add(description);
    }
}
