using HarmonyLib;
using Overlayer.Patch.Safe;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Overlayer.Module.ADOFAI.Patch;

public class SP_ShowAutoJudgment() : SafeConditionalPatch(nameof(SP_ShowAutoJudgment)) {
    protected override bool ShouldApply() => Core.Config.ShowAutoplayJudgment;

    protected override MethodBase GetTargetMethod() => SafePatch.GetMethodSafe("scrController", "UpdateHitErrorMeter");

    protected override HarmonyMethod Transpiler() {
        return new HarmonyMethod(typeof(SP_ShowAutoJudgment)
            .GetMethod(nameof(TranspilerImpl), BindingFlags.Static | BindingFlags.NonPublic));
    }

    private static IEnumerable<CodeInstruction> TranspilerImpl(IEnumerable<CodeInstruction> instructions) {
        var codes = new List<CodeInstruction>(instructions);
        int patched = 0;

        for(int i = 0; i < codes.Count; i++) {
            if(codes[i].opcode != OpCodes.Call) {
                continue;
            }
            if(codes[i].operand is not MethodInfo m || m.Name != "get_auto" || m.DeclaringType?.Name != "RDC") {
                continue;
            }
            codes[i] = new CodeInstruction(OpCodes.Ldc_I4_0);
            patched++;
        }

        if(patched == 0) {
            Core.Logger.Wrn($"[{nameof(SP_ShowAutoJudgment)}] No RDC.auto check found in scrController.UpdateHitErrorMeter. Patch did nothing.");
        }

        return codes;
    }
}