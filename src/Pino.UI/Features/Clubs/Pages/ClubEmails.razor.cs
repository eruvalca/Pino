using Pino.SharedKernel.Clubs;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Clubs.Pages;

public sealed partial class ClubEmails : SportPageBase
{
    private StaffEmailsPage? _data;
    private int _page;
    protected override async Task LoadAsync()
    {
        if (Membership?.Role != ClubRole.Administrator) { throw new UnauthorizedAccessException(); }
        _data = await ReadInitialAsync("GetEmailsAsync", () => Clubs.GetEmailsAsync(ClubId, _page, Token));
    }
    private Task RetryAsync(StaffEmailSummary email) => ExecuteAsync(async () =>
    {
        var reply = await Clubs.RetryEmailAsync(ClubId, new(email.Id, email.Revision), Token);
        Message = reply.Message;
        MessageKind = reply.Succeeded ? "success" : "error";
        await LoadAsync();
    });
    private async Task PreviousAsync() { _page--; await ReloadAsync(); }
    private async Task NextAsync() { _page++; await ReloadAsync(); }
}
