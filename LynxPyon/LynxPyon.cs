using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using XivCommon;

namespace LynxPyon {
    public sealed class LynxPyon : IDalamudPlugin {
        public string Name => "LynxPyon";
        private const string CommandName = "/pyon";
        private const string AltCommandName = "/lynx";

        [PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static ChatGui ChatGui { get; private set; } = null!;
        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] public static Framework Framework { get; private set; } = null!;
        [PluginService] public static ObjectTable Objects { get; private set; } = null!;
        [PluginService] public static SigScanner SigScanner { get; private set; } = null!;

        private WindowSystem Windows;
        private static MainWindow MainWindow;

        public static XivCommonBase XIVCommon;

        public LynxPyon() {
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
                HelpMessage = "Open plugin window."
            });
            CommandManager.AddHandler(AltCommandName, new CommandInfo(OnCommand) {
                HelpMessage = "Does the same thing!"
            });

            PluginInterface.UiBuilder.OpenConfigUi += () => {
                MainWindow.IsOpen = true;
            };

            XIVCommon = new XivCommonBase();
            Windows = new WindowSystem(Name);
            MainWindow = new MainWindow(this) { IsOpen = false };
            MainWindow.Config = PluginInterface.GetPluginConfig() as Config ?? new Config();
            MainWindow.Config.Initialize(PluginInterface);
            Windows.AddWindow(MainWindow);
            MainWindow.Initialize();

            PluginInterface.UiBuilder.Draw += Windows.Draw;
            ChatGui.ChatMessage += MainWindow.OnChatMessage;
        }

        public void Dispose() {
            PluginInterface.UiBuilder.Draw -= Windows.Draw;
            ChatGui.ChatMessage -= MainWindow.OnChatMessage;
            MainWindow.Dispose();
            CommandManager.RemoveHandler(CommandName);
            XIVCommon.Dispose();
        }

        private void OnCommand(string command, string args) {
            MainWindow.IsOpen = true;
        }
    }
}