using Overlayer.Compat;
using Overlayer.Core;
using Overlayer.IO;
using Overlayer.Localization;
using Overlayer.Module.ADOFAI.IO;
using Overlayer.Module.ADOFAI.Patch;
using Overlayer.Module.ADOFAI.UI;
using Overlayer.ModuleAPI;
using Overlayer.Patch.Safe;
using Overlayer.Resource;
using Overlayer.UI;
using Overlayer.UI.Factory;
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Overlayer.Module.ADOFAI;

public class Core : OverlayerModule {
    private IDisposable? playbackStateRegistration;
    private IDisposable? textFontRegistration;
    private static TMP_FontAsset? defaultTextFont;
    public static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();
    public static OverlayerLogger Logger { get; } = new(MainCore.Host.OverlayerLogger, "ADOFAI Module");
    public static SettingsFile<ADOFAISettings> ConfigFile { get; } = new(Path.Combine(MainCore.Paths.ModulePath, "ADOFAI/Settings.json"));
    public static ADOFAISettings Config => ConfigFile.Data;
    public static Translator Tr { get; private set; } = new();

    public static ResourceManager Res { get; } = new(Assembly, "Overlayer.Module.ADOFAI.Resource.Embedded.");
    public static SpriteManager Spr { get; } = new(Res);

    private static void LoadTr()
        => _ = Tr.Load(new(Path.Combine(MainCore.Paths.ModulePath, "ADOFAI/Lang")));

    private void OnLanguageChanged(string lang)
        => Tr.Language = lang;

    public override void OnInitialize() {
        Tr.SetLog(Logger.Msg);

        MainCore.Tr.OnLoadStart += LoadTr;
        MainCore.Tr.OnLanguageChanged += OnLanguageChanged;

        LoadTr();
        Tr.Language = MainCore.Tr.Language;

        ConfigFile.Load();

        playbackStateRegistration = PlaybackState.Register(() => {
            scrController controller = ADOBase.controller;
            return controller != null && controller.gameworld;
        });
        textFontRegistration = TextFontProvider.Register(() => {
            if(defaultTextFont == null) {
                Font sourceFont = RDString.GetFontDataForLanguage(SystemLanguage.English).font;
                defaultTextFont = TMP_FontAsset.CreateFontAsset(
                    sourceFont,
                    100,
                    10,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024
                );
            }
            return defaultTextFont;
        });

        SafePatchController.Add(new SP_BlockAsyncInput());
        SafePatchController.Add(new SP_BlockLegacyInput());
        SafePatchController.Add(new SP_LinuxTMPKeyInput());
        SafePatchController.Add(new SP_LinuxLegacyKeyInput());
        SafePatchController.Add(new SP_ShowAutoJudgment());
        SafePatchController.Add(new SP_RecordJudgment());
        SafePatchController.Add(new SP_ResetTagState());
        SafePatchController.Add(new SP_RevertTagState());
        SafePatchController.Add(new SP_RecordTiming());
        SafePatchController.ApplyAll();

        MainUI.CreateMenu(UICore.MenuContent);
        MainUI.CreatePage(PageFactory.CreatePageBase(100));
    }

    public override void OnDispose() {
        playbackStateRegistration?.Dispose();
        playbackStateRegistration = null;
        textFontRegistration?.Dispose();
        textFontRegistration = null;

        Spr.Dispose();
        Res.Dispose();

        MainCore.Tr.OnLoadStart -= LoadTr;
        MainCore.Tr.OnLanguageChanged -= OnLanguageChanged;

        ConfigFile.Save();
    }

    public override string Name => Info.Name;
    public override string Author => Info.Author;
    public override string Version => Info.Version;
}
