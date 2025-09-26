using NavQurt.Server.Application.Dto;

namespace NavQurt.Server.Application.Interfaces;

public interface IUserService
{
    Task<OperationResult<IReadOnlyList<UserDto>>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<UserDto>> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<OperationResult<UserDto>> UpdateUserProfileAsync(UpdateUserProfileRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<UserDto>> UpdateUserRolesAsync(UpdateUserRolesRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<UserDto>> SetUserActivationAsync(UpdateUserStatusRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
}
