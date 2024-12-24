using Google.Apis.Auth;
using TrafficEscape2._0.Services.Interfaces;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace TrafficEscape2._0.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        public async Task<string> ValidateAndReturnUser(string idToken)
        {
            Payload payload = await GoogleJsonWebSignature.ValidateAsync(idToken).ConfigureAwait(false);
            return payload.Subject;
        }
    }
}
