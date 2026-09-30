using Pino.SharedKernel.Clubs;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Clubs.Pages;

public sealed partial class ClubSettings : SportPageBase
{
    private ClubDetailsInput _input = new();
    protected override Task LoadAsync()
    {
        if (Membership?.Role != ClubRole.Administrator) { throw new UnauthorizedAccessException(); }
        var club = Membership.Club;
        _input = new() { Revision = club.Revision, Name = club.Name, Sport = club.Sport, City = club.City, State = club.State };
        return Task.CompletedTask;
    }

    private async Task SaveDetailsAsync()
    {
        var saved = false;
        await ExecuteAsync(async () =>
        {
            var reply = await Clubs.SaveDetailsAsync(ClubId, _input, Token);
            Message = reply.Message;
            MessageKind = reply.Succeeded ? "success" : "error";
            saved = reply.Succeeded;
        });
        if (saved) { await ReloadAsync(); }
    }
}
