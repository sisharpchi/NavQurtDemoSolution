using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NavQurt.Server.Core.Entities;
using NavQurt.Server.Core.Persistence;
using NavQurt.Server.Sms;
using NavQurt.Server.Sms.Models;
using NavQurt.Server.Sms.Persistence;
using NavQurt.Server.Web.Shared;
using NavQurt.Web.Contracts.Dto.Auth;
using NavQurt.Web.Contracts.Services;

namespace NavQurt.Web.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly INavQurtmsSender _aliposSmsSender;
        private readonly IMainRepository _mainRepository;
        public const int Minutes = 2;
        public AuthService(
            INavQurtmsSender aliposSmsSender,
            IMainRepository mainRepository)
        {
            _aliposSmsSender = aliposSmsSender;
            _mainRepository = mainRepository;
        }

        public async Task<ResponseResult<LoginResponse>> GenerateAndSendSmsCode(GenerateCodeRequest generateCodeRequest)
        {
            var user = await _mainRepository.Query<AppUser>(x => x.PhoneNumber == generateCodeRequest.PhoneNumber).FirstOrDefaultAsync();
            if (user == null)
            {
                return ResponseResult<LoginResponse>.CreateError("Пользователь с таким номером не существует");
            }

            var result = new LoginResponse
            {
                FirstName = user.FirstName,
                Id = user.Id,
                LastName = user.LastName,
                PhoneNumber = generateCodeRequest.PhoneNumber,
            };

            var canSendNewSms = await CanSendSms(generateCodeRequest.PhoneNumber);

            string randomCodes = "";

            randomCodes = Generate4DigitCode();

            if (canSendNewSms)
            {
                user.Code = randomCodes;
                await _mainRepository.UnitOfWork.CommitAsync();

                await HandleSms(generateCodeRequest.PhoneNumber, user.Id, randomCodes);
            }

            return ResponseResult<LoginResponse>.CreateSuccess(result);
        }
        private async Task<ResponseResult> HandleSms(string phoneNumber, string userId, string code)
        {

            if (_webHostEnvironment.IsProduction())
            {
                var sms = await RegisterSms(phoneNumber, code);
                var smsMessagesDto = BuildSmsMessage(sms);
                var smsSended = await _aliposSmsSender.SendSms(smsMessagesDto);
                if (!smsSended)
                {
                    return ResponseResult.CreateError("Что то пошло не так при отправке смс кода");
                }

            }

            return ResponseResult.CreateSuccess();
        }
        private SmsMessagesDto BuildSmsMessage(SmsCodeMessage smsCodeMessage)
        {
            var message = $"{smsCodeMessage.Code} - Код авторизации в NavQurt, никому не сообщайте данные с смс";
            return Utils.Map(Guid.NewGuid().ToString(), smsCodeMessage.PhoneNumber, message, Utils.Originator);
        }

        private async Task<bool> CanSendSms(string phoneNumber)
        {
            var lastSms = await _mainRepository.Query<SmsCodeMessage>()
                .Where(x => x.PhoneNumber == phoneNumber)
                .OrderByDescending(x => x.SendedAt)
                .FirstOrDefaultAsync();

            if (lastSms == null)
            {
                return true;
            }

            return lastSms.UsedAt.HasValue || lastSms.SendedAt.AddMinutes(Minutes) < DateTime.Now;
        }
        private string Generate4DigitCode() => new string(Enumerable.Range(0, 4).Select(_ => Random.Shared.Next(0, 10).ToString()[0]).ToArray());
        private async Task<SmsCodeMessage> RegisterSms(string phoneNumber, string code)
        {
            var smsCode = new SmsCodeMessage
            {
                Code = code,
                PhoneNumber = phoneNumber,
                SendedAt = DateTime.Now,
            };
            await _mainRepository.AddAsync(smsCode);
            await _mainRepository.UnitOfWork.CommitAsync();

            return smsCode;
        }
    }
}
