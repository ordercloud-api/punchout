using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IntegrationsDemo.OutboundPunchout
{
	//user lower case class name to avoid case sensitivity in urls. the config name is a param in the incoming urls.
	public class demopunchout : BasePunchoutConfig
	{
		public override string AdminAppId => "9B9B4CFA-6244-47D1-A9D8-9EAB76B7E76F";
		public override string AdminAppSecret => "punchoutAppSecret";
		//public override string BrowserFormPost => $"http://localhost:29692/PunchoutOrdermessage/{PunchoutConfigName}";
		public override string BrowserFormPost => $"http://integrationsdemo.azurewebsites.net/PunchoutOrdermessage/{PunchoutConfigName}";
		public override string OrderRequestUrl => "https://test.four51.com/ui/yiv1Qs0-s29A-pXunthxM4Fg-e-e/gmcc/PunchoutOrder.hcf";
		public override string SetupRequestUrl => "https://test.four51.com/ui/yiv1Qs0-s29A-pXunthxM4Fg-e-e/gmcc/PunchoutSetupRequest.hcf";
		public override string SecretHashKey => "punchout1secrethashkey";
		public override string BrowserFormPostRedirect => "";
		public override string PunchoutProductId => "PunchoutProduct";

		public demopunchout() : base()
		{
			AllowedBuyerClientIds.Add("134A73F3-4B12-4F9B-9A9B-BF1E137E3598");
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