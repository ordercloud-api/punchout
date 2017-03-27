using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Microsoft.IdentityModel.Tokens;


namespace IntegrationsDemo.OutboundPunchout
{
	public class DocumentValue
	{
		public string Xpath { get; }
		public string Name { get; }
		public string Value { get; set; }

		public DocumentValue(string name, string value, string xpath)
		{
			this.Name = name; this.Value = value; this.Xpath = xpath;
		}
	}
	public class BuyerToken
	{
		public string CurrentOrderID { get; set; }
		public string Username { get; set; }
		public string UserID { get; set; }
		public string clientID { get; set; }
		public string PunchoutName { get; set; }
		public string BuyerID { get; set; }
		public double dateSigned { get; set; }

	}
	public class Util
	{
		public static IPunchoutConfig GetPunchoutConfig(string name)
		{
			var fullName = $"IntegrationsDemo.OutboundPunchout.{name.ToLower()}";
			var t = Type.GetType(fullName);
			if (t == null)
				throw new Exception($"Type {fullName} not found");
			var constructor = t.GetConstructor(new Type[] { });
			if (constructor == null)
				throw new Exception($"{name} config must have a parameterless constructor");

			return (IPunchoutConfig)constructor.Invoke(new object[] {});
		}
		public static string GenerateBuyerCookie(BuyerToken token, string secret)
		{
			
			//using a separator to keep the string as short as possible. It's possible these values will have to be stored in a database if the vendor system can't handle longer buyer cookie values. One could also work the token in the punchoutordermessage url, but some systems may require that url to be static.
			var sessionValues = Encoding.UTF8.GetBytes($"{token.Username}/{token.UserID}/{token.clientID}/{token.PunchoutName}/{token.CurrentOrderID}/{token.BuyerID}/{token.dateSigned}/");

			return $"{Base64UrlEncoder.Encode(sessionValues)}.{GenerateCookieHash(sessionValues, secret)}";
		}

		private static string GenerateCookieHash(byte[] sessionValues, string secret)
		{
			var keyBytes = Encoding.UTF8.GetBytes(secret);
			var hmac = new HMACSHA256(keyBytes);
			var hash = hmac.ComputeHash(sessionValues);
			return Base64UrlEncoder.Encode(hash);
		}

		public static BuyerToken ValidateBuyerCookie(string cookie, string secret)
		{
			var sessionValues = Base64UrlEncoder.DecodeBytes(cookie.Split('.')[0]);
			var hashFromCookie = cookie.Split('.')[1];

			if (GenerateCookieHash(sessionValues, secret) != hashFromCookie)
				throw new Exception("this is not a valid buyer cookie");

			var sessionValueStrings = Encoding.UTF8.GetString(sessionValues).Split('/');

			var buyerToken = new BuyerToken
			{
				Username = sessionValueStrings[0],
				UserID = sessionValueStrings[1],
				clientID = sessionValueStrings[2],
				PunchoutName = sessionValueStrings[3],
				CurrentOrderID = sessionValueStrings[4],
				BuyerID = sessionValueStrings[5],
				dateSigned = Convert.ToDouble(sessionValueStrings[6])
			};
			if (TimeInHours() - buyerToken.dateSigned > 8) //or whatever time makes sense
				throw new Exception("buyer cookie has expiried");

			return buyerToken;
		}

		public static string ReplaceTemplateValues(XmlDocument doc, List<DocumentValue> mappings)
		{
			string missingNode = "";

			mappings.ForEach(v => {
				                      var n = doc.SelectSingleNode(v.Xpath);
				                      if (n == null)
					                      missingNode += $"{v.Xpath} node missing from template\r\n";
				                      else
					                      n.InnerText = v.Value;
			});
			return missingNode;
		}

		public static double TimeInHours()
		{
			return (DateTime.Now - DateTime.MinValue).TotalHours;
		}
		public static string ReadEmbeddedResource(string file)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var resourceName = file;

			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			using (StreamReader reader = new StreamReader(stream))
			{
				return reader.ReadToEnd();
			}
		}
		public static Action<string> Log = message =>
		{
			if (ConfigurationManager.AppSettings["verboseLogging"] == "true")
				System.IO.File.AppendAllText(ConfigurationManager.AppSettings["logFilePath"], message + "\r\n");
		};
	}
}