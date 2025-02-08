using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace SRStatsTracker;

public class TimeStats
{
    public double TotalPlaytime { get; set; } = 0.0; // 1
    public double TerroristPlaytime { get; set; } = 0.0; // 2
    public double CounterTerroristPlaytime { get; set; } = 0.0; // 3
    public double SpectatorPlaytime { get; set; } = 0.0;    // 4
    public double AlivePlaytime { get; set; } = 0.0; // 5
    public double DeadPlaytime { get; set; } = 0.0; // 6
    public string LastPlayDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd"); // 7
    public double TodayPlaytime { get; set; } = 0.0; // 8
    public long LastUpdateTime; // 9

    public void UpdatePlaytime(bool isAlive, CsTeam currentTeam)
    {
        long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();

        // Calcula a duração da sessão em minutos
        double sessionDurationMinutes = Math.Round((currentTime - LastUpdateTime) / 60.0, 2);

        // Atualiza a data do último jogo
        string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        if (LastPlayDate != currentDate)
        {
            TodayPlaytime = 0.0;
            LastPlayDate = currentDate;
        }

        // Atualiza o tempo jogado hoje
        TodayPlaytime = Math.Round(TodayPlaytime + sessionDurationMinutes, 2);

        // Atualiza o tempo total jogado
        TotalPlaytime = Math.Round(TotalPlaytime + sessionDurationMinutes, 2);

        // Atualiza o tempo jogado por equipa
        switch (currentTeam)
        {
            case CsTeam.Terrorist:
                TerroristPlaytime = Math.Round(TerroristPlaytime + sessionDurationMinutes, 2);
                break;
            case CsTeam.CounterTerrorist:
                CounterTerroristPlaytime = Math.Round(CounterTerroristPlaytime + sessionDurationMinutes, 2);
                break;
            default:
                SpectatorPlaytime = Math.Round(SpectatorPlaytime + sessionDurationMinutes, 2);
                break;
        }

        // Atualiza o tempo jogado vivo ou morto
        if (isAlive)
        {
            AlivePlaytime = Math.Round(AlivePlaytime + sessionDurationMinutes, 2);
        }
        else
        {
            DeadPlaytime = Math.Round(DeadPlaytime + sessionDurationMinutes, 2);
        }
        // Atualiza o último tempo registrado
        LastUpdateTime = currentTime;
    }
}

