using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;

namespace IntegrationsDemo.OutboundPunchout
{
    public class PunchoutOrdermessageController : ApiController
    {
	    [HttpPost, Route("Punchoutordermessage/{punchoutName}")]
	    public async Task<HttpResponseMessage> ReceiveBrowserFormPost(string punchoutName)
	    {
		    Util.Log($"starting browser form post {punchoutName}");
			var config = Util.GetPunchoutConfig(punchoutName);

			var formData = await this.Request.Content.ReadAsFormDataAsync();
			var xd = XDocument.Load(new MemoryStream(Convert.FromBase64String(formData["cxml-base64"])));
		    Util.Log(xd.ToString());

			var buyerCookie = xd.Descendants("BuyerCookie").FirstOrDefault().Value;
			Util.Log($"buyer cookie: {buyerCookie}");
			
			//will throw exception on invalid cookie
			var validatedToken = Util.ValidateBuyerCookie(buyerCookie, config.SecretHashKey);
			var api = await OrderCloudAPI.InitializeByClientCredentials(config.AdminAppId, config.AdminAppSecret);
		    await api.ImpersonateBuyerUser(validatedToken.BuyerID, validatedToken.UserID, validatedToken.clientID, "OverrideUnitPrice", "ProductReader", "ProductAdmin");
		    var order = await api.GetOrder(validatedToken.BuyerID, validatedToken.CurrentOrderID);
		    if (order.ID == null)
			    order = await api.Createorder(validatedToken.BuyerID, validatedToken.CurrentOrderID);

			var en = xd.Descendants("ItemIn").GetEnumerator();
		    while (en.MoveNext())
				await Additem(en.Current, api, validatedToken, config.PunchoutProductId);

		    if (config.BrowserFormPostRedirect != string.Empty)
		    {
			    var response = this.Request.CreateResponse(HttpStatusCode.Redirect);
			    response.Headers.Location = new Uri(config.BrowserFormPostRedirect);
			    return response;
		    }
		    else
		    {
				var lineitems = await api.ListLineItems(validatedToken.BuyerID, validatedToken.CurrentOrderID);
				return this.Request.CreateResponse(HttpStatusCode.OK, lineitems);
			}
	    }

	    private async Task Additem(XElement item, OrderCloudAPI api, BuyerToken validatedToken, string productID)
	    {
			var lineItem = new OrderCloudLineItem
		    {
			    ProductID = productID,
			    Quantity = Convert.ToInt32(item.Attributes().Where(x => x.Name == "quantity").FirstOrDefault()?.Value),
			    UnitPrice = Convert.ToDecimal(item.Descendants("Money").FirstOrDefault()?.Value)
			};
		    lineItem.xp.PunchoutName = validatedToken.PunchoutName;
			lineItem.xp.SupplierPartAuxiliaryID = item.Descendants("SupplierPartAuxiliaryID").FirstOrDefault()?.Value;
			lineItem.xp.SupplierPartID = item.Descendants("SupplierPartID").FirstOrDefault()?.Value;
		    
			//since the ShortName element is also a child of description, it's werid to pull the sibling text which is also a child of description. There must be a better way.<Description><ShortName>some name</ShortName>some other additional text</Description>
			lineItem.xp.Description = item.Descendants("Description").DescendantNodesAndSelf()
				.FirstOrDefault(x => x.NodeType == XmlNodeType.Text && x.Parent.Name == "Description")?
				.ToString();
			lineItem.xp.ShortName = item.Descendants("ShortName").FirstOrDefault()?.Value;
			await api.CreateLineItem(validatedToken.BuyerID, validatedToken.CurrentOrderID, lineItem);
	    }
    }
}
