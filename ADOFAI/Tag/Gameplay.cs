using ADOFAI;
using Overlayer.Tag.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.Tag;

public static class Gameplay {
    private static scrController? Controller => scrController.instance;
    private static scrMarginTracker? Tracker => Controller?.playerOne?.marginTracker;
    private static List<scrFloor>? Floors => scrLevelMaker.instance?.listFloors;
    private static LevelData? Level => scnGame.instance?.levelData ?? scnEditor.instance?.levelData;

    [Tag] public static double Accuracy => Percent(Tracker?.percentAcc);
    [Tag] public static double XAccuracy => Percent(Tracker?.percentXAcc);

    [Tag] public static int LScore => Score(JudgmentState.Values(global::Difficulty.Lenient));
    [Tag] public static int NScore => Score(JudgmentState.Values(global::Difficulty.Normal));
    [Tag] public static int SScore => Score(JudgmentState.Values(global::Difficulty.Strict));
    public static int ScoreValue => Score(Tracker?.hitMargins);
    [Tag(Name = "Score")] public static int ScoreTag => ScoreValue;

    [Tag] public static double Progress => (Controller?.percentComplete ?? 0f) * 100d;
    [Tag] public static double StartProgress => TotalTile == 0 ? 0 : StartTile * 100d / TotalTile;
    [Tag] public static double BestProgress {
        get {
            GameplayState.BestProgress = Math.Max(GameplayState.BestProgress, Progress);
            return GameplayState.BestProgress;
        }
    }
    [Tag] public static double ActualProgress {
        get {
            var floors = Floors;
            var floor = Controller?.currFloor;
            if(floors == null || floors.Count < 2 || floor == null) return 0;
            double start = floors[0].entryTime;
            double end = floors[^1].entryTime;
            return end <= start ? 0 : Math.Max(0, Math.Min(100, (floor.entryTime - start) * 100 / (end - start)));
        }
    }

    [Tag] public static int CheckPointUsed => scrController.checkpointsUsed;
    [Tag] public static int CurCheckPoint => Floors?.Count(floor => floor.seqID <= (Controller?.currentSeqID ?? 0) && floor.GetComponent<ffxCheckpoint>() != null) ?? 0;
    [Tag] public static int TotalCheckPoints => Floors?.Count(floor => floor.GetComponent<ffxCheckpoint>() != null) ?? 0;

    [Tag] public static int StartTile => Floors == null || Floors.Count == 0 ? 0 : Math.Min(GCS.checkpointNum + 1, Floors.Count);
    [Tag] public static int CurTile => Controller == null ? 0 : Controller.currentSeqID + 1;
    [Tag] public static int LeftTile => Math.Max(0, TotalTile - CurTile);
    [Tag] public static int TotalTile => Floors?.Count ?? 0;

    [Tag] public static double Pitch => GCS.currentSpeedTrial;
    [Tag] public static double EditorPitch => (Level?.pitch ?? 100) / 100d;
    [Tag] public static string Difficulty => RDString.Get($"enum.Difficulty.{GCS.difficulty}");
    [Tag] public static string DifficultyRaw => GCS.difficulty.ToString();

    [Tag] public static bool IsStarted => Controller != null && Controller.currentSeqID > GCS.checkpointNum;
    [Tag] public static bool IsAutoEnabled => RDConstants.data?.auto ?? false;
    [Tag] public static bool IsPracticeModeEnabled => GCS.practiceMode || (RDConstants.data?.practice ?? false);
    [Tag] public static bool IsOldAutoEnabled => RDConstants.data?.useOldAuto ?? false;
    [Tag] public static bool IsNoFailEnabled => Controller?.noFail ?? GCS.useNoFail;

    [Tag] public static double Timing => GameplayState.Timing;
    [Tag] public static double TimingAvg => GameplayState.Timings.Count == 0 ? 0 : GameplayState.Timings.Average();
    [Tag] public static double MarginScale => Controller?.currFloor?.marginScale ?? 0;

    [Tag] public static int CurMinute => SongTime.Minutes;
    [Tag] public static int CurSecond => SongTime.Seconds;
    [Tag] public static int CurMilliSecond => SongTime.Milliseconds;
    [Tag] public static int TotalMinute => SongLength.Minutes;
    [Tag] public static int TotalSecond => SongLength.Seconds;
    [Tag] public static int TotalMilliSecond => SongLength.Milliseconds;

    [Tag] public static string Title => Strip(Level?.song);
    [Tag] public static string Artist => Strip(Level?.artist);
    [Tag] public static string Author => Strip(Level?.author);
    [Tag] public static string TitleRaw => Level?.song ?? string.Empty;
    [Tag] public static string ArtistRaw => Level?.artist ?? string.Empty;
    [Tag] public static string AuthorRaw => Level?.author ?? string.Empty;

