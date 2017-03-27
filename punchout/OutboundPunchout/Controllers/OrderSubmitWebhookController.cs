using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;
using Flurl;
using Flurl.Http;

namespace IntegrationsDemo.OutboundPunchout
{
	
	public class OrderSubmitWebhookController : ApiController
	{
		[HttpPost, Route("WebhookOrdersubmit")]
		public async Task SubmitVendorPO(OrderCloudWebHook webHook)
		{
			Util.Log("starting webhook");
			Util.Log($"ut: {webHook.UserToken}");
			Util.Log($"order id: {webHook.RouteParams?.orderID}");
			Util.Log($"buyerid: {webHook.RouteParams?.buyerID}");
			try
			{
				await ProcessOrderSubmit(webHook.UserToken, (string)webHook.RouteParams.orderID, (string)webHook.RouteParams.buyerID);
			}
			catch(Exception e)
			{
				Util.Log(e.ToString());
			}
		}

		public class GroupedItemList
		{
			public string PunchoutName { get; set; }
			public IEnumerable<OrderCloudLineItem> Items { get; set; }
		}

		public async Task ProcessOrderSubmit(string bearerToken, string orderID, string buyerID)
		{
			var addresses = new Dictionary<string, OrderCloudAddress>();
			var api = new OrderCloudAPI(bearerToken);
			var order = await api.GetOrder(buyerID, orderID);
			Util.Log(order.ToString());
			var lineItemList = await api.ListLineItems(buyerID, orderID);
			
			Func<string, Task<OrderCloudAddress>> GetCachedAddress = async (id) =>
			{
				if (id == null || id == "")
					return null;
				
				if (addresses.ContainsKey(id))
					return addresses[id];
				else
				{
					var address = await api.GetUserAddress(id);
					addresses.Add(id, address);
					return address;
				}
			};
			
			//it's possible there will be line items from different punchout cofigs on this order
			var groups = lineItemList.Items.Where(x=>x.xp != null && x.xp.PunchoutName != null).GroupBy(x => x.xp.PunchoutName,(key, group) => new GroupedItemList {PunchoutName = key, Items = group}).GetEnumerator();
			var billingAddress = await GetCachedAddress(order.BillingAddressID);
			
			while (groups.MoveNext())
			{
				var lineItemGroup = groups.Current;
				var config = Util.GetPunchoutConfig(lineItemGroup.PunchoutName);
				var xml = new XmlDocument();
				var orderTotal = (decimal)0.00;

				xml.LoadXml(Util.ReadEmbeddedResource(config.OrderRequestTemplateResource));
				var itemTemplate = xml.SelectSingleNode("cXML/Request/OrderRequest/ItemOut").Clone();
				xml.SelectSingleNode("cXML/Request/OrderRequest").RemoveChild(xml.SelectSingleNode("cXML/Request/OrderRequest/ItemOut"));
				if(billingAddress != null)
					SetAddress(xml.SelectSingleNode("cXML/Request/OrderRequest/OrderRequestHeader/BillTo/Address/PostalAddress"), billingAddress);
				
				for (var i = 0; i < lineItemGroup.Items.Count(); i++)
				{
					var item = lineItemGroup.Items.ElementAt(i);
					var address = await GetCachedAddress(item.ShippingAddressID);
					if(address != null)
						SetAddress(itemTemplate.SelectSingleNode("ShipTo/Address/PostalAddress"), address);
					
					itemTemplate.SelectSingleNode("@quantity").InnerText = item.Quantity.ToString();
					itemTemplate.SelectSingleNode("@lineNumber").InnerText = i+1.ToString();
					itemTemplate.SelectSingleNode("ItemID/SupplierPartID").InnerText = item.xp.SupplierPartID;
					itemTemplate.SelectSingleNode("ItemID/SupplierPartAuxiliaryID").InnerText = item.xp.SupplierPartAuxiliaryID;
					itemTemplate.SelectSingleNode("ItemDetail/Description").InnerText = item.xp.Description;
					itemTemplate.SelectSingleNode("ItemDetail/UnitPrice/Money").InnerText = item.UnitPrice.ToString("0.00");
					xml.SelectSingleNode("cXML/Request/OrderRequest").AppendChild(itemTemplate.Clone());
					orderTotal += (item.UnitPrice*item.Quantity);
				}
				config.OrderRequestMappings.First(x => x.Name == "OrderID").Value = orderID;
				config.OrderRequestMappings.First(x => x.Name == "OrderTotal").Value = Math.Round(orderTotal, 2).ToString("0.00");
				Util.ReplaceTemplateValues(xml, config.OrderRequestMappings);
				Util.ReplaceTemplateValues(xml, config.HeaderMappings);

				Util.Log(xml.OuterXml);
				var response = await config.OrderRequestUrl.PostStringAsync(xml.OuterXml).ReceiveString();
				Util.Log(response);
			}
		}
		
		private void SetAddress(XmlNode node, OrderCloudAddress address)
		{
			node.SelectSingleNode("DeliverTo").InnerText = $"{address.FirstName} {address.LastName}";
			node.SelectSingleNode("Street[1]").InnerText = address.Street1;
			node.SelectSingleNode("Street[2]").InnerText = address.Street2;
			node.SelectSingleNode("City").InnerText = address.City;
			node.SelectSingleNode("State").InnerText = address.State;
			node.SelectSingleNode("PostalCode").InnerText = address.Zip;
		}

    }
}
