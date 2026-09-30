using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Pino.UI.Components;

public sealed partial class StaffAvatar : ComponentBase
{
    [Parameter, EditorRequired] public string Name { get; set; } = "";
    [Parameter] public Uri? PhotoUrl { get; set; }
    [Parameter] public bool Compact { get; set; }
    private Uri? _lastPhoto;
    private bool _failed;
    private string Initials
    {
        get
        {
            var words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return words.Length switch
            {
                0 => "?",
                1 => StringInfo.GetNextTextElement(words[0]).ToUpperInvariant(),
                _ => (StringInfo.GetNextTextElement(words[0]) + StringInfo.GetNextTextElement(words[^1])).ToUpperInvariant(),
            };
        }
    }

    protected override void OnParametersSet()
    {
        if (Equals(PhotoUrl, _lastPhoto)) { return; }
        _lastPhoto = PhotoUrl;
        _failed = false;
    }
}
