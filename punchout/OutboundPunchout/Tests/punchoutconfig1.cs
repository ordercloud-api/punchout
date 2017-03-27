using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IntegrationsDemo.OutboundPunchout
{
	//user lower case class name to avoid case sensitivity in urls. the config name is a param in the incoming urls.
	public class punchoutconfig1 : BasePunchoutConfig
	{
		public override string AdminAppId => "91588f4f-1262-4c35-9370-d22f3d057ea1";
		public override string AdminAppSecret => "punchoutAppSecret";
		public override string BrowserFormPost => $"http://someurlother.com/PunchoutOrdermessage/{PunchoutConfigName}";
		public override string OrderRequestUrl => "https://someurlother.com/PunchoutOrderRequest.hcf";
		public override string SetupRequestUrl => "https://someurlother.com/PunchoutSetupRequest.hcf";
		public override string SecretHashKey => "punchout1secrethashkey";
		public override string BrowserFormPostRedirect => "";
		public override string PunchoutProductId => "PunchoutProduct";

		public punchoutconfig1() : base()
		{
			AllowedBuyerClientIds.Add("e9947356-f9c5-4a1d-b5dc-0581ac8d76da");
			SetupRequestMappings.Add(new DocumentValue("UniqueUserName", "", "cXML/Request/PunchOutSetupRequest/Extrinsic[@name='UserEmail']"));
			HeaderMappings.Add(new DocumentValue("FromCredDomain", "from domain value", "cXML/Header/From/Credential/@domain"));
			HeaderMappings.Add(new DocumentValue("FromIdentity", "AN01000002779-T", "cXML/Header/From/Credential/Identity"));
			HeaderMappings.Add(new DocumentValue("ToCredDomain", "to domain value", "cXML/Header/To/Credential/@domain"));
			HeaderMappings.Add(new DocumentValue("ToIdentity", "134470637-T", "cXML/Header/To/Credential/Identity"));
			HeaderMappings.Add(new DocumentValue("SenderCredDomain", "sender domain value", "cXML/Header/Sender/Credential/@domain"));
			HeaderMappings.Add(new DocumentValue("SenderIdentity", "ariba-network-catalog-tester@ariba.com", "cXML/Header/Sender/Credential/Identity"));
			HeaderMappings.Add(new DocumentValue("SenderSecret", "four51four51", "cXML/Header/Sender/Credential/SharedSecret"));
		}
	}
}