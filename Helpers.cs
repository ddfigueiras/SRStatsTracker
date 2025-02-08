using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
namespace SRStatsTracker
{
    public class Helpers
    {
        #region HasWeapon
        public bool HasWeapon(CCSPlayerController player, string weapon_name)
        {
            foreach (var weapon in player.PlayerPawn.Value!.WeaponServices!.MyWeapons)
            {
                if (weapon is { IsValid: true, Value.IsValid: true })
                {
                    if (weapon.Value.DesignerName.Contains($"{weapon_name}"))
                    {
                        if (SRStatsTracker.debug)
                            Server.PrintToConsole($"Requested weapon is weapon_{weapon_name}");
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion
        #region Tem permissão
        public static bool IsVip(CCSPlayerController player)
        {
            if (player == null || !IsValid(player))
            {
                return false;
            }

            return AdminManager.PlayerHasPermissions(player, new String[] { "@css/vip" });
        }
        public static bool IsStaff(CCSPlayerController player)
        {
            if (player == null || !IsValid(player))
            {
                return false;
            }

            return AdminManager.PlayerHasPermissions(player, new String[] { "@css/ban" });
        }
        #endregion
        #region IsValid
        public static bool IsValid(CCSPlayerController? player)
        {
            return player?.IsValid == true && !player.IsHLTV && !player.IsBot &&
                player.Connected == PlayerConnectedState.PlayerConnected &&
                player.SteamID.ToString().Length == 17;
        }

        public static bool IsValidAlive(CCSPlayerController? player)
        {
            return IsValid(player) &&
                player!.PlayerPawn != null &&
                player!.PlayerPawn.Value != null &&
                player!.PlayerPawn.Value.Health > 0;
        }
        public static bool IsValid(CCSPlayerPawn? player)
        {
            return player != null && player.Health > 0;
        }
        #endregion
        #region GetHealth
        public static int GetHealth(CCSPlayerController? player)
        {
            CCSPlayerPawn? pawn = pawnGet(player);

            if (pawn == null)
            {
                return 0;
            }
            return pawn.Health;
        }
        #endregion
        #region pawnGet
        public static CCSPlayerPawn? pawnGet(CCSPlayerController? player)
        {
            if (player == null || !IsValid(player))
            {
                return null;
            }

            CCSPlayerPawn? pawn = player.PlayerPawn.Value;

            return pawn;
        }
        #endregion
        #region Anunciar Chat
        public static void AnunciarChat(string frase, CCSPlayerController? player = null)
        {
            if (player == null)
            {
                Server.PrintToChatAll($" {ChatColors.DarkRed}SweetRicers • {ChatColors.Green} " + frase + ".");
            }
            else
            {
                player.PrintToChat($" {ChatColors.DarkRed}SweetRicers • {ChatColors.Green} " + frase + ".");
            }
        }
        public static void AnunciarConsola(string frase, CCSPlayerController? player = null)
        {
            if (player == null)
            {
                Server.PrintToChatAll($" SweetRicers • " + frase);
            }
            else
            {
                player.PrintToChat($" SweetRicers • " + frase);
            }
        }
        #endregion
        #region GetPlayers
        public static List<SRPlayer> GetPlayers()
        {
            return SRStatsTracker.playerList.FindAll(player => player.IsValid());
        }
        public static List<SRPlayer> GetAlivePlayers()
        {
            return SRStatsTracker.playerList.FindAll(player => player.IsValidAlive());
        }
        public static List<SRPlayer> GetAliveCT()
        {
            return GetAlivePlayers().FindAll(player => player.controller.Team == CsTeam.CounterTerrorist);
        }
        public static List<SRPlayer> GetAliveT()
        {
            return GetAlivePlayers().FindAll(player => player.controller.Team == CsTeam.Terrorist);
        }
        public static List<SRPlayer> GetCTList()
        {
            return GetPlayers().FindAll(player => player.controller.Team == CsTeam.CounterTerrorist);
        }
        public static List<SRPlayer> GetTList()
        {
            return GetPlayers().FindAll(player => player.controller.Team == CsTeam.Terrorist);
        }
        public static int CtCount()
        {
            return GetPlayers().FindAll(player => player.controller.Team == CsTeam.CounterTerrorist).Count;
        }

        public static int TCount()
        {
            return GetPlayers().FindAll(player => player.controller.Team == CsTeam.Terrorist).Count;
        }
        public static int CtCountAlive()
        {
            return GetAlivePlayers().FindAll(player => player.controller.Team == CsTeam.CounterTerrorist).Count;
        }

        public static int TCountAlive()
        {
            return GetAlivePlayers().FindAll(player => player.controller.Team == CsTeam.Terrorist).Count;
        }
        public static bool StaffInGame()
        {
            return GetPlayers().Any(player => player.IsStaff());
        }
        public static SRPlayer? GetSRPlayer(CCSPlayerController? p)
        {
            if (p == null) return null;
            return SRStatsTracker.playerList.FirstOrDefault(player => player.controller == p);
        }
        public static SRPlayer? GetSRPlayer(int p)
        {
            return SRStatsTracker.playerList.FirstOrDefault(player => player.controller.Slot == p);
        }
        public static SRPlayer? GetSRPlayer(CCSPlayerPawn? p)
        {
            if (p == null) return null;
            return SRStatsTracker.playerList.FirstOrDefault(player => player.IsValidAlive() && player.controller.PlayerPawn.Value == p);
        }
        public static SRPlayer? GetSRPlayer(ulong SteamID)
        {
            return SRStatsTracker.playerList.FirstOrDefault(player => player.controller.SteamID == SteamID);
        }
        #endregion
        #region PrintAdmin
        public static void PrintAdmin(string Message, bool console = true, bool printTag = false, string DevMessage = "")
        {
            foreach (var player in SRStatsTracker.playerList.Where(p => p.IsValid() && p.IsStaff()))
            {
                if (console)
                {
                    if (printTag)
                        AnunciarChat(Message, player.controller);
                    else
                        player.controller.PrintToConsole(Message);
                }
                else
                {
                    AnunciarChat($" {ChatColors.DarkBlue}STAFF - {ChatColors.Grey}" + Message, player.controller);
                }
                if (DevMessage != "" && AdminManager.PlayerHasPermissions(player.controller, new String[] { "@css/root" }))
                {
                    player.controller.PrintToConsole(DevMessage);
                }
            }
        }
        #endregion
        #region GetGameRules
        public static CCSGameRules GetGameRules()
        {
            var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            var gameRules = gameRulesEntities.First().GameRules;

            if (gameRules == null!)
            {
                Server.PrintToConsole("Failed to get game rules");
                return null!;
            }

            return gameRules;
        }
        #endregion
        #region GetArgsFromCommandLine
        public static List<string> GetArgsFromCommandLine(string commandLine)
        {
            List<string> args = new List<string>();
            var regex = new Regex(@"(""((\\"")|([^""]))*"")|('((\\')|([^']))*')|(\S+)");
            var matches = regex.Matches(commandLine);
            foreach (Match match in matches)
            {
                args.Add(match.Value);
            }
            return args;
        }
        #endregion GetArgsFromCommandLine
        public static void PlaySoundOnPlayer(SRPlayer? player, string sound)
        {
            if (player == null || !player.IsValid()) return;
            player.controller.ExecuteClientCommand($"play {sound}");
        }
        public static int GetPAmmo(CBasePlayerWeapon? weapon)
        {
            if (WeaponIsValid(weapon) == false)
            {
                return 0;
            }
            return weapon!.Clip1;
        }
        public static CBasePlayerWeapon? GetWeapon(CCSPlayerController player, string weaponName)
        {
            if (Helpers.IsValidAlive(player))
            {
                foreach (var weapon in player.PlayerPawn.Value!.WeaponServices!.MyWeapons)
                {
                    if (weapon.Value != null && string.IsNullOrWhiteSpace(weapon.Value.DesignerName) == false && weapon.Value.DesignerName != "[null]")
                    {
                        if (weapon.Value.DesignerName == weaponName)
                        {
                            return weapon.Value;
                        }
                    }
                }
            }
            return null;
        }
        #region  WeaponIsValid
        public static bool WeaponIsValid(CBasePlayerWeapon? weapon) => weapon != null && weapon.IsValid != false;
        #endregion
        #region  IsInt
        public static bool IsInt(string sVal)
        {
            foreach (char c in sVal)
            {
                int iN = (int)c;
                if ((iN > 57) || (iN < 48))
                    return false;
            }
            return true;
        }
        #endregion
        public static List<CCSPlayerController> GetTargetList(CCSPlayerController caller, CommandInfo info, bool countWithSpec = false)
        {
            if (string.IsNullOrEmpty(info.ArgString))
            {
                AnunciarChat("Não foi possivel encontrar esse jogador, tens de dizer um target", caller!);
                return new();
            }

            var target = info.ArgByIndex(1);
            bool idCheck = target.StartsWith("#");
            if (idCheck)
            {
                target = target.Substring(1);
            }

            var players = Utilities.GetPlayers().Where(p => p != null && IsValid(p)).ToList();

            if (target.StartsWith("@"))
            {
                return target switch
                {
                    "@all" => players,
                    "@me" => new() { caller },
                    "@t" => players.Where(p => p.Team == CsTeam.Terrorist).ToList(),
                    "@ct" => players.Where(p => p.Team == CsTeam.CounterTerrorist).ToList(),
                    "@spec" => players.Where(p => p.Team == CsTeam.Spectator).ToList(),
                    "@dead" => players.Where(p => p.Team != CsTeam.Spectator && p.Team != CsTeam.None && !IsValidAlive(p)).ToList(),
                    "@!me" => players.Where(p => p != caller).ToList(),
                    _ => new()
                };
            }

            List<CCSPlayerController> foundPlayers = players.Where(p =>
                idCheck ? p.UserId.ToString() == target : p.PlayerName.Contains(target)).ToList();

            if (foundPlayers.Count == 1)
            {
                return foundPlayers;
            }

            if (foundPlayers.Count > 1)
            {
                AnunciarChat("Mais que um jogador encontrado", caller!);
            }
            else
            {
                AnunciarChat($"Foram encontrados {foundPlayers.Count} jogadores", caller!);
            }

            return foundPlayers;
        }
        public static List<SRPlayer> GetSRTargetList(CCSPlayerController caller, CommandInfo info, bool countWithSpec = false)
        {
            if (string.IsNullOrEmpty(info.ArgString))
            {
                AnunciarChat("Não foi possivel encontrar esse jogador, tens de dizer um target", caller!);
                return new();
            }

            var target = info.ArgByIndex(1);
            bool idCheck = target.StartsWith("#");
            if (idCheck)
            {
                target = target.Substring(1);
            }

            var players = Utilities.GetPlayers().Where(p => p != null && IsValid(p)).ToList();

            if (target.StartsWith("@"))
            {
                return target switch
                {
                    "@all" => SRStatsTracker.playerList,
                    "@me" => new() { GetSRPlayer(caller)! },
                    "@t" => SRStatsTracker.playerList.Where(p => p.controller.Team == CsTeam.Terrorist).ToList(),
                    "@ct" => SRStatsTracker.playerList.Where(p => p.controller.Team == CsTeam.CounterTerrorist).ToList(),
                    "@spec" => SRStatsTracker.playerList.Where(p => p.controller.Team == CsTeam.Spectator).ToList(),
                    "@dead" => SRStatsTracker.playerList.Where(p => p.controller.Team != CsTeam.Spectator && p.controller.Team != CsTeam.None && !IsValidAlive(p.controller)).ToList(),
                    "@!me" => SRStatsTracker.playerList.Where(p => p.controller != caller).ToList(),
                    _ => new()
                };
            }

            List<SRPlayer> foundPlayers = SRStatsTracker.playerList.Where(p =>
                idCheck ? p.controller.UserId.ToString() == target : p.controller.PlayerName.Contains(target)).ToList();

            if (foundPlayers.Count == 1)
            {
                return foundPlayers;
            }

            if (foundPlayers.Count > 1)
            {
                AnunciarChat("Mais que um jogador encontrado", caller!);
            }
            else
            {
                AnunciarChat($"Foram encontrados {foundPlayers.Count} jogadores", caller!);
            }

            return foundPlayers;
        }
    }
}