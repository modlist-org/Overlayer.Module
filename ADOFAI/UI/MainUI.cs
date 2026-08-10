using Overlayer.Localization;
using Overlayer.Module.ADOFAI.IO;
using Overlayer.Module.ADOFAI.Patch;
using Overlayer.Patch.Safe;
using Overlayer.UI.Factory;
using Overlayer.UI.Generator;
using Overlayer.UI.Objects;
using Overlayer.UI.Objects.Impl;
using Overlayer.UI.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Overlayer.Module.ADOFAI.UI;

public static class MainUI {
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
    }

    public static void CreateMenu(RectTransform parent)
        => MenuFactory.CreateItem(parent, "ADOFAI", Core.Spr.Get("Image.ADOFAI.png"), 100)
        .label.gameObject.AddComponent<TextLocalization>().Init("ADOFAI", "ADOFAI", Core.Tr);

    private static readonly Dictionary<string, UIObject> objects = [];

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

        _ = GenerateUI.AddTextH1(GenerateUI.Row(content.transform))
           .gameObject.AddComponent<TextLocalization>()
           .Init("ADOFAI", "ADOFAI", Core.Tr);

        if (Application.platform == RuntimePlatform.LinuxPlayer) {
            UIToggle linuxTextInputToggle = GenerateUI.Toggle(
                GenerateUI.Row(content.transform),
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
            linuxTextInputToggle.OnlyModOn = true;
            linuxTextInputToggle.Label.gameObject.AddComponent<TextLocalization>().Init("LINUX_TEXT_INPUT_FIX", "Linux Text Input Fix", Core.Tr);
            objects[linuxTextInputToggle.Id] = linuxTextInputToggle;
            linuxTextInputToggle.Rect.AddToolTip(
                "DESC_LINUX_TEXT_INPUT_FIX",
                "Fixes duplicate characters and Shift-modified text input on Linux",
                Core.Tr
            );
        }

        UIToggle blockInputToggle = GenerateUI.Toggle(
            GenerateUI.Row(content.transform),
            defSet.BlockInputWhenOpened,
            Core.Config.BlockInputWhenOpened,
            toggle => {
                Core.Config.BlockInputWhenOpened = toggle;
                Core.ConfigFile.RequestSave();

                ApplyState(SafePatchController.Get<SP_BlockAsyncInput>(), toggle);
                ApplyState(SafePatchController.Get<SP_BlockLegacyInput>(), toggle);
                ApplyState(SafePatchController.Get<SP_BlockInputMethod>(), toggle);
                ApplyState(SafePatchController.Get<SP_BlockDirectInput>(), toggle);
            },
            "Block Input When Opened",
            "block_input_when_opened"
        );
        blockInputToggle.OnlyModOn = true;
        blockInputToggle.Label.gameObject.AddComponent<TextLocalization>().Init("BLOCK_INPUT_WHEN_OPENED", "Block Input When Opened", Core.Tr);
        objects[blockInputToggle.Id] = blockInputToggle;
        blockInputToggle.Rect.AddToolTip(
            "DESC_BLOCK_INPUT_WHEN_OPENED",
            "Blocks game inputs while the Overlayer UI is opened",
            Core.Tr
        );

        UIToggle showAutoJudgmentToggle = GenerateUI.Toggle(
            GenerateUI.Row(content.transform),
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
        showAutoJudgmentToggle.OnlyModOn = true;
        showAutoJudgmentToggle.Label.gameObject.AddComponent<TextLocalization>().Init("SHOW_AUTOPLAY_JUDGMENT", "Show Autoplay Judgment", Core.Tr);
        objects[showAutoJudgmentToggle.Id] = showAutoJudgmentToggle;
        showAutoJudgmentToggle.Rect.AddToolTip(
            "DESC_SHOW_AUTOPLAY_JUDGMENT",
            "Applies a patch to show the true judgment in AutoPlay on the Hit Error Meter",
            Core.Tr
        );

        UIToggle hideTitleToggle = GenerateUI.Toggle(
            GenerateUI.Row(content.transform),
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
        hideTitleToggle.OnlyModOn = true;
        hideTitleToggle.Label.gameObject.AddComponent<TextLocalization>().Init("HIDE_TITLE", "Hide Title", Core.Tr);
        objects[hideTitleToggle.Id] = hideTitleToggle;
        hideTitleToggle.Rect.AddToolTip(
            "DESC_HIDE_TITLE",
            "Hides in-game level titles using GCS setting",
            Core.Tr
        );
        return;

        static void ApplyState<T>(T[] patches, bool enable) where T : SafeConditionalPatch {
            foreach (var patch in patches) {
                if (enable) patch.Apply();
                else patch.Remove();
            }
        }
    }
}
