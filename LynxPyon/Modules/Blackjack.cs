﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;
using LynxPyon.Extensions;
using LynxPyon.Models;

namespace LynxPyon.Modules {
    public class Blackjack {
        private readonly MainWindow MainWindow;
        public event EventHandler<MessageEventArgs> Send_Message;
        public event EventHandler<EventArgs> Close_Window;
        public bool Enabled = false;

        private Config Config;

        private SubTab CurrentSubTab = SubTab.Game;
        private enum SubTab { Game, Config, Guide }

        private Action CurrentAction = Action.None;
        private enum Action { None, DealerDraw1, DealerDraw2, DealerHit, PlayerDraw2, PlayerHit }

        private Event CurrentEvent = Event.PlaceBets;
        private enum Event { PlaceBets, BetsPlaced, CardActions }

        private Player? CurrentPlayer;
        public Player? Dealer;
        public List<Player>? Players;

        public Blackjack(MainWindow mainWindow) {
            MainWindow = mainWindow;
            Config = MainWindow.Config;
            Send_Message += MainWindow.Send_Message;
            Close_Window += MainWindow.Close_Window;
            Initialize();
        }

        public void Dispose() {
            Close_Window -= MainWindow.Close_Window;
            Send_Message -= MainWindow.Send_Message;
        }

        public void Initialize() {
            Dealer = new Player(0);
            Dealer.Name = LynxPyon.ClientState.LocalPlayer?.Name.TextValue ?? "";
            Dealer.Alias = Dealer.GetName(Config.AutoNameMode);
            InitializePlayers();
        }

        public void InitializePlayers() {
            Players = new List<Player>();
            if(Config.ChatChannel != "/p" || !Config.AutoParty) {
                Players.Add(new Player(0));
                Players.Add(new Player(1));
                Players.Add(new Player(2));
                Players.Add(new Player(3));
                Players.Add(new Player(4));
                Players.Add(new Player(5));
                Players.Add(new Player(6));
            }
        }

        public void ResetRound() {
            EndRound();
            Dealer.Reset();

            foreach(Player player in Players) {
                player.Reset();
            }
        }

        private void EndRound() {
            CurrentEvent = Event.PlaceBets;
        }

        private string FormatMessage(string message, Player player) {
            if(string.IsNullOrWhiteSpace(message)) { return ""; }

            return message.Replace("#dealer#", player.Alias)
                .Replace("#player#", player.Alias)
                .Replace("#firstname#", player.GetName(NameMode.First))
                .Replace("#lastname#", player.GetName(NameMode.Last))
                .Replace("#bet#", player.Bet.ToString("N0"))
                .Replace("#cards#", player.Blackjack.GetCards())
                .Replace("#value#", player.Blackjack.AceLowValue != 0 ? (player.Blackjack.AceHighValue < 21 ? $"{player.Blackjack.AceLowValue}/{player.Blackjack.AceHighValue}" : player.Blackjack.AceHighValue == 21 ? $"{player.Blackjack.AceHighValue}" : $"{player.Blackjack.AceLowValue}") : player.Blackjack.GetStrValue())
                .Replace("#stand#", Config.Blackjack.DealerStandMode == DealerStandMode._16 ? "16" : Config.Blackjack.DealerStandMode == DealerStandMode._17 ? "17" : "None")
                .Replace("#winnings#", player.Winnings.ToString("N0"))
                .Replace("#profit#", player.TotalWinnings.ToString("N0"))
                .Replace("#minbet#", Config.Blackjack.MinBet.ToString("N0"))
                .Replace("#maxbet#", Config.Blackjack.MaxBet.ToString("N0"));
        }

        private void SendMessage(string message) {
            if(Enabled) {
                Send_Message(this, new MessageEventArgs(message, MessageType.Normal, MainTab.Blackjack));
            }
        }

        private void SendRoll() {
            if(Enabled) {
                Send_Message(this, new MessageEventArgs(Config.Blackjack.MaxRoll.ToString(), MessageType.BlackjackRoll, MainTab.Blackjack));
            }
        }

