using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace NavQurt.Web.Contracts.Dto.Auth
{
    public class GenerateCodeRequest
    {
        [NotNull]
        [Required]
        public string PhoneNumber { get; set; } = default!;
    }
}
