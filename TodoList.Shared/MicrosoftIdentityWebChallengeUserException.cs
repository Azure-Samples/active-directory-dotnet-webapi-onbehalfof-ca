using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoList.Shared
{
    public class MicrosoftIdentityWebChallengeUserException : Exception
    {
        /// <summary>
        /// Exception thrown by MSAL when a user challenge is encountered.
        /// </summary>
        public MsalUiRequiredException MsalUiRequiredException { get; set; }

        /// <summary>
        /// Scopes to request.
        /// </summary>
        public string[] Scopes { get; set; }

        /// <summary>
        /// Specified userflow.
        /// </summary>
        public string Userflow { get; set; }

        /// <summary>
        /// Handles the user challenge for Blazor or Razor pages.
        /// </summary>
        /// <param name="msalUiRequiredException">Exception thrown by MSAL when a user challenge is encountered.</param>
        /// <param name="scopes">Scopes to request.</param>
        /// <param name="userflow">Userflow used in B2C.</param>
        public MicrosoftIdentityWebChallengeUserException(
            MsalUiRequiredException msalUiRequiredException,
            string[] scopes,
            string userflow = null)

        {
            MsalUiRequiredException = msalUiRequiredException;
            Scopes = scopes;
            Userflow = userflow;
        }
    }
}
