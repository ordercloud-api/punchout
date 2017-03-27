using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Flurl;
using Flurl.Http;
using IntegrationsDemo.OutboundPunchout;

namespace IntegrationsDemo
{
	public class OrderCloudAPI
	{
		private static readonly string _apiUrl = "https://api.ordercloud.io";
		private static readonly string _authUrl = "https://auth.ordercloud.io";
		private string _accessToken;
		public AccessToken AccessToken{ get; private set; }
		
		public OrderCloudAPI(string bearerToken)
		{
			init(bearerToken);
		}

		private void init(string bearerToken)
		{
			_accessToken = bearerToken;
			var tokenHandler = new JwtSecurityTokenHandler();
			var jwt = ((JwtSecurityToken)tokenHandler.ReadToken(bearerToken));
			this.AccessToken = new AccessToken(jwt.Claims.FirstOrDefault(x => x.Type == "cid").Value,
				jwt.Claims.Where(x => x.Type == "role").Select<Claim, string>(x => x.Value).ToArray());
		}
		public async Task ImpersonateBuyerUser(string buyerID, string userid, string clientID, params string[] roles)
		{
			var response = await _apiUrl.AppendPathSegments("v1", "buyers", buyerID, "users", userid, "accesstoken")
				.ConfigureClient(x =>
				{
					x.OnError = call =>
					{
						
						throw new Exception($"get buyer access token failed {call.Response.ReasonPhrase}\r\n{call.ErrorResponseBody}");
					};
				} )
				.WithOAuthBearerToken(_accessToken)
				.PostJsonAsync(new {ClientID = clientID, Claims = roles})
				.ReceiveJson();
			
			init(response.access_token);
		}
		public static async Task<OrderCloudAPI> InitializeByClientCredentials(string clientid, string clientSecret)
		{
			var auth = await _authUrl.AppendPathSegments("oauth", "token")
				.PostUrlEncodedAsync(new
				{
					client_id = clientid,
					client_secret = clientSecret,
					grant_type = "client_credentials",
					scope = "OrderAdmin OrderReader ProductAdmin OverrideUnitPrice"
				}).ReceiveJson();
			return new OrderCloudAPI(auth.access_token);
		}
		public async Task<OrderCloudUser> GetCurrentUser()
		{
			var user = await $"{_apiUrl}/v1/me"
				.WithOAuthBearerToken(_accessToken)
				.GetJsonAsync<OrderCloudUser>();
			return user;
		}
		public async Task<OrderCloudAddress> GetUserAddress(string id)
		{
			var address = await $"{_apiUrl}/v1/me/addresses"
				.AppendPathSegment(id)
				.WithOAuthBearerToken(_accessToken)
				.GetJsonAsync<OrderCloudAddress>();
			return address;
		}
		public async Task<OrderCloudOrder>GetOrder(string buyerID, string id)
		{
			string url = $"{_apiUrl}/v1/me/orders/{id}";
			return await url
				.AllowHttpStatus("404")
				.WithOAuthBearerToken(_accessToken)
				.GetJsonAsync<OrderCloudOrder>();
		}
		public async Task<OrderCloudOrder> Createorder(string buyerID, string orderID)
		{
			return await $"{_apiUrl}/v1/buyers/{buyerID}/orders"
				.WithOAuthBearerToken(_accessToken)
				.ConfigureClient(x =>
				{
					x.OnError = call =>
					{

						throw new Exception($"create order failed {call.Response.ReasonPhrase}\r\n{call.ErrorResponseBody}");
					};
				})
				.PostJsonAsync(new OrderCloudOrder {ID=orderID, Type="Standard"})
				.ReceiveJson<OrderCloudOrder>();
		}
		public async Task<OrderCloudLineItem> CreateLineItem(string buyerID, string orderID, OrderCloudLineItem item)
		{
			return await $"{_apiUrl}/v1/buyers/{buyerID}/orders/{orderID}/lineitems"
				.WithOAuthBearerToken(_accessToken)
				.PostJsonAsync(item)
				.ReceiveJson<OrderCloudLineItem>();
		}
		public async Task<OrderCloudList<OrderCloudLineItem>>ListLineItems(string buyerID, string orderID)
		{
			return await $"{_apiUrl}/v1/buyers/{buyerID}/orders/{orderID}/lineitems"
				.WithOAuthBearerToken(_accessToken)
				.GetAsync()
				.ReceiveJson<OrderCloudList<OrderCloudLineItem>>();
		}
	}

	public class ListMeta
	{
		public int Page { get; set; }
		public int PageSize { get; set; }
		public int TotalCount { get; set; }
		public int TotalPages { get; set; }
	}
	public class OrderCloudList<ListType>
	{
		public List<ListType> Items { get; set; }
		public ListMeta Meta { get; set; }
	}

	public class AccessToken
	{
		public AccessToken(string clientID, string[] roles)
		{
			ClientID = clientID;
			Roles = roles;
		}
		public string ClientID { get; }
		public string[] Roles { get; }
	}

	public class OrderCloudAddress
	{
		public string ID { get; set; }
		public string CompanyName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Street1 { get; set; }
		public string Street2 { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string Country { get; set; }
		public string AddressName { get; set; }
		public string Zip { get; set; }
	}
	public class OrderCloudUser
	{
		public string ID { get; set; }
		public string Username { get; set; }
		public string Email { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
	}

	public class OrderCloudOrder
	{
		public DateTime? DateSubmitted { get; set; }
		public string ID { get; set; }
		public string BillingAddressID { get; set; }
		public string Comments { get; set; }
		public string Type { get; set; }
	}

	public class OrderCloudWebHook
	{
		public dynamic RouteParams { get; set; }
		public string UserToken { get; set; }
		public string Route { get; set; }
	}

	public class OrderCloudLineItem
	{
		public OrderCloudLineItem()
		{
			xp = new PunchoutProductInfo();
		}
		public string ShippingAddressID { get; set; }
		public string ID { get; set; }
		public string ProductID { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public PunchoutProductInfo xp { get; set; }
	}

	public class PunchoutProductInfo
	{
		public string SupplierPartID { get; set; }
		public string SupplierPartAuxiliaryID { get; set; }
		public string Description { get; set; }
		public string ShortName { get; set; }
		public string PunchoutName { get; set; }
	}

}