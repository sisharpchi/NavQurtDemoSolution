using NavQurt.Server.Web.Shared;
using NavQurt.Web.Contracts.Dto.Auth;

namespace NavQurt.Web.Contracts.Services
{
    public interface IAuthService
    {
        Task<ResponseResult<LoginResponse>> GenerateAndSendSmsCode(GenerateCodeRequest generateCodeRequest);
    }
}
