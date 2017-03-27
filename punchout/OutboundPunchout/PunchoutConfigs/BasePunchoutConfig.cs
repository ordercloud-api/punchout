using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IntegrationsDemo.OutboundPunchout
{
	public abstract class BasePunchoutConfig : IPunchoutConfig
	{
		protected readonly List<string> _allowedBuyerClientIds = new List<string>();
		protected readonly List<DocumentValue> _headerMappings = new List<DocumentValue>();
		protected readonly List<DocumentValue> _orderRequestMappings = new List<DocumentValue>();
		protected readonly List<DocumentValue> _setupRequestmappings = new List<DocumentValue>();

		public List<string> AllowedBuyerClientIds => _allowedBuyerClientIds;
		public List<DocumentValue> OrderRequestMappings => _orderRequestMappings;
		public List<DocumentValue> SetupRequestMappings => _setupRequestmappings;
		public List<DocumentValue> HeaderMappings => _headerMappings;

		public abstract string AdminAppId { get; }
		public abstract string AdminAppSecret { get; }
		public abstract string BrowserFormPost { get; }
		public abstract string OrderRequestUrl { get; }
		public abstract string SetupRequestUrl { get; }
		public abstract string SecretHashKey { get; }
		public abstract string BrowserFormPostRedirect { get; }
		public abstract string PunchoutProductId { get; }
		public string OrderRequestTemplateResource => "IntegrationsDemo.OutboundPunchout.XmlTemplates.OrderRequest.xml";
		public string SetupRequestTemplateResource => "IntegrationsDemo.OutboundPunchout.XmlTemplates.SetupRequest.xml";

		public string PunchoutConfigName
		{
			get { return this.GetType().Name; }	
		}

		protected BasePunchoutConfig()
		{
			//these values set dynamically by the controller
			SetupRequestMappings.Add(new DocumentValue("ContactName", "", "cXML/Request/PunchOutSetupRequest/Contact/Name"));
			SetupRequestMappings.Add(new DocumentValue("ContactEmail", "", "cXML/Request/PunchOutSetupRequest/Contact/Email"));
			SetupRequestMappings.Add(new DocumentValue("BuyerCookie", "", "cXML/Request/PunchOutSetupRequest/BuyerCookie"));
			SetupRequestMappings.Add(new DocumentValue("BrowserFormPost", this.BrowserFormPost, "cXML/Request/PunchOutSetupRequest/BrowserFormPost/URL"));
			
			OrderRequestMappings.Add(new DocumentValue("OrderID", "", "cXML/Request/OrderRequest/OrderRequestHeader/@orderID"));
			OrderRequestMappings.Add(new DocumentValue("OrderDate", DateTime.Now.ToString("s"), "cXML/Request/OrderRequest/OrderRequestHeader/@orderDate"));
			OrderRequestMappings.Add(new DocumentValue("OrderTotal", "", "cXML/Request/OrderRequest/OrderRequestHeader/Total/Money"));
			HeaderMappings.Add(new DocumentValue("TimeStamp", DateTime.Now.ToString("s"), "cXML/@timestamp"));
			HeaderMappings.Add(new DocumentValue("PayloadID", Guid.NewGuid().ToString(), "cXML/@payloadID"));

			
		}
	}
}