        public void OnChatMessage(string sender, string message) {
            if(!Enabled) { return; }

            try {
                if((Config.RollCommand == "/dice" && sender.ToLower().Contains(Dealer.Name.ToLower()) && message.Contains("Random! (1-13)")) || (Config.RollCommand == "/random" && message.Contains("You roll a") && message.Contains("out of 13"))) {
                    if(CurrentAction == Action.DealerDraw1) {
                        CurrentAction = Action.None;

                        string cardValue = Config.RollCommand == "/dice" ? message.Replace("Random! (1-13) ", "") : Regex.Replace(message, ".*You roll a ([^\\(]+)\\(.*", "$1", RegexOptions.Singleline).Trim();
                        Dealer.Blackjack.Cards.Add(new Card(int.Parse(cardValue), Config.Blackjack.ShowSuit));

                        SendMessage($"{FormatMessage(Config.Blackjack.Messages["DealerDraw1"], Dealer)}");
                    } else if(CurrentAction == Action.DealerDraw2) {
                        CurrentAction = Action.None;

                        string cardValue = Config.RollCommand == "/dice" ? message.Replace("Random! (1-13) ", "") : Regex.Replace(message, ".*You roll a ([^\\(]+)\\(.*", "$1", RegexOptions.Singleline).Trim();
                        Dealer.Blackjack.Cards.Add(new Card(int.Parse(cardValue), Config.Blackjack.ShowSuit));

                        int value = Dealer.Blackjack.GetIntValue();
                        if(value == 21) {
                            SendMessage($"{FormatMessage(Config.Blackjack.Messages["DealerDraw2Blackjack"], Dealer)}");
                        } else if(Config.Blackjack.DealerStandMode != DealerStandMode.None && value >= GetStandValue()) {
                            SendMessage($"{FormatMessage(Config.Blackjack.Messages["DealerDraw2Stand"], Dealer)}");
                        } else if(Config.Blackjack.DealerStandMode != DealerStandMode.None) {
                            SendMessage($"{FormatMessage(Config.Blackjack.Messages["DealerDraw2UnderStand"], Dealer)}");
                        } else {
                            SendMessage($"{FormatMessage(Config.Blackjack.Messages["DealerDraw2NoStandReq"], Dealer)}");
                        }
                    } else if(CurrentAction == Action.DealerHit) {
                        CurrentAction = Action.None;

                        string cardValue = Config.RollCommand == "/dice" ? message.Replace("Random! (1-13) ", "") : Regex.Replace(message, ".*You roll a ([^\\(]+)\\(.*", "$1", RegexOptions.Singleline).Trim();
                        Dealer.Blackjack.Cards.Add(new Card(int.Parse(cardValue), Config.Blackjack.ShowSuit));

                        int value = Dealer.Blackjack.GetIntValue();
                        if(value == 21) {
                            SendMessage($"{FormatMessage(Config.Blackjack.Messages["DealerHit21"], Dealer)}");
                        } else if(Config.Blackjack.DealerStandMode != DealerStandMode.None && value >= GetStandValue() && value < 21) {
                            SendMessage($"{FormatMessage(Config.Blackjack.Messages["DealerHitStand"], Dealer)}");
                        } else if(Config.Blackjack.DealerStandMode != DealerStandMode.None && value < GetStandValue()) {
                            SendMessage($"{FormatMessage(Config.Blackjack.Messages["DealerHitUnderStand"], Dealer)}");
                        } else if(Config.Blackjack.DealerStandMode == DealerStandMode.None && value < 21) {
                            SendMessage($"{FormatMessage(Config.Blackjack.Messages["DealerHitNoStandReq"], Dealer)}");
                        } else {
                            SendMessage($"{FormatMessage(Config.Blackjack.Messages["DealerOver21"], Dealer)}");
                        }
                    } else if(CurrentAction == Action.PlayerDraw2) {
                        if(CurrentPlayer != null) {
                            string cardValue = Config.RollCommand == "/dice" ? message.Replace("Random! (1-13) ", "") : Regex.Replace(message, ".*You roll a ([^\\(]+)\\(.*", "$1", RegexOptions.Singleline).Trim();
                            CurrentPlayer.Blackjack.Cards.Add(new Card(int.Parse(cardValue), Config.Blackjack.ShowSuit));

                            if(CurrentPlayer.Blackjack.Cards.Count == 2) {
                                CurrentAction = Action.None;

                                int value = CurrentPlayer.Blackjack.GetIntValue();
                                if(value == 21) {
                                    SendMessage($"{FormatMessage(Config.Blackjack.Messages["PlayerDraw2Blackjack"], CurrentPlayer)}");
                                } else {
                                    SendMessage($"{FormatMessage(Config.Blackjack.Messages["PlayerDraw2"], CurrentPlayer)}");
                                }

                                CurrentPlayer = null;
                            }
                        }
                    } else if(CurrentAction == Action.PlayerHit) {
                        CurrentAction = Action.None;

                        if(CurrentPlayer != null) {
                            string cardValue = Config.RollCommand == "/dice" ? message.Replace("Random! (1-13) ", "") : Regex.Replace(message, ".*You roll a ([^\\(]+)\\(.*", "$1", RegexOptions.Singleline).Trim();
                            CurrentPlayer.Blackjack.Cards.Add(new Card(int.Parse(cardValue), Config.Blackjack.ShowSuit));

                            int value = CurrentPlayer.Blackjack.GetIntValue();
                            if(value == 21) {
                                SendMessage($"{FormatMessage(Config.Blackjack.Messages["PlayerHit21"], CurrentPlayer)}");
                            } else if(value < 21) {
                                if(CurrentPlayer.Blackjack.Doubled) {
                                    SendMessage($"{FormatMessage(Config.Blackjack.Messages["PlayerHitUnder21Doubled"], CurrentPlayer)}");
                                } else {
                                    SendMessage($"{FormatMessage(Config.Blackjack.Messages["PlayerHitUnder21"], CurrentPlayer)}");
                                }
                            } else {
                                SendMessage($"{FormatMessage(Config.Blackjack.Messages["PlayerHitOver21"], CurrentPlayer)}");
                            }

                            CurrentPlayer = null;
                        }
                    }
                }
            } catch { }
        }

        private int GetStandValue() {
            switch(Config.Blackjack.DealerStandMode) {
                case DealerStandMode._16:
                    return 16;
                case DealerStandMode._17:
                    return 17;
            }

            return 0;
        }

