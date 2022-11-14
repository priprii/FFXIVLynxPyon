using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using LynxPyon.Extensions;
using LynxPyon.Modules;

namespace LynxPyon {
    public class MainWindow : Window {
        private readonly LynxPyon plugin;
        public static Config Config { get; set; }

        private MainTab CurrentMainTab = MainTab.Blackjack;

        public PlayerManager? PlayerManager;
        public Blackjack? Blackjack;
        public Wheel? Wheel;
        public Lounge? Lounge;

        public MainWindow(LynxPyon plugin) : base("LynxPyon") {
            this.SizeCondition = ImGuiCond.Appearing;
            this.Size = new Vector2(670, 500) * ImGuiHelpers.GlobalScale;
            this.plugin = plugin;
        }

        public void Initialize() {
            Blackjack = new Blackjack(this);
            Wheel = new Wheel(this);
            Lounge = new Lounge(this);
        }

        public void Dispose() {
            Blackjack?.Dispose();
            Wheel?.Dispose();
            Lounge?.Dispose();
            PlayerManager?.Dispose();
        }

        public override void OnOpen() {
            base.OnOpen();
        }

        public void Close_Window(object? sender, System.EventArgs e) => IsOpen = false;
        public void Send_Message(object? sender, MessageEventArgs e) => SendMessage(e.Message, e.MessageType, e.ModuleTab);

        private List<string> QueuedMessages = new List<string>();
        private void SendMessage(string message, MessageType messageType, MainTab moduleTab) {
            if(!string.IsNullOrWhiteSpace(message)) {
                switch(moduleTab) {
                    case MainTab.Blackjack:
                        if(messageType == MessageType.Normal) {
                            QueuedMessages.Add($"{(Config.Debug ? "/echo" : Config.ChatChannel)} {message}");
                        } else if(messageType == MessageType.BlackjackRoll) {
                            int.TryParse(message, out int num);

                            if(Config.Debug) {
                                string n = (new System.Random().Next(num == 0 ? Config.Blackjack.MaxRoll : num) + 1).ToString();
                                string s = Config.ChatChannel == "/p" ? $"Random! (1-{num}) {n}" : $"Random! You roll a {n} (out of {num}).";
                                ReceivedMessage(Blackjack.Dealer.Name, s);
                            } else {
                                QueuedMessages.Add($"{Config.RollCommand} {(num == 0 ? Config.Blackjack.MaxRoll : num)}");
                            }
                        }
                        break;
                    case MainTab.Wheel:
                        QueuedMessages.Add($"{message}");
                        break;
                }
            }
        }

        public void OnChatMessage(XivChatType type, uint senderId, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled) {
            if(isHandled) { return; }

            ReceivedMessage(sender.TextValue, message.TextValue);
        }

        private void ReceivedMessage(string sender, string message) {
            if(Blackjack != null && Blackjack.Enabled) {
                Blackjack.OnChatMessage(sender, message);
            }
        }

        public override void Draw() {
            DrawMainTabs();

            switch(CurrentMainTab) {
                case MainTab.Blackjack: {
                        Blackjack?.DrawSubTabs();
                        break;
                    }
                case MainTab.Wheel: {
                        Wheel?.DrawSubTabs();
                        break;
                    }
                case MainTab.Lounge: {
                        Lounge?.DrawSubTabs();
                        break;
                    }
                case MainTab.Config: {
                        DrawMainConfig();
                        break;
                    }
                case MainTab.About: {
                        DrawAbout();
                        break;
                    }
                default:
                    Blackjack?.DrawSubTabs();
                    break;
            }

            if(QueuedMessages.Count > 0) {
                LynxPyon.XIVCommon.Functions.Chat.SendMessage(QueuedMessages[0]);
                QueuedMessages.RemoveAt(0);
            }

            if(Config.ChatChannel == "/p" && Config.AutoParty) {
                if(Blackjack != null && Blackjack.Players != null && Blackjack.Enabled) {
                    if(PlayerManager == null) {
                        PlayerManager = new PlayerManager();
                    }

                    PlayerManager.UpdateParty(ref Blackjack.Players, LynxPyon.ClientState.LocalPlayer.Name.TextValue, Config.AutoNameMode);
                }
            }

            if(Lounge != null && Lounge.Enabled) {
                if(PlayerManager == null) {
                    PlayerManager = new PlayerManager();
                }

                PlayerManager.UpdatePlayers(ref Lounge.Customers);
            }
        }

