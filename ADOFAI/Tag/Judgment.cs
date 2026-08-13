using Overlayer.Tag.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Overlayer.Module.ADOFAI.Tag;

public static class Judgment {
    private static scrMarginTracker? Tracker => scrController.instance?.playerOne?.marginTracker;

    [Tag(Desc = "Too early judgments.")] public static int TE => CurrentCount(HitMargin.TooEarly);
    [Tag(Desc = "Very early judgments.")] public static int VE => CurrentCount(HitMargin.VeryEarly);
    [Tag(Desc = "Early perfect judgments.")] public static int EP => CurrentCount(HitMargin.EarlyPerfect);
    [Tag(Desc = "Perfect judgments.")] public static int P => PP + A;
    [Tag(Desc = "Late perfect judgments.")] public static int LP => CurrentCount(HitMargin.LatePerfect);
    [Tag(Desc = "Very late judgments.")] public static int VL => CurrentCount(HitMargin.VeryLate);
    [Tag(Desc = "Too late judgments.")] public static int TL => CurrentCount(HitMargin.TooLate);
    [Tag(Desc = "Autoplay perfect judgments.")] public static int A => CurrentCount(HitMargin.Auto);
    [Tag(Desc = "Player perfect judgments.")] public static int PP => CurrentCount(HitMargin.Perfect);
    [Tag(Desc = "Fast judgments.")] public static int Fast => TE + VE + EP;
    [Tag(Desc = "Slow judgments.")] public static int Slow => LP + VL + TL;
    [Tag(Desc = "Early and late perfect judgments.")] public static int ELP => EP + LP;
    [Tag(Desc = "Very early and very late judgments.")] public static int V => VE + VL;
    [Tag(Desc = "Too early and too late judgments.")] public static int T => TE + TL;
    [Tag(Desc = "Number of Misses")]  public static int Miss => CurrentCount(HitMargin.FailMiss);
    [Tag(Desc = "Number of Overloads")] public static int Overload => CurrentCount(HitMargin.OverPress);
    [Tag(Desc = "MissCount + Overloads")] public static int Fail => Miss + Overload;
    [Tag(Desc = "Number of Multipresses")] public static int Multipress => CurrentCount(HitMargin.Multipress);

    private static int CurrentCount(HitMargin margin) => Tracker?.GetHits(margin) ?? 0;
}

public static class Combo {
    private static IReadOnlyList<HitMargin> Current => scrController.instance?.playerOne?.marginTracker?.hitMargins is { } values
        ? values
        : Array.Empty<HitMargin>();

    public static int ComboValue => Tail(Current, IsPerfect);
    [Tag(Name = "Combo")] public static int ComboTag => ComboValue;
    [Tag] public static int MaxCombo => MaxRun(Current, IsPerfect);
    [Tag(TagType = TagType.ProcessFormat)] public static int MarginCombo(HitMargin margin) => Tail(Current, hit => hit == margin);
    [Tag(TagType = TagType.ProcessFormat)] public static int MarginMaxCombo(HitMargin margin) => MaxRun(Current, hit => hit == margin);
    [Tag(TagType = TagType.ProcessFormat)] public static int MarginCombos(string margins) => Tail(Current, Parse(margins));
    [Tag(TagType = TagType.ProcessFormat)] public static int MarginMaxCombos(string margins) => MaxRun(Current, Parse(margins));

    internal static int Tail(IReadOnlyList<HitMargin> values, Func<HitMargin, bool> matches) {
        int count = 0;
        for(int i = values.Count - 1; i >= 0 && matches(values[i]); i--) count++;
        return count;
    }

    internal static int MaxRun(IReadOnlyList<HitMargin> values, Func<HitMargin, bool> matches) {
        int best = 0, current = 0;
        foreach(HitMargin value in values) {
            current = matches(value) ? current + 1 : 0;
            if(current > best) best = current;
        }
        return best;
    }

    private static Func<HitMargin, bool> Parse(string margins) {
        var set = new HashSet<HitMargin>();
        foreach(string value in margins.Split('|')) {
            if(Enum.TryParse(value, true, out HitMargin margin)) set.Add(margin);
        }
        return set.Contains;
    }

    private static bool IsPerfect(HitMargin margin) => margin is HitMargin.Perfect or HitMargin.Auto;
}