        private bool DealerCanHit() {
            return (Config.Blackjack.DealerStandMode == DealerStandMode.None && Dealer.Blackjack.GetIntValue() < 21) || (Config.Blackjack.DealerStandMode != DealerStandMode.None && Dealer.Blackjack.GetIntValue() < GetStandValue());
        }

        private bool DealerSafe() {
            return Dealer.Blackjack.GetIntValue() <= 21 && (Config.Blackjack.DealerStandMode == DealerStandMode.None || (Config.Blackjack.DealerStandMode != DealerStandMode.None && Dealer.Blackjack.GetIntValue() >= GetStandValue()));
        }

        public void DrawSubTabs() {
            if(ImGui.BeginTabBar("BlackjackSubTabBar", ImGuiTabBarFlags.NoTooltip)) {
                if(ImGui.BeginTabItem("Game###GamblePyon_BlackjackGame_SubTab")) {
                    CurrentSubTab = SubTab.Game;
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("Config###GamblePyon_BlackjackConfig_SubTab")) {
                    CurrentSubTab = SubTab.Config;
                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("Guide###GamblePyon_BlackjackGuide_SubTab")) {
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
            //ImGui.InputTextMultiline("###input", ref Messages, 1000, new Vector2(500, 300));
            //ImGui.Separator();
            //ImGuiHelpers.ScaledDummy(5);

            DrawGameDealer();

            ImGui.Separator();
            ImGui.PopID();
            ImGui.Columns(1);
            ImGui.Separator();

            DrawGamePlayers();

            ImGui.Separator();
            ImGui.PopID();
            ImGui.Columns(1);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5);

            if(ImGui.Button("Reset")) {
                ResetRound();
            }
            ImGui.SameLine();
            if(ImGui.Button("Close")) {
                Close_Window(this, new EventArgs());
            }
        }

        private void DrawGameDealer() {
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 90 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(1, 230 + 5 * ImGuiHelpers.GlobalScale);

            ImGui.Checkbox("Enabled", ref Enabled);
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Must first enable this option for the plugin to function.\nShould disable it while not doing a blackjack round to prevent unnecessary dice roll & object monitoring."); }
            ImGui.NextColumn();
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Dealer Name:");
            ImGui.SameLine();
            ImGui.InputText($"###dealerName", ref Dealer.Name, 255);
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("This is you! ..Or at least it should be.\nI dunno what would happen if it's not."); }

            ImGui.Columns(9);
            ImGui.SetColumnWidth(0, 55 + 5 * ImGuiHelpers.GlobalScale); //Order
            ImGui.SetColumnWidth(1, 90 + 5 * ImGuiHelpers.GlobalScale); //Alias
            ImGui.SetColumnWidth(2, 80 + 5 * ImGuiHelpers.GlobalScale); //Bet
            ImGui.SetColumnWidth(3, 65 + 5 * ImGuiHelpers.GlobalScale); //Bet Actions
            ImGui.SetColumnWidth(4, 65 + 5 * ImGuiHelpers.GlobalScale); //Card Actions
            ImGui.SetColumnWidth(5, 80 + 5 * ImGuiHelpers.GlobalScale); //Cards
            ImGui.SetColumnWidth(6, 45 + 5 * ImGuiHelpers.GlobalScale); //Value
            ImGui.SetColumnWidth(7, 65 + 5 * ImGuiHelpers.GlobalScale); //Result
            ImGui.SetColumnWidth(8, 80 + 5 * ImGuiHelpers.GlobalScale); //Profit

            ImGui.Separator();

            ImGui.Text("");
            ImGui.NextColumn();
            ImGui.Text("Alias");
            ImGui.NextColumn();
            ImGui.Text("Total Bet");
            ImGui.NextColumn();
            ImGui.Text(" Bet "); //BET
            ImGui.NextColumn();
            ImGui.Text(" Card "); //CARD
            ImGui.NextColumn();
            ImGui.Text("Cards");
            ImGui.NextColumn();
            ImGui.Text("Value");
            ImGui.NextColumn();
            ImGui.Text(" Result "); //Result
            ImGui.NextColumn();
            ImGui.Text("Profit");
            ImGui.NextColumn();

            ImGui.Separator();

            ImGui.PushID($"dealer");

            //Order
            ImGui.SetNextItemWidth(-1);
            ImGui.NextColumn();

            //Alias
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText($"###dealerAlias", ref Dealer.Alias, 255);
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("A name to refer to the dealer by, does not need to be your full name.\nThis should be set automatically, but you can change it if you want a cooler name."); }
            ImGui.NextColumn();

            //Bet
            ImGui.SetNextItemWidth(-1);
            int totalBet = 0;
            foreach(Player player in Players) {
                if(string.IsNullOrWhiteSpace(player.Alias)) { continue; }
                totalBet += player.Bet;
            }
            Dealer.Bet = totalBet;
            ImGuiEx.InputText("###dealerBet", ref Dealer.Bet);
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("The total bet pool for this round.\nYou don't need to touch this.\n..you can try though if you want?"); }
            ImGui.NextColumn();