    [Tag] public static double TileBpm => BaseBpm * CurrentSpeed * SongPitch;
    [Tag] public static double CurBpm => RealBpm * SongPitch;
    [Tag] public static double RecKPS => CurBpm / 60d;
    [Tag] public static double TileBpmWithoutPitch => BaseBpm * CurrentSpeed;
    [Tag] public static double CurBpmWithoutPitch => RealBpm;
    [Tag] public static double RecKPSWithoutPitch => CurBpmWithoutPitch / 60d;

    private static TimeSpan SongTime => TimeSpan.FromSeconds(Math.Max(0, scrConductor.instance?.song?.time ?? 0));
    private static TimeSpan SongLength => TimeSpan.FromSeconds(Math.Max(0, scrConductor.instance?.song?.clip?.length ?? 0));
    private static double BaseBpm => Level?.bpm ?? scrConductor.instance?.bpm ?? 0;
    private static double CurrentSpeed => Controller?.currFloor?.speed ?? 0;
    private static double SongPitch => scrConductor.instance?.song?.pitch ?? 1;
    private static double RealBpm {
        get {
            var floor = Controller?.currFloor;
            if(floor?.nextfloor == null) return BaseBpm * CurrentSpeed;
            double duration = floor.nextfloor.entryTime - floor.entryTime;
            return duration <= 0 ? BaseBpm * CurrentSpeed : 60d / duration;
        }
    }

    private static double Percent(float? value) => value.HasValue && !float.IsNaN(value.Value) ? value.Value * 100d : 0;
    private static string Strip(string? value) => string.IsNullOrEmpty(value) ? string.Empty : RDUtils.RemoveRichTags(value);
    private static int Score(IEnumerable<HitMargin>? margins) => margins?.Sum(margin => margin switch {
        HitMargin.Perfect or HitMargin.Auto => 300,
        HitMargin.EarlyPerfect or HitMargin.LatePerfect => 150,
        HitMargin.VeryEarly or HitMargin.VeryLate => 91,
        _ => 0
    }) ?? 0;
}

public static class Performance {
    private static readonly Process process = Process.GetCurrentProcess();
    private static DateTime lastCpuTime = DateTime.UtcNow;
    private static TimeSpan lastProcessTime = process.TotalProcessorTime;
    private static double cpuUsage;

    [Tag] public static double FrameTime => UnityEngine.Time.unscaledDeltaTime * 1000d;
    [Tag] public static double Fps => UnityEngine.Time.unscaledDeltaTime <= 0 ? 0 : 1d / UnityEngine.Time.unscaledDeltaTime;
    [Tag] public static int ProcessorCount => SystemInfo.processorCount;
    [Tag] public static double CpuUsage => SampleCpu();
    [Tag] public static double TotalCpuUsage => 0;
    [Tag] public static double MemoryUsageGBytes => process.WorkingSet64 / 1073741824d;
    [Tag] public static double TotalMemoryUsageGBytes => 0;
    [Tag] public static double MemoryUsage => SystemInfo.systemMemorySize <= 0 ? 0 : MemoryUsageGBytes * 102400d / SystemInfo.systemMemorySize;
    [Tag] public static double TotalMemoryUsage => 0;
    [Tag] public static double MemoryGBytes => SystemInfo.systemMemorySize / 1024d;

    [Tag] public static double Days => DateTime.Now.TimeOfDay.TotalDays;
    [Tag] public static double Hours => DateTime.Now.TimeOfDay.TotalHours;
    [Tag] public static double Minutes => DateTime.Now.TimeOfDay.TotalMinutes;
    [Tag] public static double Seconds => DateTime.Now.TimeOfDay.TotalSeconds;
    [Tag] public static double MilliSeconds => DateTime.Now.TimeOfDay.TotalMilliseconds;
    [Tag(Name = "MilliSecond")] public static int MilliSecondTag => DateTime.Now.Millisecond;

    private static double SampleCpu() {
        DateTime now = DateTime.UtcNow;
        double elapsed = (now - lastCpuTime).TotalMilliseconds;
        if(elapsed < 250) return cpuUsage;
        TimeSpan current = process.TotalProcessorTime;
        cpuUsage = Math.Max(0, Math.Min(100, (current - lastProcessTime).TotalMilliseconds * 100 / elapsed / Math.Max(1, Environment.ProcessorCount)));
        lastCpuTime = now;
        lastProcessTime = current;
        return cpuUsage;
    }
}

internal static class GameplayState {
    internal static double Timing;
    internal static double BestProgress;
    internal static readonly List<double> Timings = new();

    internal static void RecordTiming(double value) {
        Timing = value;
        Timings.Add(value);
    }

    internal static void Reset() {
        Timing = 0;
        Timings.Clear();
    }
}
