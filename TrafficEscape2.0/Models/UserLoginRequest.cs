using System.ComponentModel.DataAnnotations;

namespace TrafficEscape2._0.Models
{
    public class UserLoginRequest
    {
        [Required]
        public string IdToken { get; set; }

        [Required]
        public string Audience { get; set; }
    }
}