            //Bet Actions
            ImGui.SetNextItemWidth(-1);
            if(Enabled && !string.IsNullOrWhiteSpace(Dealer.Alias) && (CurrentEvent == Event.PlaceBets || (CurrentEvent == Event.BetsPlaced && Players.Find(x => x.Bet > 0) != null))) {
                string btnId = CurrentEvent != Event.BetsPlaced ? "B" : "F";
                string btnMsg = CurrentEvent != Event.BetsPlaced ? Config.Blackjack.Messages["PlaceBets"] : Config.Blackjack.Messages["BetsPlaced"];
                string hoverMsg = CurrentEvent != Event.BetsPlaced ? "Request players to place bets." : "Announce all bets are placed.";

                if(ImGui.Button(btnId)) {
                    SendMessage($"{FormatMessage(btnMsg, Dealer)}");
                    if(CurrentEvent == Event.PlaceBets) {
                        ResetRound();
                        CurrentEvent = Event.BetsPlaced;
                    } else if(CurrentEvent == Event.BetsPlaced) {
                        CurrentEvent = Event.CardActions;
                    }
                }
                if(ImGui.IsItemHovered()) {
                    ImGui.SetTooltip(hoverMsg);
                }
            }
            ImGui.NextColumn();

            //Card Actions
            ImGui.SetNextItemWidth(-1);
            if(Enabled && !string.IsNullOrWhiteSpace(Dealer.Alias) && CurrentEvent == Event.CardActions && DealerCanHit()) {
                if(Dealer.Blackjack.Cards.Count == 0 && Players.Find(x => x.Alias != "" && x.Bet != 0 && x.Blackjack.Cards.Count != 2) == null) {
                    if(ImGui.Button("1")) {
                        CurrentAction = Action.DealerDraw1;
                        SendRoll();
                    }
                    if(ImGui.IsItemHovered()) { ImGui.SetTooltip("After player initial 2 cards, draw dealer 1st card."); }
                } else if(Dealer.Blackjack.Cards.Count > 0) {
                    string btnId = Dealer.Blackjack.Cards.Count == 1 ? "2" : "H";
                    string hoverMsg = Dealer.Blackjack.Cards.Count == 1 ? "After player card actions, draw dealer 2nd card." : Config.Blackjack.DealerStandMode == DealerStandMode.None ? "After 2nd card, hit/stand as desired." : $"After 2nd card, hit until {GetStandValue()} or over.";

                    if(ImGui.Button(btnId)) {
                        CurrentAction = Dealer.Blackjack.Cards.Count == 1 ? Action.DealerDraw2 : Action.DealerHit;
                        SendRoll();
                    }
                    if(ImGui.IsItemHovered()) { ImGui.SetTooltip(hoverMsg); }
                }
            }
            ImGui.NextColumn();

            //Cards
            ImGui.SetNextItemWidth(-1);
            string cards = Dealer.Blackjack.GetCards();
            ImGui.InputText($"###dealerCards", ref cards, 255);
            ImGui.NextColumn();

            //Value
            ImGui.SetNextItemWidth(-1);
            string value = Dealer.Blackjack.GetStrValue();
            ImGui.InputText($"###dealerValue", ref value, 255);
            ImGui.NextColumn();

            //Result Actions
            ImGui.SetNextItemWidth(-1);
            int dealerValue = Dealer.Blackjack.GetIntValue();
            Player? playerValue = Players.Find(x => !string.IsNullOrWhiteSpace(x.Alias) && x.Blackjack.Cards.Count > 0 && x.Blackjack.GetIntValue() > dealerValue && x.Blackjack.GetIntValue() <= 21);
            if(Enabled && !string.IsNullOrWhiteSpace(Dealer.Alias) && playerValue == null && DealerSafe()) {
                if(ImGui.Button("W")) {
                    SendMessage($"{FormatMessage(Config.Blackjack.Messages["NoWinners"], Dealer)}");
                    EndRound();
                }
                if(ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("Announce dealer wins.");
                }
            }
            ImGui.NextColumn();

