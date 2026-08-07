using HarmonyLib;
using Overlayer.Patch.Safe;
using Overlayer.UI;
using Overlayer.UI.Objects;
using System.Reflection;
using UnityEngine;

namespace Overlayer.Module.ADOFAI.Patch;

public class SP_BlockLegacyInput() : SafeConditionalPatch(nameof(SP_BlockLegacyInput)) {
    protected override bool ShouldApply() => true;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("RDInputType_Keyboard", "CheckKeyState", allowStatic: true);

    protected override HarmonyMethod Prefix() => new HarmonyMethod(typeof(SP_BlockLegacyInput)
        .GetMethod(nameof(PrefixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static bool PrefixImpl(KeyCode key, ref bool __result) {
        if(UICore.CanvasObj == null || !UICore.CanvasObj.activeInHierarchy) {
            return true;
        }

        if(UIInputBlocker.IsEditing || IsMouseButton(key)) {
            __result = false;
            return false;
        }

        return true;
    }

    private static bool IsMouseButton(KeyCode key)
        => key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;
}
