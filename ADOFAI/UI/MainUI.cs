using Overlayer.Localization;
using Overlayer.Module.ADOFAI.IO;
using Overlayer.Module.ADOFAI.Patch;
using Overlayer.Patch.Safe;
using Overlayer.Core;
using Overlayer.UI.Factory;
using O5Kit.Behaviour;
using O5Kit.Control;
using O5Kit.Core;
using O5Kit.Factory;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Overlayer.Module.ADOFAI.UI;

public static class MainUI {
    private static GameObject? _inputBlockerObject;

    public static void CreateInputBlocker(Transform parent) {
        GameObject blocker = new("ADOFAI Input Blocker");
        blocker.transform.SetParent(parent, false);
        blocker.transform.SetAsFirstSibling();

        RectTransform rect = blocker.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        blocker.AddComponent<EmptyGraphic>().raycastTarget = true;

        _inputBlockerObject = blocker;
        UpdateInputBlockerState(Core.Config.BlockInputWhenOpened);
    }

    public static void CreateMenu(RectTransform parent)
        => MenuFactory.CreateItem(parent, "ADOFAI", Core.Spr.Get("Image.ADOFAI.png"), 100)
        .label.gameObject.AddComponent<TextLocalization>().Init("ADOFAI", "ADOFAI", Core.Tr);

    private static readonly Dictionary<string, O5Object> objects = [];

    public static void CreatePage(RectTransform parent) {
        GameObject pad = new("Pad");
        pad.transform.SetParent(parent, false);

        RectTransform padRect = pad.AddComponent<RectTransform>();
        padRect.anchorMin = Vector2.zero;
        padRect.anchorMax = Vector2.one;
        padRect.pivot = new Vector2(0.5f, 0.5f);
        padRect.offsetMin = new Vector2(18f, 18f);
        padRect.offsetMax = new Vector2(-18f, -18f);

        GameObject viewport = new("Viewport");
        viewport.transform.SetParent(pad.transform, false);

        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);

        viewport.AddComponent<EmptyGraphic>().raycastTarget = true;
        viewport.AddComponent<RectMask2D>();

        GameObject content = new("Content");
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        pad.AddComponent<UIScrollController>().SetContent(contentRect, viewportRect);

        ADOFAISettings defSet = new();

        _ = O5Factory.ControlTextH1(O5Factory.Row(content.transform))
           .gameObject.AddComponent<TextLocalization>()
           .Init("ADOFAI", "ADOFAI", Core.Tr);

        if (Application.platform == RuntimePlatform.LinuxPlayer) {
            O5Toggle linuxTextInputToggle = O5Factory.Toggle(
                O5Factory.Row(content.transform),
                defSet.LinuxTextInputFix,
                Core.Config.LinuxTextInputFix,
                toggle => {
                    Core.Config.LinuxTextInputFix = toggle;
                    Core.ConfigFile.RequestSave();

                    ApplyState(SafePatchController.Get<SP_LinuxTMPKeyInput>(), toggle);
                    ApplyState(SafePatchController.Get<SP_LinuxLegacyKeyInput>(), toggle);
                },
                "Linux Text Input Fix",
                "linux_text_input_fix"
            );
            linuxTextInputToggle.EnabledWhen = () => MainCore.IsModEnabled;
            linuxTextInputToggle.Label.gameObject.AddComponent<TextLocalization>().Init("LINUX_TEXT_INPUT_FIX", "Linux Text Input Fix", Core.Tr);
            objects[linuxTextInputToggle.Id] = linuxTextInputToggle;
            linuxTextInputToggle.Rect.AddToolTip(() => TooltipText("DESC_LINUX_TEXT_INPUT_FIX", "Fixes duplicate characters and Shift-modified text input on Linux", "ADV_DESC_LINUX_TEXT_INPUT_FIX", "Prevents Linux Unity builds from double-processing input caused\nby OS text events and physical key events firing simultaneously.\n\nThe Process method tracks frame counts and pending states to\npass only the first arriving event of a pair while dropping duplicates.\n\nIt also resolves corrupted Shift and CapsLock inputs\nby analyzing key codes and modifier states via bitwise operations to recalculate the exact ASCII characters."));
        }

