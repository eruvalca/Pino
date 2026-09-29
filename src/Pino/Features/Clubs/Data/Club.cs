namespace Pino.Features.Clubs.Data;

internal sealed class Club
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Sport { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public Guid OperationId { get; set; }
}
