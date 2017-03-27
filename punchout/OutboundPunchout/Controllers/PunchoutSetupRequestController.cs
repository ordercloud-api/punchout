using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml;
using IntegrationsDemo;
using Flurl;
using Flurl.Http;

namespace IntegrationsDemo.OutboundPunchout
{
	public class SetupResponse
	{
		public string StatusCode { get; set; }
		public string Message { get; set; }
		public string StartURL { get; set; }
	}

	public class SetupRequest
	{
		public string punchoutName { get; set; }
		public string buyerID { get; set; }
		public string access_token { get; set; }
		public string currentOrderID { get; set; }
		public string shiptoID { get; set; }
		public string selectedItemID { get; set; }
		public string selectedItemAuxID { get; set; }
	}
	public class PunchoutSetupRequestController : ApiController
    {
		[Route("OutBoundSetupRequest"), HttpPost]
		public async Task<HttpResponseMessage> StartPunchoutSession(SetupRequest setup)
		{
			Func<string, string, HttpResponseMessage> fail = (message, punchoutCode) => this.Request.CreateResponse(HttpStatusCode.BadRequest, new { StatusCode = punchoutCode, Message = message });
		
			if (setup.access_token == null || setup.punchoutName == null || setup.currentOrderID == null || setup.buyerID == null)
				return fail("Please pass AccessToken, PunchoutName, CurrentOrderID, and buyerID", null);

			var api = new OrderCloudAPI(setup.access_token);
			var ocUser = await api.GetCurrentUser();
			var config = Util.GetPunchoutConfig(setup.punchoutName);
			XmlDocument doc = new XmlDocument();
			
			//this provides a bit of security since the access token is validated against the OrderCloud api. This check will verify that an authorized user is using the punchout
			if (config.AllowedBuyerClientIds.Where(x=> x.ToLower() == api.AccessToken.ClientID.ToLower()).Count() == 0 )
				return fail("client id is not allowed to use this punchout config.", "");

			//load the setuprequest template specified in the config. Normally only one template will be needed, but it happens that vendors don't closely follow the spec and it might be easier to have a different template for that config
			doc.LoadXml(Util.ReadEmbeddedResource(config.SetupRequestTemplateResource));

			//if a shipto id is specified, pull the data from the OC api and add it to the setupRequest
			await MapAddressElement(doc, api, setup.shiptoID);

			//if a selected item is specified, add it directly to the setupRequest
			MapSelectedItem(doc, setup.selectedItemID, setup.selectedItemAuxID);

			//set the user based values in the in the Setuprequest 
			config.SetupRequestMappings.First(x=>x.Name == "BuyerCookie").Value = Util.GenerateBuyerCookie(new BuyerToken{
				UserID = ocUser.ID,
				Username = ocUser.Username,
				clientID = api.AccessToken.ClientID,
				PunchoutName = setup.punchoutName,
				CurrentOrderID = setup.currentOrderID,
				dateSigned = Util.TimeInHours(),
				BuyerID = setup.buyerID
			},
			config.SecretHashKey);
			config.SetupRequestMappings.First(x => x.Name == "UniqueUserName").Value = ocUser.Username;
			config.SetupRequestMappings.First(x => x.Name == "ContactName").Value = $"{ocUser.FirstName} {ocUser.LastName}";
			config.SetupRequestMappings.First(x => x.Name == "ContactEmail").Value = ocUser.Email;

			//load the Setuprequest values specific to this punchout config into the document
			var missingNode = Util.ReplaceTemplateValues(doc, config.HeaderMappings);
			missingNode += Util.ReplaceTemplateValues(doc, config.SetupRequestMappings);

			if (missingNode != "")
				return fail(missingNode, null);

			//post the setuprequest to the vendor
			var stringResponse = await config.SetupRequestUrl.PostStringAsync(doc.OuterXml).ReceiveString();
			var response = new XmlDocument();
			response.LoadXml(stringResponse);

			var code = response.SelectSingleNode("cXML/Response/Status/@code")?.InnerText;
			if (code != "200")
				return fail(response.SelectSingleNode("cXML/Response/Status/@text")?.InnerText, code);
			else
			{
				// the client app should look for a start url and redirect the browser there.
				var httpResponse = this.Request.CreateResponse(HttpStatusCode.Redirect);
				httpResponse.Headers.Location = new Uri(response.SelectSingleNode("cXML/Response/PunchOutSetupResponse/StartPage/URL").InnerText);
				return httpResponse;
			}
		}
		
		private void MapSelectedItem(XmlDocument xml, string id, string auxID)
	    {
		    string xpath = "cXML/Request/PunchOutSetupRequest/SelectedItem";
			if (id == null)
			{
				xml.SelectSingleNode("cXML/Request/PunchOutSetupRequest").RemoveChild(xml.SelectSingleNode(xpath));
				return;
			}
			xml.SelectSingleNode($"{xpath}/ItemID/SupplierPartID").InnerText = id;
			xml.SelectSingleNode($"{xpath}/ItemID/SupplierPartAuxiliaryID").InnerText = auxID;
		}
	    private async Task MapAddressElement(XmlDocument xml, OrderCloudAPI api, string shiptoID)
	    {
		    var xpath = "cXML/Request/PunchOutSetupRequest/ShipTo";

			if (shiptoID == null)
			{
				xml.SelectSingleNode("cXML/Request/PunchOutSetupRequest").RemoveChild(xml.SelectSingleNode(xpath));
				return;
		    }

		    var address = await api.GetUserAddress(shiptoID);
		    xml.SelectSingleNode($"{xpath}/Address/@addressID").InnerText = address.ID;
			xml.SelectSingleNode($"{xpath}/Address/PostalAddress/@name").InnerText = address.AddressName;
		    xml.SelectSingleNode($"{xpath}/Address/PostalAddress/DeliverTo").InnerText = $"{address.FirstName} {address.LastName}";
		    xml.SelectSingleNode($"{xpath}/Address/PostalAddress/Street").InnerText = $"{address.Street1} {address.Street2}";
			xml.SelectSingleNode($"{xpath}/Address/PostalAddress/State").InnerText = address.State;
			xml.SelectSingleNode($"{xpath}/Address/PostalAddress/PostalCode").InnerText = address.Zip;
			xml.SelectSingleNode($"{xpath}/Address/PostalAddress/Country/@isoCountryCode").InnerText = address.Country;
			xml.SelectSingleNode($"{xpath}/Address/PostalAddress/Country").InnerText = address.Country;
		}
	}
}
