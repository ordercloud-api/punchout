using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using Flurl.Http.Testing;
using IntegrationsDemo.OutboundPunchout;
using IntegrationsDemo;

namespace IntegrationsDemo.OutboundPunchout.Tests
{
	public class OrderRequestTests
	{
		[Test]
		public async Task HandleWebhook()
		{
			var order = new OrderCloudOrder {ID = "123", BillingAddressID = "address1", Comments = "some order comments", DateSubmitted=DateTime.Now};
			
			var orderItems = new System.Collections.Generic.List<OrderCloudLineItem>{
					new OrderCloudLineItem {ShippingAddressID="address1", Quantity =2, UnitPrice = 3, ProductID = "PunchoutProduct", xp = new PunchoutProductInfo {Description = "description from order message", PunchoutName = "punchoutconfig1", ShortName = "short name from order message", SupplierPartAuxiliaryID="a-unique-id-to-assert-123", SupplierPartID="abc"} },
					new OrderCloudLineItem {ShippingAddressID="address1", Quantity =1, UnitPrice = 1, ProductID = "PunchoutProduct", xp = new PunchoutProductInfo {Description = "description from order message line 2", PunchoutName = "punchoutconfig1", ShortName = "short name from order message line 2", SupplierPartAuxiliaryID="1234", SupplierPartID="abcd"} },
					new OrderCloudLineItem {ShippingAddressID="address2", Quantity =1, UnitPrice = 1, ProductID = "PunchoutProduct", xp = new PunchoutProductInfo {Description = "description from order message line 2", PunchoutName = "punchoutconfig1", ShortName = "short name from order message line 2", SupplierPartAuxiliaryID="12345", SupplierPartID="abcde"} },
					new OrderCloudLineItem {ShippingAddressID="address1", Quantity =1, UnitPrice = 1, ProductID = "PunchoutProduct", xp = new PunchoutProductInfo {Description = "description from order message line 3 punchout 2", PunchoutName = "punchoutconfig2", ShortName = "short name from order message line 3 punchout 2", SupplierPartAuxiliaryID="12345", SupplierPartID="abcde"} }};
			new OrderCloudLineItem { ShippingAddressID = "address1", Quantity = 1, UnitPrice = 1, ProductID = "not a punchout product" };
			var controller = new OrderSubmitWebhookController();
			var httpTest = new HttpTest();

			httpTest.RespondWithJson(order);
			httpTest.RespondWithJson(new OrderCloudList<OrderCloudLineItem>{Meta = new ListMeta {Page = 1, TotalCount = 3},Items = orderItems});

			var addresses = new Dictionary<string, OrderCloudAddress>();
			addresses.Add("address1", new OrderCloudAddress { AddressName = "oca name", City = "my city", CompanyName = "my companyName", Country = "US", FirstName = "Jeff", LastName = "Ilse", State = "MN", Street1 = "My street address", Street2 = "my street 2", ID = "address1", Zip = "55804" });
			addresses.Add("address2",new OrderCloudAddress { AddressName = "2oca name", City = "2my city", CompanyName = "2my companyName", Country = "US", FirstName = "2Jeff", LastName = "2Ilse", State = "MN", Street1 = "2My street address", Street2 = "my street 2", ID = "address2", Zip = "55804" });
			httpTest.RespondWithJson(addresses["address1"]);
			httpTest.RespondWithJson(addresses["address2"]);

			await controller.ProcessOrderSubmit("eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJ1c3IiOiJidXllcnVzZXJQdW5jaG91dDEyMyIsImNpZCI6IjEzNGE3M2YzLTRiMTItNGY5Yi05YTliLWJmMWUxMzdlMzU5OCIsInVzcnR5cGUiOiJidXllciIsImlzcyI6Imh0dHBzOi8vYXV0aC5vcmRlcmNsb3VkLmlvIiwiYXVkIjoiaHR0cHM6Ly9hcGkub3JkZXJjbG91ZC5pbyIsImV4cCI6MTQ5MDA1OTUxOSwibmJmIjoxNDkwMDIzNTE5fQ._RYABiZY695yJHCcUVBrrSw8id3rNDdDrrPDMa22cDs", "123", "buyerid");

			var config1 = Util.GetPunchoutConfig("punchoutconfig1");
			var config2 = Util.GetPunchoutConfig("punchoutconfig2");

			var call = httpTest.CallLog.Find(x => x.Url == config1.OrderRequestUrl && x.Request.Method == HttpMethod.Post);
			var order1XML = new XmlDocument();
			order1XML.LoadXml(call.RequestBody);
			CheckOrderRequest(order1XML, config1, order, orderItems, addresses);

			call = httpTest.CallLog.Find(x => x.Url == config2.OrderRequestUrl && x.Request.Method == HttpMethod.Post);
			var order2XML = new XmlDocument();
			order2XML.LoadXml(call.RequestBody);
			CheckOrderRequest(order2XML, config2, order, orderItems, addresses);

			httpTest.Dispose();
		}

