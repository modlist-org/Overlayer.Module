using HarmonyLib;
using Overlayer.Module.ADOFAI.Tag;
using Overlayer.Patch.Safe;
using System;
using System.Reflection;

namespace Overlayer.Module.ADOFAI.Patch;

public sealed class SP_ResetTagState() : SafeConditionalPatch(nameof(SP_ResetTagState)) {
    protected override bool ShouldApply() => true;
    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scrMarginTracker", "Reset");
    protected override HarmonyMethod Postfix() => new(typeof(SP_ResetTagState).GetMethod(nameof(PostfixImpl), BindingFlags.Static | BindingFlags.NonPublic));
    private static void PostfixImpl() => GameplayState.Reset();
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
