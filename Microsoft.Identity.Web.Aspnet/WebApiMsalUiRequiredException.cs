using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;

namespace Microsoft.Identity.Web.Aspnet
{
    public class WebApiMsalUiRequiredException : Exception
    {
        private HttpResponseMessage httpResponseMessage;
        public WebApiMsalUiRequiredException(string message, HttpResponseMessage response) : base(message)
        {
            httpResponseMessage = response;
        }
        public HttpStatusCode StatusCode
        {
            get { return httpResponseMessage.StatusCode; }
        }

        public HttpResponseHeaders Headers
        {
            get { return httpResponseMessage.Headers; }
        }

        public HttpResponseMessage HttpResponseMessage
        {
            get { return httpResponseMessage; }
        }
    }
}
