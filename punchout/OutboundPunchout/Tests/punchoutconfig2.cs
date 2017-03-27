using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IntegrationsDemo.OutboundPunchout
{
	//user lower case class name to avoid case sensitivity in urls. the config name is a param in the incoming urls.
	public class punchoutconfig2 : BasePunchoutConfig
	{
		public override string AdminAppId => "8e7c32b9-ec04-4ad1-bb7e-af5f2875c48e";
		public override string AdminAppSecret => "punchoutAppSecret2";
		public override string BrowserFormPost => $"http://someurl.com/PunchoutOrdermessage/{PunchoutConfigName}";
		public override string OrderRequestUrl => "https://someurl.com/PunchoutOrderRequest.hcf";
		public override string SetupRequestUrl => "https://someurl.com/PunchoutSetupRequest.hcf";
		public override string SecretHashKey => "punchout2secrethashkey";
		public override string BrowserFormPostRedirect => "";
		public override string PunchoutProductId => "PunchoutProduct";

		public punchoutconfig2() : base()
		{
			AllowedBuyerClientIds.Add("4fe9b9f2-8517-4ed0-ad5a-32765cba0c6d");
			SetupRequestMappings.Add(new DocumentValue("UniqueUserName", "", "cXML/Request/PunchOutSetupRequest/Extrinsic[@name='UserEmail']"));
			HeaderMappings.Add(new DocumentValue("FromCredDomain", "from cred", "cXML/Header/From/Credential/@domain"));
			HeaderMappings.Add(new DocumentValue("FromIdentity", "from id", "cXML/Header/From/Credential/Identity"));
			HeaderMappings.Add(new DocumentValue("ToCredDomain", "to cred", "cXML/Header/To/Credential/@domain"));
			HeaderMappings.Add(new DocumentValue("ToIdentity", "to id", "cXML/Header/To/Credential/Identity"));
			HeaderMappings.Add(new DocumentValue("SenderCredDomain", "sender domain value", "cXML/Header/Sender/Credential/@domain"));
			HeaderMappings.Add(new DocumentValue("SenderIdentity", "sender id", "cXML/Header/Sender/Credential/Identity"));
			HeaderMappings.Add(new DocumentValue("SenderSecret", "sender secret", "cXML/Header/Sender/Credential/SharedSecret"));
		}
	}
}