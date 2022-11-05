using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface;
using ImGuiNET;
using LynxPyon.Extensions;

namespace LynxPyon.Modules {
    public class Wheel {
        private readonly MainWindow MainWindow;
        public event EventHandler<MessageEventArgs> Send_Message;
        public event EventHandler<EventArgs> Close_Window;

        private Config Config;

        private SubTab CurrentSubTab = SubTab.Game;
        private enum SubTab { Game, Config, Guide }

        private int BetAmount = 20000;
        private int SpinAmount = 1;
        private string WheelResults = "";
        private int HouseProfit = 0;

        public Wheel(MainWindow mainWindow) {
            MainWindow = mainWindow;
            Config = MainWindow.Config;
            Send_Message += MainWindow.Send_Message;
            Close_Window += MainWindow.Close_Window;
        }

        public void Dispose() {
            Close_Window -= MainWindow.Close_Window;
            Send_Message -= MainWindow.Send_Message;
        }

        private string FormatMessage(string message) {
            if(string.IsNullOrWhiteSpace(message)) { return ""; }

            return message.Replace("#totalbet#", $"{(BetAmount * SpinAmount).ToString("N0")}")
                .Replace("#spins#", SpinAmount.ToString())
                .Replace("#spinstr#", SpinAmount == 1 ? "spin" : "spins")
                .Replace("#minbet#", Config.Wheel.MinBet.ToString("N0"))
                .Replace("#maxbet#", Config.Wheel.MaxBet.ToString("N0"));
        }

        private void SendMessage(string message) {
            Send_Message(this, new MessageEventArgs(message, MessageType.Normal, MainTab.Wheel));
        }

