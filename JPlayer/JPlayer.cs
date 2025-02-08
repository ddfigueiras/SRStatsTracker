using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace SRStatsTracker;

public class SRPlayer
{
    public CCSPlayerController controller;
    public bool DatabaseSaved = false;
    public TimeStats timeStats = new();
    public SRPlayer(CCSPlayerController player)
    {
        this.controller = player;
    }
    public CsTeam GetTeam()
    {
        if (!IsValid()) return CsTeam.None;
        return this.controller?.Team ?? CsTeam.None;
    }
    public bool IsValid() =>
        this.controller != null &&
        this.controller.IsValid &&
        !this.controller.IsHLTV &&
        !this.controller.IsBot &&
        this.controller.Connected == PlayerConnectedState.PlayerConnected &&
        this.controller.SteamID.ToString().Length == 17;
    public bool IsValidAlive() =>
        IsValid() &&
        this.controller.PawnIsAlive &&
        this.controller.PlayerPawn?.Value?.Health > 0;
    public bool IsVip()
    {
        if (this.controller == null || !IsValid())
        {
            return false;
        }
        return AdminManager.PlayerHasPermissions(this.controller, new String[] { "@css/vip" });
    }
    public bool IsStaff()
    {
        if (this.controller == null || !IsValid())
        {
            return false;
        }
        return AdminManager.PlayerHasPermissions(this.controller, new String[] { "@css/ban" });
    }
    public void ResetPlayer()
    {
    }
    public CBasePlayerWeapon? GetWeapon(string weaponName)
    {
        if (IsValidAlive())
        {
            foreach (var weapon in this.controller.PlayerPawn.Value!.WeaponServices!.MyWeapons)
            {
                if (weapon.Value != null && string.IsNullOrWhiteSpace(weapon.Value.DesignerName) == false && weapon.Value.DesignerName != "[null]")
                {
                    if (weapon.Value.DesignerName.Contains(weaponName))
                    {
                        return weapon.Value;
                    }
                }
            }
        }
        return null;
    }
    private void PlaySoundOnPlayer(String sound)
    {
        if (!IsValid()) return;
        controller.ExecuteClientCommand($"play {sound}");
    }
    public void OnPlayerDeath()
    {
        timeStats.isAlive = false;
        timeStats.UpdatePlaytime(GetTeam());
    }
    public void OnPlayerSpawn()
    {
        timeStats.isAlive = true;
        timeStats.UpdatePlaytime(GetTeam());
    }
}
