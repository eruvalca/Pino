using System.Diagnostics.CodeAnalysis;
namespace Pino.SharedKernel.Clubs;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public interface IClubGateway
{
    Task<InvitationsPage> GetInvitationsAsync(Guid clubId, int page, CancellationToken cancellationToken = default);
    Task<ClubReply> InviteAsync(Guid clubId, InvitationInput input, CancellationToken cancellationToken = default);
    Task<ClubReply> ResendInvitationAsync(Guid clubId, InvitationChangeInput input, CancellationToken cancellationToken = default);
    Task<ClubReply> RevokeInvitationAsync(Guid clubId, InvitationChangeInput input, CancellationToken cancellationToken = default);
    Task<InvitationPreview> PreviewInvitationAsync(Guid invitationId, string token, CancellationToken cancellationToken = default);
    Task<ClubReply> AcceptInvitationAsync(Guid invitationId, InvitationAcceptInput input, CancellationToken cancellationToken = default);
    Task<StaffEmailsPage> GetEmailsAsync(Guid clubId, int page, CancellationToken cancellationToken = default);
    Task<ClubReply> RetryEmailAsync(Guid clubId, InvitationChangeInput input, CancellationToken cancellationToken = default);
    Task<ClubReply> SaveDetailsAsync(Guid clubId, ClubDetailsInput input, CancellationToken cancellationToken = default);
    Task<AccessSnapshot> GetAccessAsync(CancellationToken cancellationToken = default);
    Task<ClubSearchPage> SearchAsync(string query, int page, CancellationToken cancellationToken = default);
    Task<ClubReply> SaveProfileAsync(ProfileInput input, CancellationToken cancellationToken = default);
    Task<ClubReply> CreateAsync(CreateClubInput input, CancellationToken cancellationToken = default);
    Task<ClubReply> RequestAsync(Guid clubId, CancellationToken cancellationToken = default);
    Task<ClubReply> CancelAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<ClubReply> LeaveAsync(Guid clubId, CancellationToken cancellationToken = default);
    Task<PeoplePage> GetPeopleAsync(Guid clubId, bool requests, int page, CancellationToken cancellationToken = default);
    Task<ClubReply> DecideAsync(Guid clubId, RequestDecisionInput input, CancellationToken cancellationToken = default);
    Task<ClubReply> ChangeMemberAsync(Guid clubId, MemberChangeInput input, CancellationToken cancellationToken = default);
}