        public void DrawSubTabs() {
            if(ImGui.BeginTabBar("WheelSubTabBar", ImGuiTabBarFlags.NoTooltip)) {
                if(ImGui.BeginTabItem("Game###GamblePyon_WheelGame_SubTab")) {
                    CurrentSubTab = SubTab.Game;
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("Config###GamblePyon_WheelConfig_SubTab")) {
                    CurrentSubTab = SubTab.Config;
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("Guide###GamblePyon_WheelGuide_SubTab")) {
                    CurrentSubTab = SubTab.Guide;
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
                ImGui.Spacing();
            }

            switch(CurrentSubTab) {
                case SubTab.Game: {
                        DrawGame();
                        break;
                    }
                case SubTab.Config: {
                        DrawConfig();
                        break;
                    }
                case SubTab.Guide: {
                        DrawGuide();
                        break;
                    }
                default:
                    DrawGame();
                    break;
            }
        }

        private void DrawGame() {
            DrawGameMain();

            ImGui.PopID();
            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

            if(ImGui.Button("Close")) {
                Close_Window(this, new EventArgs());
            }
        }

        private void DrawGameMain() {
            ImGui.Columns(4);
            ImGui.SetColumnWidth(0, 100 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(1, 100 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(2, 100 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(3, 100 + 5 * ImGuiHelpers.GlobalScale);

            ImGui.TextColored(ImGuiColors.DalamudGrey, "Bet per Spin");
            ImGuiEx.InputText("###betAmount", ref BetAmount);
            ImGui.NextColumn();

            ImGui.TextColored(ImGuiColors.DalamudGrey, "Spins");
            ImGuiEx.InputText("###spinAmount", ref SpinAmount);
            ImGui.NextColumn();

            ImGui.TextColored(ImGuiColors.DalamudGrey, "Bet Total");
            ImGui.TextColored(ImGuiColors.DalamudGrey, $"{BetAmount * SpinAmount}");
            ImGui.NextColumn();

            ImGui.TextColored(ImGuiColors.DalamudGrey, "House Profit");
            ImGuiEx.InputText("###houseProfit", ref HouseProfit);
            ImGui.NextColumn();

            ImGui.Separator();

            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 100 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(1, 100 + 5 * ImGuiHelpers.GlobalScale);

            ImGui.TextColored(ImGuiColors.DalamudGrey, "Wheel Results");
            ImGui.NextColumn();
            ImGui.Dummy(new Vector2(15));
            ImGui.NextColumn();

            ImGui.Separator();

            ImGui.InputTextMultiline("###wheelResults", ref WheelResults, 1000, new Vector2(90, 150));
            ImGui.NextColumn();

            string[] lines = WheelResults.Split('\n');
            List<string> Winnings = new List<string>();
            float totalMulti = 0f;
            int prizeCount = 0;
            int catnipCount = 0;

            foreach(string line in lines) {
                if(string.IsNullOrWhiteSpace(line)) { continue; }
                float multi = 0f;
                string item = line.Trim();

                if(line.Contains('x')) {
                    float.TryParse(item.Replace("x", ""), out multi);
                }

                if(item == "Prize") {
                    prizeCount++;
                } else if(item == "Catnip") {
                    catnipCount++;
                }

                if(multi != 0f) {
                    totalMulti += multi;
                } else if(!Winnings.Contains(item)) {
                    Winnings.Add(item);
                }
            }

            if(ImGui.Button("Advertise")) {
                SendMessage($"{FormatMessage(Config.Wheel.Messages["WheelAd"])}");
            }
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Send message advertising to spin."); }

            if(ImGui.Button("Target Play")) {
                SendMessage($"{FormatMessage(Config.Wheel.Messages["WheelPlay"])}");
            }
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Send message announcing target is playing."); }

            if(Winnings.Contains("Bonus")) {
                if(ImGui.Button("Bonus Choice")) {
                    SendMessage($"{FormatMessage(Config.Wheel.Messages["BonusChoice"])}");
                }
                if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Send message announcing target landed on Bonus."); }

                if(ImGui.Button("Bonus Win")) {
                    SendMessage($"{FormatMessage(Config.Wheel.Messages["BonusWin"])}");
                }
                if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Send message announcing target won Bonus."); }
            }

            if(Winnings.Contains("Prize")) {
                if(ImGui.Button("Tell Prize Pool")) {
                    SendMessage($"{FormatMessage(Config.Wheel.Messages["PrizePool"])}");
                }
                if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Send message to target informing of prize pool."); }
            }

            ImGui.NextColumn();

            ImGui.Columns(1);
            ImGui.Separator();

            if(Winnings.Count != 0 || totalMulti != 0) {
                Winnings.Remove("Bonus");
                string result = $"/tell <t> Result: {totalMulti}x ({(BetAmount * totalMulti).ToString("N0")}), {string.Join(", ", Winnings)}";
                if(prizeCount > 0) {
                    result = result.Replace("Prize", $"Prize x{prizeCount}");
                    result = result.Replace("Catnip", $"Catnip x{catnipCount}");
                }
                ImGui.InputText("###result", ref result, 255);

                ImGui.Separator();
                if(ImGui.Button("Update House Profit")) {
                    HouseProfit += (BetAmount * SpinAmount) - (int)(BetAmount * totalMulti);
                }
            }
        }

        private void DrawConfig() {
            DrawConfigGameSetup();

            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

            if(ImGui.Button("Save")) {
                Config.Save();
            }
            ImGui.SameLine();
            if(ImGui.Button("Close")) {
                Close_Window(this, new EventArgs());
            }

            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

            DrawConfigMessages();

            ImGui.PopID();
            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

            if(ImGui.Button("Save")) {
                Config.Save();
            }
            ImGui.SameLine();
            if(ImGui.Button("Close")) {
                Close_Window(this, new EventArgs());
            }
        }

        private void DrawConfigGameSetup() {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Wheel Setup");

            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 180 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(1, 500 + 5 * ImGuiHelpers.GlobalScale);

            ImGui.Separator();

            ImGui.Text("Min Bet");
            ImGui.NextColumn();
            ImGui.Text("Max Bet");
            ImGui.NextColumn();

            ImGui.Separator();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputInt("###minBet", Config.Wheel, nameof(Config.Wheel.MinBet));
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(180);
            ImGuiEx.InputInt("###maxBet", Config.Wheel, nameof(Config.Wheel.MaxBet));
            ImGui.NextColumn();
        }

        private void DrawConfigMessages() {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Localization");
            ImGui.TextWrapped("Adjust text to be output for various events.\nKeywords: #minbet#, #maxbet#, #totalbet#, #spins#, #spinstr#");

            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 180 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(1, 500 + 5 * ImGuiHelpers.GlobalScale);

            ImGui.Separator();

            ImGui.Text("Descriptor");
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.Text("Text");
            ImGui.NextColumn();

            ImGui.Separator();

            foreach(var message in Config.Wheel.Messages) {
                ImGui.PushID($"m_{message.Key}");
                ImGui.SetNextItemWidth(-1);
                ImGui.Text(message.Key);
                ImGui.NextColumn();
                ImGui.SetNextItemWidth(-1);
                string msg = message.Value;
                ImGui.InputText($"###m{message.Key}", ref msg, 255);
                Config.Wheel.Messages[message.Key] = msg;
                ImGui.NextColumn(); ImGui.Separator();
            }
        }

        private void DrawGuide() {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Wheel Guide");
            ImGui.Separator();
            ImGui.TextWrapped("todo...");
            ImGui.Separator();

            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);
            if(ImGui.Button("Close")) {
                Close_Window(this, new EventArgs());
            }
        }
    }
}