		private void CheckOrderRequest(XmlDocument xmlOrder, IPunchoutConfig config, OrderCloudOrder order, List<OrderCloudLineItem> items, Dictionary<string, OrderCloudAddress> addresses)
		{
			Action<string, string, List<DocumentValue>> checkValue = (name, value, list) =>
			{
				var docVal = list.Find(x => x.Name == name);
				var shouldBe = value ?? docVal.Value;
				Assert.AreEqual(shouldBe, xmlOrder.SelectSingleNode(docVal.Xpath)?.InnerText, $"{docVal.Name} was either not found or not equal to {shouldBe}");
			};

			checkValue("ToIdentity", null, config.HeaderMappings);
			checkValue("ToCredDomain", null, config.HeaderMappings);
			checkValue("FromIdentity", null, config.HeaderMappings);
			checkValue("FromCredDomain", null, config.HeaderMappings);
			checkValue("SenderIdentity", null, config.HeaderMappings);
			checkValue("SenderCredDomain", null, config.HeaderMappings);
			checkValue("SenderSecret", null, config.HeaderMappings);
			checkValue("OrderID", order.ID, config.OrderRequestMappings);
			
			CheckAddress(xmlOrder.SelectSingleNode("cXML/Request/OrderRequest/OrderRequestHeader/BillTo/Address/PostalAddress"), addresses[order.BillingAddressID]);
			var itemCount = 0;
			decimal orderTotal = 0;
			items.FindAll(x => x.xp != null && x.xp.PunchoutName == config.PunchoutConfigName).ForEach(item =>
			{
				var itemNode = xmlOrder.SelectNodes($"cXML/Request/OrderRequest/ItemOut")[itemCount];
				CheckAddress(itemNode.SelectSingleNode("ShipTo/Address/PostalAddress"), addresses[item.ShippingAddressID]);
				Assert.AreEqual(item.xp.SupplierPartAuxiliaryID, itemNode.SelectSingleNode("ItemID/SupplierPartAuxiliaryID").InnerText);
				Assert.AreEqual(item.xp.SupplierPartID, itemNode.SelectSingleNode("ItemID/SupplierPartID").InnerText);
				Assert.AreEqual(item.UnitPrice.ToString("0.00"), itemNode.SelectSingleNode("ItemDetail/UnitPrice/Money").InnerText);
				Assert.AreEqual(item.xp.Description, itemNode.SelectSingleNode("ItemDetail/Description").InnerText);
				orderTotal += (item.UnitPrice*item.Quantity);
				itemCount++;
			});
			Assert.AreEqual(itemCount, xmlOrder.SelectNodes("cXML/Request/OrderRequest/ItemOut").Count);
			checkValue("OrderTotal", orderTotal.ToString("0.00"), config.OrderRequestMappings);//order 1 = 2x3(unit price) + 1x1
		}

		private void CheckAddress(XmlNode node, OrderCloudAddress address)
		{
			Assert.AreEqual($"{address.FirstName} {address.LastName}", node.SelectSingleNode("DeliverTo").InnerText);
			Assert.AreEqual(address.Street1, node.SelectSingleNode("Street[1]").InnerText);
			Assert.AreEqual(address.Street2, node.SelectSingleNode("Street[2]").InnerText);
			Assert.AreEqual(address.City, node.SelectSingleNode("City").InnerText);
			Assert.AreEqual(address.State, node.SelectSingleNode("State").InnerText);
			Assert.AreEqual(address.Zip, node.SelectSingleNode("PostalCode").InnerText);
		}
	}
}

