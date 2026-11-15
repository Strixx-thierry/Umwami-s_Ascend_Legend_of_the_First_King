using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>The fonts of the design language. Assigned in the scene (ProjectBuilder sets them).</summary>
[Serializable]
public class FontSet
{
    public Font display, serif, ui, light, bold;

    /// <summary>Root gets Lato; elements with f-display / f-serif / f-light / f-bold classes get their face.
    /// Call again after adding elements from code.</summary>
    public void Apply(VisualElement root)
    {
        if (ui) root.style.unityFontDefinition = FontDefinition.FromFont(ui);
        Set(root, "f-display", display);
        Set(root, "f-serif", serif);
        Set(root, "f-light", light);
        Set(root, "f-bold", bold);
    }

    static void Set(VisualElement root, string cls, Font f)
    {
        if (!f) return;
        var def = FontDefinition.FromFont(f);
        root.Query(className: cls).ForEach(e => e.style.unityFontDefinition = def);
    }
}

/// <summary>Builders for the recurring parts of the UI (panels, cores, save cards, notices).</summary>
public static class UIKit
{
    public static Label L(string text, params string[] classes)
    {
        var l = new Label(text);
        foreach (var c in classes) l.AddToClassList(c);
        return l;
    }

    public static VisualElement E(params string[] classes)
    {
        var e = new VisualElement();
        foreach (var c in classes) e.AddToClassList(c);
        return e;
    }

    /// <summary>Double hairline frame with imigongo corner ornaments.</summary>
    public static VisualElement Panel(bool hot = false)
    {
        var p = E("panel");
        if (hot) p.AddToClassList("hot");
        var inner = E("panel-inner");
        inner.pickingMode = PickingMode.Ignore;
        p.Add(inner);
        float[] rot = { 0, 90, 180, 270 };
        for (int i = 0; i < 4; i++)
        {
            var c = E("corner");
            c.pickingMode = PickingMode.Ignore;
            if (i == 0 || i == 3) c.style.left = -4; else c.style.right = -4;
            if (i < 2) c.style.top = -4; else c.style.bottom = -4;
            c.style.rotate = new Rotate(rot[i]);
            p.Add(c);
        }
        return p;
    }

    public static VisualElement Cores(int lit, int empty, int lost, float size = 22)
    {
        var row = E("cores");
        void add(string cls, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var c = E("core");
                if (cls != null) c.AddToClassList(cls);
                c.style.width = size;
                c.style.height = size;
                c.style.marginRight = size * 0.3f;
                row.Add(c);
            }
        }
        add(null, lit);
        add("empty", empty);
        add("lost", lost);
        return row;
    }

    public static string Time(float seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours} h {t.Minutes:00} m" : $"{t.Minutes} m";
    }

    public static string BgClass(string image) => image switch
    {
        "village" => "bg-village",
        "marsh-arena" => "bg-arena",
        "marsh-valley" => "bg-valley",
        _ => "bg-player",
    };

    public static VisualElement SlotCard(string slot, SaveData d)
    {
        var p = Panel(slot == "auto" && d != null);
        p.AddToClassList("slot");
        if (d == null)
        {
            p.style.justifyContent = Justify.Center;
            p.style.alignItems = Align.Center;
            p.style.borderTopColor = p.style.borderBottomColor = p.style.borderLeftColor = p.style.borderRightColor = new Color(0.83f, 0.69f, 0.42f, 0.2f);
            var core = E("core", "empty");
            p.Add(core);
            var t = L(slot == "auto" ? "No autosave yet" : "Empty", "f-serif", "parch");
            t.style.fontSize = 22;
            t.style.marginTop = 18;
            p.Add(t);
            p.Add(L("Begin a new telling", "body", "f-light"));
            return p;
        }
        var img = E("slot-img", "bg", BgClass(d.image));
        p.Add(img);
        var body = E("slot-body");
        var top = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
        top.Add(L(slot == "auto" ? "AUTOSAVE" : $"SLOT {slot}", "eyebrow"));
        var date = L(d.savedAt, "dim-text", "f-light");
        date.style.fontSize = 12;
        top.Add(date);
        body.Add(top);
        var place = L(d.place, "f-serif", "parch");
        place.style.fontSize = 23;
        place.style.marginTop = 8;
        body.Add(place);
        var ch = L(d.chapter, "dim-text", "f-light");
        ch.style.fontSize = 13;
        body.Add(ch);
        var cores = Cores(d.cores, 0, Mathf.Max(0, 8 - d.cores), 12);
        cores.style.marginTop = 10;
        cores.style.marginBottom = 8;
        body.Add(cores);
        for (int i = Mathf.Max(0, d.choices.Count - 3); i < d.choices.Count; i++)
        {
            var c = L("◆  " + d.choices[i], "dim-text", "f-light");
            c.style.fontSize = 13;
            body.Add(c);
        }
        var hair = E("hair");
        hair.style.marginTop = 12;
        hair.style.marginBottom = 8;
        body.Add(hair);
        var time = L(Time(d.playSeconds).ToUpper(), "parch");
        time.style.fontSize = 12;
        time.style.letterSpacing = 3;
        body.Add(time);
        p.Add(body);
        return p;
    }

    public static VisualElement ContinueCard(SaveData d)
    {
        var p = Panel();
        p.style.paddingLeft = p.style.paddingRight = 20;
        p.style.paddingTop = p.style.paddingBottom = 14;
        p.style.marginTop = 4;
        p.style.marginBottom = 12;
        var row1 = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
        var place = L(d.place, "f-serif", "parch");
        place.style.fontSize = 19;
        row1.Add(place);
        var t = L(Time(d.playSeconds), "dim-text", "f-light");
        t.style.fontSize = 13;
        row1.Add(t);
        var row2 = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
        var ch = L(d.chapter, "dim-text", "f-light");
        ch.style.fontSize = 13;
        row2.Add(ch);
        row2.Add(Cores(d.cores, 0, 0, 9));
        p.Add(row1);
        p.Add(row2);
        return p;
    }

    /// <summary>A notice line that fades out and removes itself.</summary>
    public static void Notice(VisualElement host, string text, bool ochre = false, long ms = 3500)
    {
        var n = L(text, "notice");
        if (ochre) n.AddToClassList("ochre-edge");
        host.Add(n);
        n.schedule.Execute(() => n.style.opacity = 0).StartingIn(ms);
        n.schedule.Execute(() => n.RemoveFromHierarchy()).StartingIn(ms + 500);
    }
}
