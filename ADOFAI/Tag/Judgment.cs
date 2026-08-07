using Overlayer.Tag.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Overlayer.Module.ADOFAI.Tag;

public static class Judgment {
    private static scrMarginTracker? Tracker => scrController.instance?.playerOne?.marginTracker;
    private static HitMargin CurrentHit => Tracker?.hitMargins.LastOrDefault() ?? HitMargin.Perfect;

    [Tag(Desc = "Current lenient judgment.")] public static string LHit => Localized(JudgmentState.Last(Difficulty.Lenient));
    [Tag(Desc = "Current normal judgment.")] public static string NHit => Localized(JudgmentState.Last(Difficulty.Normal));
    [Tag(Desc = "Current strict judgment.")] public static string SHit => Localized(JudgmentState.Last(Difficulty.Strict));
    [Tag(Desc = "Current judgment.")] public static string CHit => Localized(CurrentHit);
    [Tag(Desc = "Current lenient judgment, independent of game language.")] public static string LHitRaw => JudgmentState.Last(Difficulty.Lenient).ToString();
    [Tag(Desc = "Current normal judgment, independent of game language.")] public static string NHitRaw => JudgmentState.Last(Difficulty.Normal).ToString();
    [Tag(Desc = "Current strict judgment, independent of game language.")] public static string SHitRaw => JudgmentState.Last(Difficulty.Strict).ToString();
    [Tag(Desc = "Current judgment, independent of game language.")] public static string CHitRaw => CurrentHit.ToString();

    [Tag] public static int LTE => Count(Difficulty.Lenient, HitMargin.TooEarly);
    [Tag] public static int LVE => Count(Difficulty.Lenient, HitMargin.VeryEarly);
    [Tag] public static int LEP => Count(Difficulty.Lenient, HitMargin.EarlyPerfect);
    [Tag] public static int LP => Count(Difficulty.Lenient, HitMargin.Perfect);
    [Tag] public static int LLP => Count(Difficulty.Lenient, HitMargin.LatePerfect);
    [Tag] public static int LVL => Count(Difficulty.Lenient, HitMargin.VeryLate);
    [Tag] public static int LTL => Count(Difficulty.Lenient, HitMargin.TooLate);

    [Tag] public static int NTE => Count(Difficulty.Normal, HitMargin.TooEarly);
    [Tag] public static int NVE => Count(Difficulty.Normal, HitMargin.VeryEarly);
    [Tag] public static int NEP => Count(Difficulty.Normal, HitMargin.EarlyPerfect);
    [Tag] public static int NP => Count(Difficulty.Normal, HitMargin.Perfect);
    [Tag] public static int NLP => Count(Difficulty.Normal, HitMargin.LatePerfect);
    [Tag] public static int NVL => Count(Difficulty.Normal, HitMargin.VeryLate);
    [Tag] public static int NTL => Count(Difficulty.Normal, HitMargin.TooLate);

    [Tag] public static int STE => Count(Difficulty.Strict, HitMargin.TooEarly);
    [Tag] public static int SVE => Count(Difficulty.Strict, HitMargin.VeryEarly);
    [Tag] public static int SEP => Count(Difficulty.Strict, HitMargin.EarlyPerfect);
    [Tag] public static int SP => Count(Difficulty.Strict, HitMargin.Perfect);
    [Tag] public static int SLP => Count(Difficulty.Strict, HitMargin.LatePerfect);
    [Tag] public static int SVL => Count(Difficulty.Strict, HitMargin.VeryLate);
    [Tag] public static int STL => Count(Difficulty.Strict, HitMargin.TooLate);

    [Tag] public static int CTE => CurrentCount(HitMargin.TooEarly);
    [Tag] public static int CVE => CurrentCount(HitMargin.VeryEarly);
    [Tag] public static int CEP => CurrentCount(HitMargin.EarlyPerfect);
    [Tag] public static int CP => CurrentCount(HitMargin.Perfect) + CurrentCount(HitMargin.Auto);
    [Tag] public static int CLP => CurrentCount(HitMargin.LatePerfect);
    [Tag] public static int CVL => CurrentCount(HitMargin.VeryLate);
    [Tag] public static int CTL => CurrentCount(HitMargin.TooLate);

