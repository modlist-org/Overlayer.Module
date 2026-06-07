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
using System.IO;
using System.Reflection;

namespace Overlayer.Module.ADOFAI;

public class Core : OverlayerModule {
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

        SafePatchController.Add(new SP_ShowAutoJudgment());
        SafePatchController.ApplyAll();

        MainUI.CreateMenu(UICore.MenuContent);
        MainUI.CreatePage(PageFactory.CreatePageBase(100));
    }

    public override void OnDispose() {
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