            //Profit
            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputText("###dealerProfit", ref Dealer.TotalWinnings);
            string areYouOk = Dealer.TotalWinnings < 0 ? "\nAhh.. not looking good is it? ;w;" : Dealer.TotalWinnings > 0 ? "\nEmpty their pockets!! :3" : "";
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip($"The total profit/loss for the dealer.\n{areYouOk}"); }
            ImGui.NextColumn();
        }

        private void DrawGamePlayers() {
            //ImGuiComponents.IconButton(FontAwesomeIcon.AngleDown))
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Players");

            ImGui.Columns(9);
            ImGui.SetColumnWidth(0, 55 + 5 * ImGuiHelpers.GlobalScale); //Order
            ImGui.SetColumnWidth(1, 90 + 5 * ImGuiHelpers.GlobalScale); //Alias
            ImGui.SetColumnWidth(2, 80 + 5 * ImGuiHelpers.GlobalScale); //Bet
            ImGui.SetColumnWidth(3, 65 + 5 * ImGuiHelpers.GlobalScale); //Bet Actions
            ImGui.SetColumnWidth(4, 65 + 5 * ImGuiHelpers.GlobalScale); //Card Actions
            ImGui.SetColumnWidth(5, 80 + 5 * ImGuiHelpers.GlobalScale); //Cards
            ImGui.SetColumnWidth(6, 45 + 5 * ImGuiHelpers.GlobalScale); //Value
            ImGui.SetColumnWidth(7, 65 + 5 * ImGuiHelpers.GlobalScale); //Result
            ImGui.SetColumnWidth(8, 80 + 5 * ImGuiHelpers.GlobalScale); //Profit

            ImGui.Separator();

            ImGui.Text("Order");
            ImGui.NextColumn();
            ImGui.Text("Alias");
            ImGui.NextColumn();
            ImGui.Text("Bet Amount");
            ImGui.NextColumn();
            ImGui.Text(" Bet "); //BET
            ImGui.NextColumn();
            ImGui.Text(" Card "); //CARD
            ImGui.NextColumn();
            ImGui.Text("Cards");
            ImGui.NextColumn();
            ImGui.Text("Value");
            ImGui.NextColumn();
            ImGui.Text(" Result "); //Result
            ImGui.NextColumn();
            ImGui.Text("Profit");
            ImGui.NextColumn();

            foreach(Player player in Players) {
                ImGui.Separator();
                ImGui.PushID($"player_{player.ID}");

                //Order
                ImGui.SetNextItemWidth(-1);
                if(Players.Count > 1) {
                    int index = Players.IndexOf(player);

                    if(index != 0) {
                        if(ImGui.Button("↑###orderUp")) {
                            Player p = Players[index - 1];
                            Players[index - 1] = player;
                            Players[index] = p;
                        }
                        if(ImGui.IsItemHovered()) {
                            ImGui.SetTooltip("Move this player up the list.");
                        }
                    } else {
                        ImGui.Dummy(new System.Numerics.Vector2(20, 10));
                    }
                    ImGui.SameLine();
                    if(index != Players.Count - 1) {
                        if(ImGui.Button("↓###orderDown")) {
                            Player p = Players[index + 1];
                            Players[index + 1] = player;
                            Players[index] = p;
                        }
                        if(ImGui.IsItemHovered()) {
                            ImGui.SetTooltip("Move this player down the list.");
                        }
                    }
                }
                ImGui.NextColumn();

                //Name
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText($"###playerAlias", ref player.Alias, 255);
                if(ImGui.IsItemHovered()) { ImGui.SetTooltip("A name to refer to this player by, does not need to be their full name.\nLeave blank if the seat is free.\nYou do not need to touch this if 'Auto Party' is enabled.. you can if you REALLY want to though."); }
                ImGui.NextColumn();

                //Bet Amount
                ImGui.SetNextItemWidth(-1);
                ImGuiEx.InputText("###playerBet", ref player.Bet);
                if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Input the bet amount that the player traded.\nLeave at 0 if player is sitting out the round or the seat is free."); }
                ImGui.NextColumn();

                //Bet Actions
                ImGui.SetNextItemWidth(-1);
                if(Enabled && !string.IsNullOrWhiteSpace(player.Alias) && player.Bet > 0 && player.Blackjack.Cards.Count == 0 && CurrentEvent == Event.BetsPlaced) {
                    string btnId = player.Blackjack.IsPush ? "P" : "B";
                    string btnMsg = player.Blackjack.IsPush ? Config.Blackjack.Messages["PlayerBetPushed"] : Config.Blackjack.Messages["PlayerBet"];
                    string hoverMsg = player.Blackjack.IsPush && Config.Blackjack.PushAllowBet ? "After trading, announce player's bet as a pushed bet." : player.Blackjack.IsPush ? "No trading, announce player's bet as a pushed bet from previous round." : "After trading, announce player's bet.";
                    if(ImGui.Button(btnId)) {
                        SendMessage($"{FormatMessage(btnMsg, player)}");
                    }
                    if(ImGui.IsItemHovered()) {
                        ImGui.SetTooltip(hoverMsg);
                    }
                }
                
                if(Enabled && !string.IsNullOrWhiteSpace(player.Alias) && !player.Blackjack.Doubled && Dealer.Blackjack.Cards.Count == 1 && player.Blackjack.Cards.Count == 2 && player.Blackjack.GetIntValue() < 21 && (!player.Blackjack.IsPush || (player.Blackjack.IsPush && Config.Blackjack.PushAllowDouble))) {
                    ImGui.SameLine();
                    if(ImGui.Button("D###doubleBet")) {
                        if(Config.Blackjack.AutoDouble) {
                            player.Bet = player.Bet * 2;
                        }
                        player.Blackjack.Doubled = true;
                        SendMessage($"{FormatMessage(Config.Blackjack.DoubleMustHit ? Config.Blackjack.Messages["PlayerBetDoubleMustHit"] : Config.Blackjack.Messages["PlayerBetDouble"], player)}");
                    }
                    if(ImGui.IsItemHovered()) {
                        ImGui.SetTooltip($"After trading, announce player's bet as a doubled bet.{(Config.Blackjack.DoubleMustHit ? "\nRule in play: Player must hit on double down." : "")}");
                    }
                }
                ImGui.NextColumn();

                //Card Actions
                ImGui.SetNextItemWidth(-1);
                if(Enabled && !string.IsNullOrWhiteSpace(player.Alias) && player.Bet > 0 && CurrentEvent == Event.CardActions) {
                    string btnId = player.Blackjack.Cards.Count == 0 ? "1" : player.Blackjack.Cards.Count == 1 ? "2" : "";
                    string hoverMsg = player.Blackjack.Cards.Count < 2 ? "After bets, draw initial 2 cards." : "";

                    if(btnId != "") {
                        if(ImGui.Button(btnId)) {
                            CurrentAction = Action.PlayerDraw2;
                            CurrentPlayer = player;
                            SendRoll();
                        }
                        if(ImGui.IsItemHovered()) {
                            ImGui.SetTooltip(hoverMsg);
                        }
                    } else if(player.Blackjack.GetIntValue() < 21 && Dealer.Blackjack.Cards.Count > 0 && (!player.Blackjack.Doubled || !player.Blackjack.DoubleHit)) {
                        btnId = "?";
                        hoverMsg = "After dealer's 1st card, request player to " + (!player.Blackjack.IsPush || Config.Blackjack.PushAllowDouble ? "Stand/Hit/Double" : "Stand/Hit");

                        if(ImGui.Button(btnId)) {
                            SendMessage($"{FormatMessage((!player.Blackjack.IsPush || Config.Blackjack.PushAllowDouble ? Config.Blackjack.Messages["PlayerStandHitDouble"] : Config.Blackjack.Messages["PlayerStandHit"]), player)}");
                        }
                        if(ImGui.IsItemHovered()) {
                            ImGui.SetTooltip(hoverMsg);
                        }

                        ImGui.SameLine();
                        if(ImGui.Button("H")) {
                            if(player.Blackjack.Doubled) {
                                player.Blackjack.DoubleHit = true;
                            }
                            CurrentAction = Action.PlayerHit;
                            CurrentPlayer = player;
                            SendRoll();
                        }
                        if(ImGui.IsItemHovered()) {
                            ImGui.SetTooltip("Respond to player hit request, draw additional card.");
                        }

                        ImGui.SameLine();
                        if(ImGui.Button("S")) {
                            SendMessage($"{FormatMessage(Config.Blackjack.Messages["PlayerStand"], player)}");
                        }
                        if(ImGui.IsItemHovered()) {
                            ImGui.SetTooltip("Respond to player stand request, ending player's turn.");
                        }
                    }
                }
                ImGui.NextColumn();

                //Cards
                ImGui.SetNextItemWidth(-1);
                string cards = player.Blackjack.GetCards();
                ImGui.InputText($"###playerCards", ref cards, 255);
                ImGui.NextColumn();

                //Value
                ImGui.SetNextItemWidth(-1);
                string value = player.Blackjack.GetStrValue();
                ImGui.InputText($"###playerValue", ref value, 255);
                ImGui.NextColumn();

                //Result Actions
                ImGui.SetNextItemWidth(-1);
                if(Enabled && !string.IsNullOrWhiteSpace(player.Alias) && player.Bet > 0 && player.Blackjack.Cards.Count >= 2 && Dealer.Blackjack.Cards.Count >= 2) {
                    int dealerValue = Dealer.Blackjack.GetIntValue();
                    if(Config.Blackjack.DealerStandMode == DealerStandMode.None || dealerValue >= GetStandValue()) {
                        int playerValue = player.Blackjack.GetIntValue();

                        if((playerValue > dealerValue || dealerValue > 21) && playerValue <= 21) {
                            if(ImGui.Button("W")) {
                                double playerWinnings = playerValue == 21 && (!Config.Blackjack.NaturalBlackjack || player.Blackjack.Cards.Count == 2) ? (player.Bet * Config.Blackjack.BlackjackWinMultiplier) : player.Bet * Config.Blackjack.NormalWinMultiplier;
                                player.Winnings = (int)playerWinnings;
                                player.TotalWinnings += player.Winnings;
                                Dealer.TotalWinnings -= player.Winnings;
                                player.Bet = 0;
                                SendMessage($"{FormatMessage(Config.Blackjack.Messages["Win"], player)}");
                                EndRound();
                            }
                            if(ImGui.IsItemHovered()) {
                                ImGui.SetTooltip("Calculate & announce player's winnings.");
                            }
                        } else if(playerValue < dealerValue || playerValue > 21) {
                            if(ImGui.Button("L")) {
                                player.TotalWinnings -= player.Bet;
                                Dealer.TotalWinnings += player.Bet;
                                player.Bet = 0;
                                SendMessage($"{FormatMessage(Config.Blackjack.Messages["Loss"], player)}");
                                EndRound();
                            }
                            if(ImGui.IsItemHovered()) {
                                ImGui.SetTooltip("Calculate & optionally announce player's loss.");
                            }
                        } else if(playerValue <= 21 && playerValue == dealerValue) {
                            if(ImGui.Button("D###draw")) {
                                SendMessage($"{FormatMessage(Config.Blackjack.Messages["Draw"], player)}");
                                EndRound();
                            }
                            if(ImGui.IsItemHovered()) {
                                ImGui.SetTooltip("Announce player draw, offer push/refund.");
                            }
                            ImGui.SameLine();
                            ImGui.Checkbox("###playerPush", ref player.Blackjack.Pushed);
                            if(ImGui.IsItemHovered()) {
                                ImGui.SetTooltip("Push player's bet to next round.");
                            }
                        }
                    }
                }
                ImGui.NextColumn();

                //Profit
                ImGui.SetNextItemWidth(-1);
                ImGuiEx.InputText("###playerProfit", ref player.TotalWinnings);
                string areTheyOk = player.TotalWinnings < 0 ? "\nThis person is unlucky! :3" : player.TotalWinnings > 0 ? "\nThis person is too lucky! ;w;" : "";
                if(ImGui.IsItemHovered()) { ImGui.SetTooltip($"The total profit/loss for this player.\nEmpty this field when the player stops playing.{areTheyOk}"); }
                ImGui.NextColumn();
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
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Game Rules");

            ImGui.Columns(3);
            ImGui.SetColumnWidth(0, 200 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(1, 200 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(2, 200 + 5 * ImGuiHelpers.GlobalScale);

            ImGui.Separator();

            ImGui.Text("Min Bet");
            ImGui.NextColumn();
            ImGui.Text("Max Bet");
            ImGui.NextColumn();
            ImGui.Text("Dealer Stands On");
            ImGui.NextColumn();

            ImGui.Separator();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputInt("###minBet", Config.Blackjack, nameof(Config.Blackjack.MinBet));
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputInt("###maxBet", Config.Blackjack, nameof(Config.Blackjack.MaxBet));
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            if(ImGui.RadioButton("None", Config.Blackjack.DealerStandMode == DealerStandMode.None)) {
                Config.Blackjack.DealerStandMode = DealerStandMode.None;
            }
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Dealer plays without stand limitation."); }
            ImGui.SameLine();
            if(ImGui.RadioButton("16", Config.Blackjack.DealerStandMode == DealerStandMode._16)) {
                Config.Blackjack.DealerStandMode = DealerStandMode._16;
            }
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Dealer must stand when value is 16 or over."); }
            ImGui.SameLine();
            if(ImGui.RadioButton("17", Config.Blackjack.DealerStandMode == DealerStandMode._17)) {
                Config.Blackjack.DealerStandMode = DealerStandMode._17;
            }
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Dealer must stand when value is 17 or over."); }
            ImGui.NextColumn();

            ImGui.Separator();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.Checkbox($"Auto Double", Config.Blackjack, nameof(Config.Blackjack.AutoDouble));
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Automatically double bet amount when clicking Double button.\nProbably never have to uncheck this.\n..unless you enjoy typing numbers yourself, I won't judge.");
            }
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.Checkbox($"Double Must Hit", Config.Blackjack, nameof(Config.Blackjack.DoubleMustHit));
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Whether double down will require a hit.\nDisabling this is a big advantage to a player that doubles on 17-20.");
            }
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.Checkbox($"Allow Bet on Push", Config.Blackjack, nameof(Config.Blackjack.PushAllowBet));
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Whether a player with a pushed bet can bet again next round, adding to their pushed bet.");
            }
            ImGui.NextColumn();

            ImGui.Separator();

            ImGui.SetNextItemWidth(-1);
            ImGui.Text("Normal Win Multiplier");
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.Checkbox($"Blackjack Win Multiplier", Config.Blackjack, nameof(Config.Blackjack.NaturalBlackjack));
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Enable if you want Blackjack Win Multiplier to only be triggered on a 'natural' blackjack.\n'Natural' meaning, the player's first 2 cards are valued 21.\nOtherwise this win multiplier will be offered for any hand valued 21.");
            }
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.Checkbox($"Allow Double on Push", Config.Blackjack, nameof(Config.Blackjack.PushAllowDouble));
            if(ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Whether a player with a pushed bet can double down next round, doubling their pushed bet.");
            }
            ImGui.NextColumn();

            ImGui.Separator();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputFloat($"###normalWin", Config.Blackjack, nameof(Config.Blackjack.NormalWinMultiplier));
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Win multiplier for when a player wins with a hand less than 21" + (Config.Blackjack.NaturalBlackjack ? ", or 21 with more than 2 cards." : ".")); }
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.InputFloat($"###blackjackWin", Config.Blackjack, nameof(Config.Blackjack.BlackjackWinMultiplier));
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Win multiplier for when a player wins with a " + (Config.Blackjack.NaturalBlackjack ? "natural blackjack (21 with 2 cards)." : "hand valued 21.")); }
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            ImGuiEx.Checkbox($"Show Suits###showSuit", Config.Blackjack, nameof(Config.Blackjack.ShowSuit));
            if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Some boring people don't like these symbols ;w;\nDisabling this will make the boring people happy..\n..but Pyon will be sad."); }
            ImGui.NextColumn();
        }

        private void DrawConfigMessages() {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Localization");
            ImGui.TextWrapped("Adjust text to be output for various events.\nKeywords: #player#, #firstname#, #lastname#, #dealer#, #minbet#, #maxbet#, #bet#, #cards#, #value#, #stand#, #winnings#, #profit#");

            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 180 + 5 * ImGuiHelpers.GlobalScale);
            ImGui.SetColumnWidth(1, 500 + 5 * ImGuiHelpers.GlobalScale);

            ImGui.Separator();

            ImGui.Text("Descriptor");
            ImGui.NextColumn();
            ImGui.Text("Text");
            ImGui.NextColumn();

            ImGui.Separator();

            foreach(var message in Config.Blackjack.Messages) {
                ImGui.PushID($"m_{message.Key}");
                ImGui.SetNextItemWidth(-1);
                ImGui.Text(message.Key);
                ImGui.NextColumn();
                ImGui.SetNextItemWidth(-1);
                string msg = message.Value;
                ImGui.InputText($"###m{message.Key}", ref msg, 255);
                Config.Blackjack.Messages[message.Key] = msg;
                if(message.Key == "PlayerStandHit") {
                    if(ImGui.IsItemHovered()) { ImGui.SetTooltip("This might look the same as PlayerHitUnder21\n..but it's not the same, trust me."); }
                } else if(message.Key == "Loss") {
                    if(ImGui.IsItemHovered()) { ImGui.SetTooltip("Should probably not let people know when they lose.\n..it might make them realize they're losing, you know?"); }
                }
                ImGui.NextColumn(); ImGui.Separator();
            }
        }

        private void DrawGuide() {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Initial Setup");
            ImGui.Separator();
            ImGui.TextWrapped("1. This plugin supports public play in /say chat as well as private /party play, this can be adjusted from the 'Main Config' tab. The default & recommended is /party play to avoid spamming public channels.");
            ImGui.Separator();
            ImGui.TextWrapped("2. On the Blackjack 'Config' tab, you can adjust game rules & customize what messages are output to chat for various events, you can also leave a message blank to output nothing for a specific event.");
            ImGui.Separator();
            ImGui.TextWrapped("3. On the Blackjack 'Game' tab, ensure the 'Enable' checkbox is checked, the Dealer Name is your own name (automatically set).\n'Alias' is a name you want the plugin to refer to you by, you may also have to manually set this for the participating players if you're not playing in party with 'Auto Party' enabled in 'Main Config'.\nWhen a player is finished playing, you can simply delete their name & profit values to reset their seat (this is done automatically with 'Auto Party' enabled).\nThe game logic also supports having players sit out a round, just keep their bet amount at 0 to ignore their seat.");
            ImGui.Separator();
            ImGui.TextColored(ImGuiColors.DalamudGrey, "How to be a Cute Dealer in 10 Easy Steps!");
            ImGui.Separator();
            ImGui.TextWrapped("1. When ready, you can start a round by pressing the 'B' button in Dealer  Bet , this will send a message informing players that the round is starting & they'll need to place bets.");
            ImGui.Separator();
            ImGui.TextWrapped("2. Trade each player for their bet amount & manually add it to their 'Bet Amount' field, press the 'B' button in Player  Bet  to announce their bet amount.");
            ImGui.Separator();
            ImGui.TextWrapped("3. Press the 'F' button in Dealer  Bet  to announce all bets have been placed.");
            ImGui.Separator();
            ImGui.TextWrapped("4. For each player, press the '1' & '2' button in Player  Card  to draw 1st & 2nd card in turn.");
            ImGui.Separator();
            ImGui.TextWrapped("5. Press the '1' button in Dealer  Card  to draw 1st dealer card, do not draw 2nd dealer card yet.");
            ImGui.Separator();
            ImGui.TextWrapped("6. For each player, press the '?' button in Player  Card  to request them to Stand/Hit/Double.");
            ImGui.Separator();
            ImGui.TextWrapped(" - Hit: Press the 'H' button in Player  Card  to draw another card, then request them to Hit/Stand if they don't bust.");
            ImGui.Separator();
            ImGui.TextWrapped(" - Double: Trade the player for their bet amount again, then press the 'D' button in Player  Bet  to announce it & automatically update their 'Bet Amount' field, then request them to Hit/Stand.");
            ImGui.Separator();
            ImGui.TextWrapped(" - Stand: Can optionally press the 'S' button in Player  Card  if you'd like to announce that the player stands, otherwise just move on to the next player.");
            ImGui.Separator();
            ImGui.TextWrapped("7. When all players have either stood or bust, press the '2' button in Dealer  Card  to draw 2nd dealer card.");
            ImGui.Separator();
            ImGui.TextWrapped("8. While dealer hand is under 17, press the 'H' button in Dealer  Card  to continue drawing.");
            ImGui.Separator();
            ImGui.TextWrapped("9. Win/Loss states are calculated automatically, press either of W/L/D buttons in  Result .");
            ImGui.Separator();
            ImGui.TextWrapped(" - Win: (Dealer) - Available if all players have lost/drawn and announces as such.");
            ImGui.Separator();
            ImGui.TextWrapped(" - Win: (Player) - Available if player beats dealer hand or dealer bust. Calculate & announce winnings which should be traded to them. This will also tally total wins/losses in the 'Profits' field for both player & dealer.");
            ImGui.Separator();
            ImGui.TextWrapped(" - Draw: (Player) - Available if player matches dealer hand. Request Push/Refund, if they push click the checkbox to the right of the 'D' button. When a new round is started, their bet will be automatically set, press the 'P' button in Player  Bet  to announce their bet as a pushed bet that round.");
            ImGui.Separator();
            ImGui.TextWrapped(" - Loss: (Player) - Available if player is lower than dealer hand or bust. Tally total wins/losses in the 'Profits' field for both dealer & player, by default a loss is not announced as the loss message is blank.");
            ImGui.Separator();
            ImGui.TextWrapped("10. A new round can be started by pressing the 'B' button again in Dealer  Bet , automatically resetting cards & bet amounts (other than push).\nThe 'Reset' button can also be used to do the same thing, without announcing a new round starting.");
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
