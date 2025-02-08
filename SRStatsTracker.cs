using System;
using System.Runtime.CompilerServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Memory;
using System.Drawing;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Entities;

using CounterStrikeSharp.API.Modules.Entities.Constants;
namespace SRStatsTracker;
public class SRStatsTracker : BasePlugin, IPluginConfig<DbConfig>
{
    public override string ModuleAuthor => "ddfigueiras";
    public override string ModuleName => "SRStatsTracker";
    public override string ModuleVersion => "1.0";

    #region Variaveis Globais
    public static bool debug = false;
    public static List<SRPlayer> playerList = new();
    public static Database? _dataBaseService;
    public DbConfig Config { get; set; } = new();

    public static SRStatsTracker? basePlugin { get; set; }
    #endregion

    #region Load
    public override void Load(bool hotReload)
    {
        basePlugin = this;
        // Registering events
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);

        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn, HookMode.Post);
        //AddTimer(10.0f, OnRewardTimer, TimerFlags.REPEAT);

        if (hotReload)
        {
            playerList = new();
            basePlugin = basePlugin;
        }
    }
    public override void Unload(bool hotReload)
    {
        foreach (var player in playerList)
        {
            UnLoadJogadorAsync(player);
        }
        playerList = new();
    }
    public void OnConfigParsed(DbConfig config)
    {
        _dataBaseService = new Database(config);
        _dataBaseService.TestAndCheckDataBaseTableAsync().GetAwaiter().GetResult();
        Config = config;
    }
    #endregion
    #region HookResults
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (@event.Userid == null) return HookResult.Continue;

        if (!Helpers.IsValid(@event.Userid!)) return HookResult.Continue;
        SRPlayer p = new SRPlayer(@event.Userid!);
        if (p != null && p.IsValid())
        {
            p.OnPlayerSpawn();
        }
        return HookResult.Continue;
    }
    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (@event.Userid == null) return HookResult.Continue;

        if (!Helpers.IsValid(@event.Userid!)) return HookResult.Continue;
        SRPlayer p = new SRPlayer(@event.Userid!);
        if (p != null && p.IsValid())
        {
            p.OnPlayerDeath();
        }
        return HookResult.Continue;
    }
    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        if (@event.Userid == null) return HookResult.Continue;

        if (!Helpers.IsValid(@event.Userid!)) return HookResult.Continue;
        SRPlayer p = new SRPlayer(@event.Userid!);
        if (p != null && p.IsValid())
        {
            CsTeam team = (CsTeam)@event.Team;
            p.timeStats.UpdatePlaytime(team);
        }
        return HookResult.Continue;
    }
    public HookResult EventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (!Helpers.IsValid(@event.Userid!)) return HookResult.Continue;
        SRPlayer p = new SRPlayer(@event.Userid!);
        if (p != null && p.IsValid())
        {
            LoadJogador(p);
        }
        else
        {
            if (debug)
            {
                Helpers.AnunciarChat("Erro ao criar o SRPlayer");
            }
        }
        return HookResult.Continue;
    }
    public HookResult EventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        try
        {
            if (!Helpers.IsValid(@event.Userid!)) return HookResult.Continue;
            RemovePlayerFromListAsync(@event.Userid!);
            return HookResult.Continue;
        }
        catch (Exception ex)
        {
            Server.PrintToConsole(ex.Message);
            if (debug == true) Server.PrintToConsole(ex.Message);
        }

        return HookResult.Continue;
    }
    #endregion
    #region Timer
    private void OnRewardTimer()
    {
        int interval = 60; // 10 depois vemos se alteramos
        if (interval <= 0)
            return;
        foreach (var player in playerList)
        {
            player.timeStats.UpdatePlaytime(player.IsValidAlive(), player.GetTeam());

            bool IsActive = player.timeStats.TotalPlaytime > 1 ||
                                player.timeStats.TerroristPlaytime > 1 ||
                                player.timeStats.CounterTerroristPlaytime > 1 ||
                                player.timeStats.SpectatorPlaytime > 1 ||
                                player.timeStats.AlivePlaytime > 1 ||
                                player.timeStats.DeadPlaytime > 1;
            //if (IsActive)
            //SendNotification(player, interval);
        }
    }
    #endregion
    #region DB Operation
    private void OnClientDisconnect(int playerSlot)
    {
        var player = Utilities.GetPlayerFromSlot(playerSlot);
        if (player != null && !player.IsBot && !player.IsValid)
        {
            SRPlayer? j = Helpers.GetSRPlayer(player);
            if (j == null || !j.IsValid())
            {
                return;
            }
            RemovePlayerFromListAsync(player);
        }
    }
    public void RemovePlayerFromListAsync(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV) return;

        SRPlayer? jogador = Helpers.GetSRPlayer(player);
        if (jogador != null && _dataBaseService != null && jogador.DatabaseSaved == false)
        {
            jogador.DatabaseSaved = true;
            UnLoadJogadorAsync(jogador);
        }
    }

    public void UnLoadJogadorAsync(SRPlayer jogador)
    {
        if (_dataBaseService == null) return;
        try
        {
            _dataBaseService.SaveJogador(jogador);
        }
        catch (Exception ex)
        {
            Server.PrintToConsole(ex.Message);
        }
    }
    public void LoadJogador(SRPlayer jogador)
    {
        if (_dataBaseService == null) return;
        try
        {
            var result = _dataBaseService!.LoadJogadorAsync(jogador.controller!).GetAwaiter().GetResult();
            if (result != null)
                jogador = result;
            playerList.Add(jogador!);
        }
        catch (Exception ex)
        {
            Server.PrintToConsole(ex.Message);
        }
    }
    #endregion
}