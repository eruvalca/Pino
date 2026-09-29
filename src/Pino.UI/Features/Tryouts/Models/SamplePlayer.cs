namespace Pino.UI.Features.Tryouts.Models;

internal sealed class SamplePlayer(int id, string name, int graduationYear, string position, int bib)
{
    internal int Id { get; } = id;
    internal string Name { get; } = name;
    internal int GraduationYear { get; } = graduationYear;
    internal string Position { get; } = position;
    internal int Bib { get; } = bib;
    internal List<Observation> Notes { get; } = [];
    internal List<DecisionRecord> History { get; } = [];
    internal Decision Decision { get; set; } = new Decision.Awaiting();
    internal int Version { get; set; }
    internal string Draft { get; set; } = "";
}
