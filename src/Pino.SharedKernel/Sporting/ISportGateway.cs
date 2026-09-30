using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public interface ISportGateway
{
    Task<SportReply> SaveTeamTargetsAsync(Guid clubId, Guid teamId, TeamTargetsInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentCandidate>> GetEnrollmentCandidatesAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default);
    Task<SportingBatchReport> EnrollBulkAsync(Guid clubId, Guid tryoutId, BulkEnrollmentInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamAvailabilitySummary>> GetTeamAvailabilityAsync(Guid clubId, Guid seasonId, Guid? tryoutId, CancellationToken cancellationToken = default);
    Task<SportReply> SaveTeamAvailabilityAsync(Guid clubId, Guid seasonId, Guid? tryoutId, TeamAvailabilityInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturningPlayerReview>> GetReturningPlayersAsync(Guid clubId, Guid tryoutId, Guid sourceSeasonId, CancellationToken cancellationToken = default);
    Task<SportingBatchReport> PlaceReturningPlayersAsync(Guid clubId, Guid tryoutId, ReturningPlacementInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentDetail>> GetEnrollmentsAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default);
    Task<SportReply> ChangeEnrollmentAsync(Guid clubId, Guid tryoutId, EnrollmentChangeInput input, CancellationToken cancellationToken = default);
    Task<SportingBatchReport> ChangeEnrollmentsAsync(Guid clubId, Guid tryoutId, BulkEnrollmentChangeInput input, CancellationToken cancellationToken = default);
    Task<ErasureReport> ErasePlayerAsync(Guid clubId, ErasePlayerInput input, CancellationToken cancellationToken = default);
    Task<ErasureReport> GetErasureAsync(Guid clubId, Guid operationId, CancellationToken cancellationToken = default);
    Task<SeasonReview> GetSeasonReviewAsync(Guid clubId, Guid seasonId, CancellationToken cancellationToken = default);
    Task<TryoutReview> GetTryoutReviewAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default);
    Task<SportReply> CloseTryoutAsync(Guid clubId, Guid tryoutId, CloseTryoutInput input, CancellationToken cancellationToken = default);
    Task<SportReply> ReopenTryoutAsync(Guid clubId, Guid tryoutId, ReopenTryoutInput input, CancellationToken cancellationToken = default);
    Task<TeamDetail> GetTeamAsync(Guid clubId, Guid teamId, Guid? seasonId, CancellationToken cancellationToken = default);
    Task<SportOverview> GetOverviewAsync(Guid clubId, CancellationToken cancellationToken = default);
    Task<PlayerPage> GetPlayersAsync(Guid clubId, string query, bool archived, int page, CancellationToken cancellationToken = default);
    Task<PlayerDetail> GetPlayerAsync(Guid clubId, Guid playerId, CancellationToken cancellationToken = default);
    Task<TryoutDetail> GetTryoutAsync(Guid clubId, Guid tryoutId, CancellationToken cancellationToken = default);
    Task<PlayerNotebook> GetNotebookAsync(Guid clubId, Guid tryoutId, Guid playerId, CancellationToken cancellationToken = default);
    Task<SportReply> SavePlayerAsync(Guid clubId, PlayerInput input, CancellationToken cancellationToken = default);
    Task<SportReply> SaveSeasonAsync(Guid clubId, SeasonInput input, CancellationToken cancellationToken = default);
    Task<SportReply> SaveTeamAsync(Guid clubId, TeamInput input, CancellationToken cancellationToken = default);
    Task<SportReply> SaveTryoutAsync(Guid clubId, TryoutInput input, CancellationToken cancellationToken = default);
    Task<SportReply> EnrollAsync(Guid clubId, Guid tryoutId, EnrollmentInput input, CancellationToken cancellationToken = default);
    Task<SportReply> SaveBibNumberAsync(Guid clubId, Guid tryoutId, BibNumberInput input, CancellationToken cancellationToken = default);
    Task<SportReply> DecideAsync(Guid clubId, Guid tryoutId, DecisionInput input, CancellationToken cancellationToken = default);
    Task<SportReply> AddNoteAsync(Guid clubId, Guid tryoutId, NoteInput input, CancellationToken cancellationToken = default);
    Task<SportReply> RedactNoteAsync(Guid clubId, Guid tryoutId, RedactNoteInput input, CancellationToken cancellationToken = default);
    Task<ImportReport> ImportAsync(Guid clubId, ImportInput input, CancellationToken cancellationToken = default);
}
