using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace LynxPyon {
    public enum MainTab { Blackjack, Wheel, Lounge, Config, About }
    public enum NameMode { First, Last, Both }

    [Serializable]
    public class Config : IPluginConfiguration {
        public int Version { get; set; } = 0;
        public bool Debug { get; set; } = false;
        public string RollCommand { get; set; } = "/dice"; // /random
        public string ChatChannel { get; set; } = "/p"; // /s
        public bool AutoParty { get; set; } = true;
        public NameMode AutoNameMode { get; set; } = NameMode.First;

        public BlackjackConfig Blackjack { get; set; } = new BlackjackConfig();
        public LoungeConfig Lounge { get; set; } = new LoungeConfig();
        public WheelConfig Wheel { get; set; } = new WheelConfig();

        [NonSerialized] private DalamudPluginInterface? PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            PluginInterface = pluginInterface;
        }

        public void Save() {
            PluginInterface!.SavePluginConfig(this);
        }
    }
}
