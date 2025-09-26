namespace NavQurt.Server.Application.Dto
{
    public record SignUpRequest(string UserName, string Password, string? FirstName, string? LastName, string? Email, string? Phone);
    public record SignUpResponse(string UserId, string UserName, string? Email, string? Phone, string? FullName);
    public record AssignRoleRequest(string UserId, string RoleName);
}
