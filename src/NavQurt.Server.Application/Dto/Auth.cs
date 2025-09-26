namespace NavQurt.Server.Application.Dto
{
    public record SignUpRequest(string UserName, string Password, string? Firstname, string Lastname, string? Email, string? Phone);
    public record SignUpResponse(Guid UserId, string UserName, string? Email, string? Phone, string? FullName);
    public record AssignRoleRequest(Guid UserId, string RoleName);
}