        O5Toggle blockInputToggle = O5Factory.Toggle(
            O5Factory.Row(content.transform),
            defSet.BlockInputWhenOpened,
            Core.Config.BlockInputWhenOpened,
            toggle => {
                Core.Config.BlockInputWhenOpened = toggle;
                Core.ConfigFile.RequestSave();
                UpdateInputBlockerState(toggle);
                ApplyState(SafePatchController.Get<SP_BlockAsyncInput>(), toggle);
                ApplyState(SafePatchController.Get<SP_BlockLegacyInput>(), toggle);
                ApplyState(SafePatchController.Get<SP_BlockInputMethod>(), toggle);
                ApplyState(SafePatchController.Get<SP_BlockDirectInput>(), toggle);
            },
            "Block Input When Opened",
            "block_input_when_opened"
        );
        blockInputToggle.EnabledWhen = () => MainCore.IsModEnabled;
        blockInputToggle.Label.gameObject.AddComponent<TextLocalization>().Init("BLOCK_INPUT_WHEN_OPENED", "Block Input When Opened", Core.Tr);
        objects[blockInputToggle.Id] = blockInputToggle;
        blockInputToggle.Rect.AddToolTip(() => TooltipText("DESC_BLOCK_INPUT_WHEN_OPENED", "Blocks game inputs while the Overlayer UI is opened", "ADV_DESC_BLOCK_INPUT_WHEN_OPENED", "Hooks into ADOFAI's input architecture across 4 distinct layers:\n\n1. Async Input: Patches scrPlayer.ValidInputWasTriggered and clears key masks in AsyncInputManager.\n2. Legacy Input: Patches RDInputType_Keyboard.CheckKeyState to block editing and mouse input.\n3. Input Method: Intercepts OptionsPanelsCLS.CheckInputs to suppress menu input events.\n4. Direct Input: Uses Transpiler on level select Update methods to redirect UnityEngine.Input calls to custom wrappers.\n\nAlso creates a full-screen Raycast target (EmptyGraphic) behind the UI to block UI-level interactions"));

        O5Toggle showAutoJudgmentToggle = O5Factory.Toggle(
            O5Factory.Row(content.transform),
            defSet.ShowAutoplayJudgment,
            Core.Config.ShowAutoplayJudgment,
            toggle => {
                Core.Config.ShowAutoplayJudgment = toggle;
                Core.ConfigFile.RequestSave();

                ApplyState(SafePatchController.Get<SP_ShowAutoJudgment>(), toggle);
            },
            "Show Autoplay Judgment",
            "show_autoplay_judgment"
        );
        showAutoJudgmentToggle.EnabledWhen = () => MainCore.IsModEnabled;
        showAutoJudgmentToggle.Label.gameObject.AddComponent<TextLocalization>().Init("SHOW_AUTOPLAY_JUDGMENT", "Show Autoplay Judgment", Core.Tr);
        objects[showAutoJudgmentToggle.Id] = showAutoJudgmentToggle;
        showAutoJudgmentToggle.Rect.AddToolTip(() => TooltipText("DESC_SHOW_AUTOPLAY_JUDGMENT", "Applies a patch to show the true judgment in AutoPlay on the Hit Error Meter", "ADV_DESC_SHOW_AUTOPLAY_JUDGMENT", "Patches scrController.UpdateHitErrorMeter method using Transpiler.\n\nOriginal UpdateHitErrorMeter checks 'RDC.auto'\nto force hit error meter values to 0.0f (Perfect)\nduring AutoPlay.\nThe Transpiler scans IL instructions for Call 'RDC.get_auto',\nand replaces it with Ldc_I4_0.\n\nThis forces the auto check to evaluate as false,\nallowing the Error Meter to process actual angle diffs\nand margin scales"));

        O5Toggle hideTitleToggle = O5Factory.Toggle(
            O5Factory.Row(content.transform),
            defSet.HideTitle,
            Core.Config.HideTitle,
            toggle => {
                Core.Config.HideTitle = toggle;
                Core.ConfigFile.RequestSave();
                GCS.d_dontShowTitles = toggle;
            },
            "Hide Title",
            "hide_title"
        );
        hideTitleToggle.EnabledWhen = () => MainCore.IsModEnabled;
        hideTitleToggle.Label.gameObject.AddComponent<TextLocalization>().Init("HIDE_TITLE", "Hide Title", Core.Tr);
        objects[hideTitleToggle.Id] = hideTitleToggle;
        hideTitleToggle.Rect.AddToolTip(() => TooltipText("DESC_HIDE_TITLE", "Hides in-game level titles using GCS setting", "ADV_DESC_HIDE_TITLE", "Controls the unused static flag 'GCS.d_dontShowTitles' in game memory.\n\nADOFAI's codebase contains logic that reads 'd_dontShowTitles' to hide level titles during gameplay,\nbut the game never assigns a value to this field anywhere.\n\nThis option exposes control over that field directly,\nenabling native title hiding without needing additional patches"));
        return;

        static void ApplyState<T>(T[] patches, bool enable) where T : SafeConditionalPatch {
            foreach (var patch in patches) {
                if (enable) patch.Apply();
                else patch.Remove();
            }
        }
    }

    private static string TooltipText(string key, string def, string advKey, string advDef)
        => MainCore.Conf.AdvancedTooltip
            ? $"{Core.Tr.Get(key, def)}\n--\n{Core.Tr.Get(advKey, advDef)}"
            : Core.Tr.Get(key, def);

    private static void UpdateInputBlockerState(bool enable) {
        if (_inputBlockerObject != null) {
            _inputBlockerObject.SetActive(enable);
        }
    }
}