using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NavQurt.Server.Application.Dto;
using NavQurt.Server.Application.Interfaces;
using NavQurt.Server.Web.Authorization;

namespace NavQurt.Server.Web.Controllers;

[ApiController]
[Route("api/v1/roles")]
[Authorize(Policy = AuthorizationPolicies.RoleManager)]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly IAuthorizationService _authorizationService;

    public RoleController(IRoleService roleService, IAuthorizationService authorizationService)
    {
        _roleService = roleService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var result = await _roleService.GetRolesAsync(cancellationToken);
        return MapResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRole(string id, CancellationToken cancellationToken)
    {
        var result = await _roleService.GetRoleByIdAsync(id, cancellationToken);
        return MapResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        if (IsElevatedRole(request.Name) && !await IsAuthorizedForElevatedRolesAsync())
        {
            return Forbid();
        }

        var result = await _roleService.CreateRoleAsync(request, cancellationToken);
        return MapResult(result, created => CreatedAtAction(nameof(GetRole), new { id = created?.Id }, created));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        if (IsElevatedRole(request.Name) && !await IsAuthorizedForElevatedRolesAsync())
        {
            return Forbid();
        }

        var result = await _roleService.UpdateRoleAsync(id, request, cancellationToken);
        return MapResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetRoleByIdAsync(id, cancellationToken);
        if (!role.Succeeded)
        {
            return MapResult(role);
        }

        if (IsElevatedRole(role.Data?.Name) && !await IsAuthorizedForElevatedRolesAsync())
        {
            return Forbid();
        }

        var result = await _roleService.DeleteRoleAsync(id, cancellationToken);
        return MapResult(result, _ => NoContent());
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignRoles([FromBody] RoleAssignmentRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        if (request.Roles?.Any(IsElevatedRole) == true && !await IsAuthorizedForElevatedRolesAsync())
        {
            return Forbid();
        }

        var result = await _roleService.AssignRolesToUserAsync(request, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("remove")]
    public async Task<IActionResult> RemoveRoles([FromBody] RoleAssignmentRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        if (request.Roles?.Any(IsElevatedRole) == true && !await IsAuthorizedForElevatedRolesAsync())
        {
            return Forbid();
        }

        var result = await _roleService.RemoveRolesFromUserAsync(request, cancellationToken);
        return MapResult(result);
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserRoles(string userId, CancellationToken cancellationToken)
    {
        var result = await _roleService.GetUserRolesAsync(userId, cancellationToken);
        return MapResult(result);
    }

    private IActionResult MapResult<T>(OperationResult<T> result, Func<T?, IActionResult>? successFactory = null)
    {
        if (result.Succeeded)
        {
            return successFactory?.Invoke(result.Data) ?? Ok(result.Data);
        }

        return result.Status switch
        {
            OperationStatus.NotFound => NotFound(new { errors = result.Errors }),
            OperationStatus.Conflict => Conflict(new { errors = result.Errors }),
            OperationStatus.Forbidden => Forbid(),
            OperationStatus.Error => StatusCode(StatusCodes.Status500InternalServerError, new { errors = result.Errors }),
            _ => BadRequest(new { errors = result.Errors })
        };
    }

    private bool IsElevatedRole(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return false;
        }

        return string.Equals(roleName, Core.Constants.RoleConstants.SuperAdmin, StringComparison.OrdinalIgnoreCase)
               || string.Equals(roleName, Core.Constants.RoleConstants.Admin, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> IsAuthorizedForElevatedRolesAsync()
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, null, AuthorizationPolicies.ManageElevatedRoles);
        return authorizationResult.Succeeded;
    }
}
