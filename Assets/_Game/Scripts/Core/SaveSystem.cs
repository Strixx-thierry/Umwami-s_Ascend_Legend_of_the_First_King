using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string slot = "1";
    public string scene = "Marsh";
    public string place = "Mubari Marsh";
    public string chapter = "Chapter I · The One Who Fell";
    public string image = "marsh-player";          // background used on the save card
    public int cores = 7;                            // lit ember cores (8 in the sky, 7 after the fall)
    public List<string> flags = new();              // story flags, e.g. askedForHelp, knowsThePlot
    public List<string> choices = new();            // human-readable choices for save cards and the Tellings
    public float playSeconds;
    public string savedAt = "";
}

/// <summary>JSON saves in persistentDataPath/saves. Slots: auto, 1, 2, 3.</summary>
public static class SaveSystem
{
    public static readonly string[] Slots = { "auto", "1", "2", "3" };
    /// <summary>Set by the screenshot tool so it never touches real saves.</summary>
    public static string DirOverride;
    static string Dir => DirOverride ?? Path.Combine(Application.persistentDataPath, "saves");
    static string PathOf(string slot) => Path.Combine(Dir, $"{slot}.json");

    public static SaveData Load(string slot)
    {
        var p = PathOf(slot);
        if (!File.Exists(p)) return null;
        try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(p)); }
        catch (Exception e) { Debug.LogWarning($"Save '{slot}' unreadable: {e.Message}"); return null; }
    }

    /// <summary>Writes the slot (and the autosave). Writes to a temp file first so a crash never leaves a half-written save.</summary>
    public static void Save(SaveData d, bool alsoAutosave = true)
    {
        Directory.CreateDirectory(Dir);
        d.savedAt = DateTime.Now.ToString("dd MMM · HH:mm");
        Write(d.slot, d);
        if (alsoAutosave && d.slot != "auto") Write("auto", d);
    }

    static void Write(string slot, SaveData d)
    {
        var path = PathOf(slot);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonUtility.ToJson(d, true));
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }

    public static void Delete(string slot)
    {
        var p = PathOf(slot);
        if (File.Exists(p)) File.Delete(p);
    }

    public static SaveData MostRecent()
    {
        SaveData best = null;
        var bestTime = DateTime.MinValue;
        foreach (var s in Slots)
        {
            var p = PathOf(s);
            if (!File.Exists(p)) continue;
            var t = File.GetLastWriteTime(p);
            if (t <= bestTime) continue;
            var d = Load(s);
            if (d == null) continue;
            best = d;
            bestTime = t;
        }
        return best;
    }
}
