using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TryoutPrint : SportPageBase
{
    [Parameter] public Guid TryoutId { get; set; }
    private TryoutDetail? _data;
    private DateTimeOffset _loadedAt;
    private string _order = "name";
    private IEnumerable<RosterEntry> Entries => string.Equals(_order, "bib", StringComparison.Ordinal)
        ? (_data?.Roster ?? []).OrderBy(value => BibOrder(value.Bib)).ThenBy(value => value.Bib, StringComparer.OrdinalIgnoreCase).ThenBy(value => value.Player.LastName, StringComparer.OrdinalIgnoreCase).ThenBy(value => value.Player.Id)
        : _data?.Roster ?? [];

    protected override async Task LoadAsync()
    {
        _data = null;
        _data = await ReadInitialAsync("GetTryoutAsync", () => Gateway.GetTryoutAsync(ClubId, TryoutId, Token));
        _loadedAt = DateTimeOffset.UtcNow;
    }

    private static long BibOrder(string bib) => long.TryParse(bib, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : long.MaxValue;
}
