using Overlayer.Tag.Core;
using System;
using System.Collections.Generic;

namespace Overlayer.Module.ADOFAI.Tag;

public static class Judgment {
    private static scrMarginTracker? Tracker => scrController.instance?.playerOne?.marginTracker;
    [Tag(Desc = "Too Early")]     public static int TE => CurrentCount(HitMargin.TooEarly);
    [Tag(Desc = "Very Early")]    public static int VE => CurrentCount(HitMargin.VeryEarly);
    [Tag(Desc = "Early Perfect")] public static int EP => CurrentCount(HitMargin.EarlyPerfect);
    [Tag(Desc = "Perfect Minus")] public static int PM => CurrentCount(HitMargin.PerfectMinus);
    [Tag(Desc = "XPerfect")]      public static int XP => CurrentCount(HitMargin.XPerfect) + A;
    [Tag(Desc = "Perfect Plus")]  public static int PP => CurrentCount(HitMargin.PerfectPlus);
    [Tag(Desc = "Late Perfect")]  public static int LP => CurrentCount(HitMargin.LatePerfect);
    [Tag(Desc = "Very Late")]     public static int VL => CurrentCount(HitMargin.VeryLate);
    [Tag(Desc = "Too Late")]      public static int TL => CurrentCount(HitMargin.TooLate);
        
    [Tag(Desc = "Auto")]                           public static int A => CurrentCount(HitMargin.Auto);
    [Tag(Desc = "Pure XPerfect (excluding Auto)")] public static int PXP => CurrentCount(HitMargin.XPerfect);


    [Tag(Desc = "Perfect (EP + PM + XP + PP + LP)")] public static int P => PM + XP + EP;
    [Tag(Desc = "Fast (TE + VE + EP + PM)")]         public static int Fast => TE + VE + EP + PM;
    [Tag(Desc = "Slow (PP + LP + VL + TL)")]         public static int Slow => PP + LP + VL + TL;
    [Tag(Desc = "Inner Perfects (PM + PP)")]         public static int IP => PM + PP;
    [Tag(Desc = "Outer Perfects (EP + LP)")]         public static int OP => EP + LP;
    [Tag(Desc = "Very Early & Very Late (VE + VL)")] public static int V => VE + VL;
    [Tag(Desc = "Too Early & Too Late (TE + TL)")]   public static int T => TE + TL;

    [Tag(Desc = "Number of Misses")]       public static int Miss => CurrentCount(HitMargin.FailMiss);
    [Tag(Desc = "Number of Overloads")]    public static int Overload => CurrentCount(HitMargin.FailOverload);
    [Tag(Desc = "Total Deaths/Fails")]     public static int Fail => Tracker?.GetDeaths() ?? 0;
    [Tag(Desc = "Number of Multipresses")] public static int Multipress => CurrentCount(HitMargin.Multipress);
    [Tag(Desc = "Number of OverPress")]    public static int OverPress => CurrentCount(HitMargin.OverPress);

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

    private static bool IsPerfect(HitMargin margin) => margin is HitMargin.PerfectMinus or HitMargin.PerfectPlus or HitMargin.Auto;
}