        private void DrawMainTabs() {
            if(ImGui.BeginTabBar("GamblePyonMainTabBar", ImGuiTabBarFlags.NoTooltip)) {
                if(ImGui.BeginTabItem("Blackjack###GamblePyon_Blackjack_MainTab")) {
                    CurrentMainTab = MainTab.Blackjack;
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("Wheel###GamblePyon_Wheel_MainTab")) {
                    CurrentMainTab = MainTab.Wheel;
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("Lounge###GamblePyon_Lounge_MainTab")) {
                    CurrentMainTab = MainTab.Lounge;
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("Main Config###GamblePyon_Config_MainTab")) {
                    CurrentMainTab = MainTab.Config;
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("About###GamblePyon_About_MainTab")) {
                    CurrentMainTab = MainTab.About;
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
                ImGui.Spacing();
            }
        }

        private void DrawMainConfig() {
            ImGui.Text("Channel");

            if(ImGui.RadioButton("Party", Config.ChatChannel == "/p")) {
                Config.ChatChannel = "/p";
                Config.RollCommand = "/dice";
                Blackjack?.InitializePlayers();
            }
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Play in private /p chat using /dice roll.");
            }
            ImGui.SameLine();
            if(ImGui.RadioButton("Say", Config.ChatChannel == "/s")) {
                Config.ChatChannel = "/s";
                Config.RollCommand = "/random";
                Blackjack?.InitializePlayers();
            }
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Play in public /s chat using /random roll.");
            }

            ImGui.Separator();
            if(ImGuiEx.Checkbox("Auto Party", Config, nameof(Config.AutoParty))) {
                Blackjack?.InitializePlayers();
            }
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Automatically update participating players by monitoring the party list.\nThis of course is only available while playing in Party.\nI haven't tested whether this works, because I didn't have a party while making this ;w;\nProbably does work though, fingers crossed!!");
            }
            ImGui.SameLine();
            if(ImGui.RadioButton("First Name", Config.AutoNameMode == NameMode.First)) {
                Config.AutoNameMode = NameMode.First;
                if(Config.AutoParty) { Blackjack?.InitializePlayers(); }
            }
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("With Auto Party enabled, Alias of players will be set to their first name.");
            }
            ImGui.SameLine();
            if(ImGui.RadioButton("Last Name", Config.AutoNameMode == NameMode.Last)) {
                Config.AutoNameMode = NameMode.Last;
                if(Config.AutoParty) { Blackjack?.InitializePlayers(); }
            }
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("With Auto Party enabled, Alias of players will be set to their last name.\nBit weird to refer to people by their last name only, isn't it?\nSome people call me Pyon, so I guess it's not too weird.");
            }
            ImGui.SameLine();
            if(ImGui.RadioButton("Both", Config.AutoNameMode == NameMode.Both)) {
                Config.AutoNameMode = NameMode.Both;
                if(Config.AutoParty) { Blackjack?.InitializePlayers(); }
            }
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("With Auto Party enabled, Alias of players will be set to both their first & last name.");
            }

            ImGui.Separator();
            ImGuiEx.Checkbox("Debug", Config, nameof(Config.Debug));
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Output messages to /echo chat & roll internally.\nThis is just here so I can test things without spamming chat!");
            }

            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

            if(ImGui.Button("Save")) {
                Config.Save();
            }
            ImGui.SameLine();
            if(ImGui.Button("Close")) {
                IsOpen = false;
            }
        }

        private void DrawAbout() {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "About");
            ImGui.TextWrapped("This plugin is developed by Primu Pyon@Omega");
            ImGui.TextWrapped("Intended for exclusive use by staff working for Emerald Lynx");
            ImGui.Separator();
            ImGui.TextWrapped("If you'd like to support me, send me a gift :3");

            ImGui.Separator();
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Change Log");
            ImGui.TextWrapped("1.0.0.5 ~ 2022.11.14\n" +
                "- Added rules messages that can be customized & output.\n" +
                "- Fixed profit values, they'll now only display the actual gain/loss.\n" +
                "- Added splitting option when player's first 2 cards are a pair, can be toggled with the 'Allow Split' option in config. This required a lot of code restructuring so there might be bugs, probably not though!! Some hard-coded rules for splitting: Split hand with natural blackjack is treated as a normal win, split hand cannot be split again & bet from split hands cannot be pushed.\n" +
                "- The above also means there are more message fields specific to splitting.\n" +
                "- Changed cards/value input fields to readonly as you can't modify them manually anyway.\n" +
                "- Also changed the Bet Amount field to readonly when appropriate.\n" +
                "- With the above change, removed the 'auto double' option as it's not necessary, bet is always automatically calculated for double/split functions.\n" +
                "- Fixed some minor errors.");

            ImGui.TextWrapped("1.0.0.4 ~ 2022.11.10\n" +
                "- Added this change log!\n" +
                "- Implemented automatic way of handling Blackjack dealer name, so different name display types are now properly supported.");

            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);
            if(ImGui.Button("Close")) {
                IsOpen = false;
            }
        }
    }
}
