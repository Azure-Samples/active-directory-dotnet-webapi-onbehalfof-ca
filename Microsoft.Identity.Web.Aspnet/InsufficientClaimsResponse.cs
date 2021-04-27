using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Aspnet
{
    public class InsufficientClaimsResponse
	{
		public string Code { get; set; }
		public string Message { get; set; }
		public string AdditionalInfo { get; set; }
		public ResponseInnerError InnerError { get; set; }
    }
	public class ResponseInnerError
	{
		public DateTime Date { get; set; }
		public string RequestId{get;set;}
		public string ClientRequestId { get; set; }
	}
}
