using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace NavQurt.Web.Contracts
{
    public class LoginRequest
    {
        [Required]
        [NotNull]
        public string PhoneNumber { get; set; } = default!;
        [Required]
        [NotNull]
        public string Code { get; set; } = default!;
    }

}
