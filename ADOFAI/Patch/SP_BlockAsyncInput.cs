using HarmonyLib;
using Overlayer.Patch.Safe;
using Overlayer.UI;
using Overlayer.UI.Objects;
using System.Reflection;

namespace Overlayer.Module.ADOFAI.Patch;

public class SP_BlockAsyncInput() : SafeConditionalPatch(nameof(SP_BlockAsyncInput)) {
    protected override bool ShouldApply() => true;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("scrPlayer", "ValidInputWasTriggered");

    protected override HarmonyMethod Prefix() => new HarmonyMethod(typeof(SP_BlockAsyncInput)
        .GetMethod(nameof(PrefixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static bool PrefixImpl(ref bool __result) {
        if(UICore.CanvasObj == null || !UICore.CanvasObj.activeInHierarchy || !UIInputBlocker.IsEditing) {
            return true;
        }

        __result = false;
        AsyncInputManager.ClearKeys();
        AsyncInputManager.frameDependentKeyMask.Clear();
        AsyncInputManager.frameDependentKeyDownMask.Clear();
        AsyncInputManager.frameDependentKeyUpMask.Clear();
        return false;
    }
}
