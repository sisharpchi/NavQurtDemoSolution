using NavQurt.Server.Application.Dto;

namespace NavQurt.Server.Application.Interfaces;

public interface IRoleService
{
    Task<OperationResult<IReadOnlyList<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<RoleDto>> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default);

    Task<OperationResult<RoleDto>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<RoleDto>> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default);

    Task<OperationResult<RoleAssignmentResponse>> AssignRolesToUserAsync(RoleAssignmentRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<RoleAssignmentResponse>> RemoveRolesFromUserAsync(RoleAssignmentRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<RoleAssignmentResponse>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
}