    [Tag] public static int LFast => LTE + LVE + LEP;
    [Tag] public static int NFast => NTE + NVE + NEP;
    [Tag] public static int SFast => STE + SVE + SEP;
    [Tag] public static int CFast => CTE + CVE + CEP;
    [Tag] public static int LSlow => LLP + LVL + LTL;
    [Tag] public static int NSlow => NLP + NVL + NTL;
    [Tag] public static int SSlow => SLP + SVL + STL;
    [Tag] public static int CSlow => CLP + CVL + CTL;

    [Tag] public static int MissCount => CurrentCount(HitMargin.FailMiss);
    [Tag] public static int Overloads => CurrentCount(HitMargin.FailOverload);
    [Tag] public static int Multipress => CurrentCount(HitMargin.Multipress);

    private static int Count(Difficulty difficulty, HitMargin margin) => JudgmentState.Count(difficulty, margin);
    private static int CurrentCount(HitMargin margin) => Tracker?.GetHits(margin) ?? 0;
    private static string Localized(HitMargin margin) => RDString.Get($"HitMargin.{margin}");
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

internal static class JudgmentState {
    private static readonly List<HitMargin>[] margins = { new(), new(), new() };

    internal static void Add(HitMargin lenient, HitMargin normal, HitMargin strict) {
        margins[(int)Difficulty.Lenient].Add(lenient);
        margins[(int)Difficulty.Normal].Add(normal);
        margins[(int)Difficulty.Strict].Add(strict);
    }

    internal static void Reset() {
        foreach(var values in margins) values.Clear();
        GameplayState.Reset();
    }

    internal static void Trim(int count) {
        foreach(var values in margins) {
            if(values.Count > count) values.RemoveRange(count, values.Count - count);
        }
    }

    internal static HitMargin Last(Difficulty difficulty) => margins[(int)difficulty].Count == 0
        ? HitMargin.Perfect
        : margins[(int)difficulty][^1];
    internal static int Count(Difficulty difficulty, HitMargin margin) => margins[(int)difficulty].Count(value => value == margin);
    internal static IReadOnlyList<HitMargin> Values(Difficulty difficulty) => margins[(int)difficulty];

    internal static HitMargin Calculate(Difficulty difficulty, float hitAngle, float referenceAngle, bool clockwise,
        float bpmTimesSpeed, float conductorPitch, double marginScale) {
        float window = difficulty switch {
            Difficulty.Lenient => 0.091f,
            Difficulty.Strict => 0.04f,
            _ => 0.065f
        };
        float speed = Math.Max(GCS.currentSpeedTrial, 0.0001f);
        window = ADOBase.isMobile ? 0.09f : window / speed;
        float perfectWindow = ADOBase.isMobile ? 0.07f : 0.03f / speed;
        float pureWindow = ADOBase.isMobile ? 0.05f : 0.02f / speed;
        window = Math.Max(window, 0.025f);
        perfectWindow = Math.Max(perfectWindow, 0.025f);
        pureWindow = Math.Max(pureWindow, 0.025f);

        double counted = Math.Max(GCS.HITMARGIN_COUNTED * marginScale,
            scrMisc.TimeToAngleInRad(window, bpmTimesSpeed, conductorPitch) * 180d / Math.PI);
        double perfect = Math.Max(45d * marginScale,
            scrMisc.TimeToAngleInRad(perfectWindow, bpmTimesSpeed, conductorPitch) * 180d / Math.PI);
        double pure = Math.Max(30d * marginScale,
            scrMisc.TimeToAngleInRad(pureWindow, bpmTimesSpeed, conductorPitch) * 180d / Math.PI);
        double error = (hitAngle - referenceAngle) * (clockwise ? 1d : -1d) * 180d / Math.PI;

        if(error < -counted) return HitMargin.TooEarly;
        if(error < -perfect) return HitMargin.VeryEarly;
        if(error < -pure) return HitMargin.EarlyPerfect;
        if(error <= pure) return HitMargin.Perfect;
        if(error <= perfect) return HitMargin.LatePerfect;
        return error <= counted ? HitMargin.VeryLate : HitMargin.TooLate;
    }
}
