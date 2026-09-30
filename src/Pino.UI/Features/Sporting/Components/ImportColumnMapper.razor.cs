using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class ImportColumnMapper
{
    private static readonly (string Key, string Label, bool Required)[] _fields =
    [
        ("first", "First name", true), ("middle", "Middle name (optional)", false),
        ("last", "Last name", true), ("year", "Graduation year", true),
        ("position", "Primary position (optional)", false), ("secondary", "Secondary position (optional)", false),
        ("email", "Contact email (optional)", false), ("reference", "Player ID from file (optional)", false),
    ];
    [Parameter, EditorRequired] public IReadOnlyList<string> Columns { get; set; } = [];
    [Parameter, EditorRequired] public CsvColumnMapping Mapping { get; set; } = new();
    [Parameter] public EventCallback<CsvColumnMapping> MappingChanged { get; set; }
    [Parameter] public bool Disabled { get; set; }

    private int Selected(string key) => key switch
    {
        "first" => Mapping.FirstName,
        "middle" => Mapping.MiddleName,
        "last" => Mapping.LastName,
        "year" => Mapping.GraduationYear,
        "position" => Mapping.Position,
        "secondary" => Mapping.SecondaryPosition,
        "email" => Mapping.ContactEmail,
        _ => Mapping.PlayerReference,
    };

    private Task ChangeAsync(string key, ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)) { return Task.CompletedTask; }
        var mapping = key switch
        {
            "first" => Mapping with { FirstName = index },
            "middle" => Mapping with { MiddleName = index },
            "last" => Mapping with { LastName = index },
            "year" => Mapping with { GraduationYear = index },
            "position" => Mapping with { Position = index },
            "secondary" => Mapping with { SecondaryPosition = index },
            "email" => Mapping with { ContactEmail = index },
            _ => Mapping with { PlayerReference = index },
        };
        return MappingChanged.InvokeAsync(mapping);
    }
}
