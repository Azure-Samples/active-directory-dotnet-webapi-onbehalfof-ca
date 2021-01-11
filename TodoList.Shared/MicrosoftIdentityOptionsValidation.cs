using Microsoft.Identity.Client;

namespace TodoList.Shared
{
    public class MicrosoftIdentityOptionsValidation 
    {

        public static void ValidateEitherClientCertificateOrClientSecret(
            string clientSecret)
        {
            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new MsalClientException(
                    ErrorCodes.MissingClientCredentials,
                    IDWebErrorMessage.ClientSecretAndCertficateNull);
            }
        }
    }
}
