namespace NavQurt.Server.Application.Dto;

public record UserDto(
    string Id,
    string UserName,
    string? FirstName,
    string? LastName,
    string? Email,
    bool EmailConfirmed,
    string? PhoneNumber,
    bool PhoneNumberConfirmed,
    bool IsActive,
    string? FullName,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles);

public record UpdateUserProfileRequest(
    string? UserId,
    string? UserName,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    bool? IsActive);

public record UpdateUserRolesRequest(string? UserId, IReadOnlyCollection<string> Roles);

public record UpdateUserStatusRequest(string? UserId, bool IsActive);
