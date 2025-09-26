using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NavQurt.Server.Application.Dto;
using NavQurt.Server.Application.Interfaces;

namespace NavQurt.Server.Web.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersAsync(cancellationToken);
        return MapResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        return MapResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        var payload = request with { UserId = id };
        var result = await _userService.UpdateUserProfileAsync(payload, cancellationToken);
        return MapResult(result);
    }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] UpdateUserRolesRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        var payload = request with { UserId = id };
        var result = await _userService.UpdateUserRolesAsync(payload, cancellationToken);
        return MapResult(result);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> SetUserStatus(string id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        var payload = request with { UserId = id };
        var result = await _userService.SetUserActivationAsync(payload, cancellationToken);
        return MapResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id, CancellationToken cancellationToken)
    {
        var result = await _userService.DeleteUserAsync(id, cancellationToken);
        return MapResult(result, _ => NoContent());
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
}
