using HarmonyLib;
using Overlayer.Module.ADOFAI.Tag;
using Overlayer.Patch.Safe;
using System;
using System.Reflection;

namespace Overlayer.Module.ADOFAI.Patch;

public sealed class SP_RecordJudgment() : SafeConditionalPatch(nameof(SP_RecordJudgment)) {
    protected override bool ShouldApply() => true;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scrMisc", "GetHitMargin", allowStatic: true);
    protected override HarmonyMethod Postfix() => new(typeof(SP_RecordJudgment).GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PostfixImpl(float hitangle, float refangle, bool isCW, float bpmTimesSpeed,
        float conductorPitch, double marginScale, HitMargin __result) {
        var controller = scrController.instance;
        if(controller?.currFloor == null || controller.currFloor.freeroam || controller.currFloor.isSafe) return;
        HitMargin lenient = Normalize(JudgmentState.Calculate(Difficulty.Lenient, hitangle, refangle, isCW, bpmTimesSpeed, conductorPitch, marginScale), controller);
        HitMargin normal = Normalize(JudgmentState.Calculate(Difficulty.Normal, hitangle, refangle, isCW, bpmTimesSpeed, conductorPitch, marginScale), controller);
        HitMargin strict = Normalize(JudgmentState.Calculate(Difficulty.Strict, hitangle, refangle, isCW, bpmTimesSpeed, conductorPitch, marginScale), controller);
        JudgmentState.Add(lenient, normal, strict);
    }

    private static HitMargin Normalize(HitMargin margin, scrController controller) {
        if(controller.noFailInfiniteMargin) return HitMargin.FailMiss;
        if(controller.playerOne.midspinInfiniteMargin || (RDConstants.data.auto && !RDConstants.data.useOldAuto)) return HitMargin.Perfect;
        return margin;
    }
}

public sealed class SP_ResetTagState() : SafeConditionalPatch(nameof(SP_ResetTagState)) {
    protected override bool ShouldApply() => true;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scrMarginTracker", "Reset");
    protected override HarmonyMethod Postfix() => new(typeof(SP_ResetTagState).GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static void PostfixImpl() => JudgmentState.Reset();
}

public sealed class SP_RevertTagState() : SafeConditionalPatch(nameof(SP_RevertTagState)) {
    protected override bool ShouldApply() => true;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scrMarginTracker", "RevertToLastCheckpoint");
    protected override HarmonyMethod Postfix() => new(typeof(SP_RevertTagState).GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static void PostfixImpl(scrMarginTracker __instance) => JudgmentState.Trim(__instance.hitMargins.Count);
}

public sealed class SP_RecordTiming() : SafeConditionalPatch(nameof(SP_RecordTiming)) {
    protected override bool ShouldApply() => true;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scrPlanet", "SwitchChosen");
    protected override HarmonyMethod Prefix() => new(typeof(SP_RecordTiming).GetMethod(nameof(PrefixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static void PrefixImpl(scrPlanet __instance) {
        var controller = scrController.instance;
        if(controller == null || __instance.conductor == null) return;
        double denominator = Math.PI * __instance.conductor.bpm * controller.currFloor.speed * __instance.conductor.song.pitch;
        if(denominator == 0) return;
        double timing = (__instance.angle - __instance.targetExitAngle) * (controller.currFloor.isCCW ? -1d : 1d) * 60000d / denominator;
        GameplayState.RecordTiming(timing);
    }
}
