using HarmonyLib;
using Overlayer.Patch.Safe;
using Overlayer.UI;
using System.Reflection;

namespace Overlayer.Module.ADOFAI.Patch;

public class SP_BlockAsyncInput() : SafeConditionalPatch(nameof(SP_BlockAsyncInput)) {
    protected override bool ShouldApply() => Core.Config.BlockInputWhenOpened;

    protected override MethodBase GetTargetMethod()
        => SafePatch.GetMethodSafe("scrPlayer", "ValidInputWasTriggered");

    protected override HarmonyMethod Prefix() => new HarmonyMethod(typeof(SP_BlockAsyncInput)
        .GetMethod(nameof(PrefixImpl), BindingFlags.Static | BindingFlags.NonPublic));

    private static bool PrefixImpl(ref bool __result) {
        if(UICore.CanvasObj == null || !UICore.CanvasObj.activeInHierarchy) {
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
