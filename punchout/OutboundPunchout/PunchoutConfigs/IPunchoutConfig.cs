using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;
using Microsoft.IdentityModel.Tokens;

namespace IntegrationsDemo.OutboundPunchout
{
	public interface IPunchoutConfig
	{
		string SecretHashKey { get; }
		string BrowserFormPost { get; }
		string BrowserFormPostRedirect { get; }
		string SetupRequestTemplateResource { get; }
		string OrderRequestTemplateResource { get; }
		string SetupRequestUrl { get; }
		string OrderRequestUrl { get; }
		List<string> AllowedBuyerClientIds { get; }
		string AdminAppId { get; }
		string AdminAppSecret { get; }
		List<DocumentValue> SetupRequestMappings { get; }
		List<DocumentValue> OrderRequestMappings { get; }
		List<DocumentValue> HeaderMappings { get; }
		string PunchoutConfigName { get; }
		string PunchoutProductId { get; }
	}
}