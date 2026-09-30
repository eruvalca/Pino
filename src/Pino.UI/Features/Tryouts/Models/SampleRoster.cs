namespace Pino.UI.Features.Tryouts.Models;

internal static class SampleRoster
{
    internal static List<SamplePlayer> Create(string size, IReadOnlyList<Team> teams)
    {
        if (string.Equals(size, "empty", StringComparison.Ordinal))
        {
            return [];
        }

        List<SamplePlayer> players =
        [
            new(1, "Avery Morgan", 2031, "Midfielder", 14),
            new(2, "Jordan Ellis", 2030, "Defender", 7),
            new(3, "Riley Chen", 2032, "Forward", 22),
            new(4, "Cameron Brooks", 2031, "Goalkeeper", 1),
            new(5, "Sofia Martinez", 2030, "Midfielder", 18),
            new(6, "Noah Williams", 2031, "Forward", 9),
            new(7, "Isabella Fernández-Rodriguez", 2032, "Defender", 31),
            new(8, "Elliot Park", 2029, "Midfielder", 12),
            new(9, "Quinn Davis", 2030, "Defender", 4),
            new(10, "Harper Wilson", 2031, "Forward", 11),
            new(11, "Luca Bennett", 2032, "Midfielder", 16),
            new(12, "Maya Patel", 2030, "Goalkeeper", 25),
            new(13, "Rowan Clarke", 2031, "Defender", 6),
            new(14, "Amara Okafor", 2032, "Forward", 20),
            new(15, "Theo Robinson", 2030, "Midfielder", 8),
            new(16, "Finley James", 2031, "Defender", 3),
        ];
        var morning = new DateTimeOffset(2027, 4, 17, 10, 42, 0, TimeSpan.FromHours(-5));
        players[0].Notes.Add(new("Coach Sam", morning, "Checks shoulder before receiving; confident turning into space."));
        players[0].Notes.Add(new("Coach Lee", morning.AddMinutes(-6), "Looked comfortable with either foot in the small-sided game."));
        players[0].History.Add(new("Spring 2026", "Spring 2026 tryout", "Placed · Silver 2031", "Coach Lee", morning.AddYears(-1)));
        players[1].Notes.Add(new("Coach Lee", morning.AddMinutes(-12), "Communicates early with the back line. Watch recovery runs in the next game."));
        players[6].Notes.Add(new("Coach Sam", morning.AddMinutes(-18), "Stayed composed under pressure on the left side. In the final game, found a teammate between the lines three times after drawing the forward in. Would like to see the same willingness to carry the ball when the space opens on the right. Follow up in the next small-sided session."));
        for (var index = 8; index < 14; index++)
        {
            var team = teams.First(team => players[index].GraduationYear >= team.GraduationYear);
            players[index].Decision = new Decision.Placement(team);
            players[index].History.Add(new("Spring 2027", "Spring 2027 tryout", $"Placed · {team.Name}", "Coach Lee", morning.AddMinutes(-30)));
        }

        players[14].Decision = new Decision.Withdrawal();
        players[15].Decision = new Decision.NonSelection();
        if (string.Equals(size, "single", StringComparison.Ordinal))
        {
            return [players[0]];
        }

        if (string.Equals(size, "large", StringComparison.Ordinal))
        {
            for (var index = 17; index <= 96; index++)
            {
                players.Add(new(index, string.Create(System.Globalization.CultureInfo.CurrentCulture, $"Sample player {index}"), 2030 + (index % 3), "Position not recorded", index + 30));
            }
        }

        return players;
    }
}
