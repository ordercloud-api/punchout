cXML Punchout can be used to connect a procurement system (often Ariba) to various vendors. This project was built as a demonstration of how one could configure an asp.net web application to manage the communications between an OrderCloud buyer (not Ariba) and multiple external Punchout sellers. There’s also a, probably more common, scenario where the users will be coming from their procurement system into an OrderCloud catalog to purchase items. For simplicity, the former can be referred to as an outbound punchout and the later an inbound punchout. I fully expect to add inbound punchout to this demo when it becomes needed, but for now, it only contains a demo of outbound punchout. This document assumes you have a basic understanding of cXML punchout and the ability to deploy an asp.net web application. 

There are a number configurations needed to get all the systems involved to work correctly. Let’s consider a case where buyer users need to Punchout to a seller called Officemart to buy the office supplies for their office. 

* **Add a class to the punchout manager.** that inherits BasePunchoutConfig. See demopunchout.cs for an example. Make the class name all lower case which makes finding it easier from a case insensitive URLs. Let’s call it officemart.cs. There is a dynamic list of mappings that will be inserted into the appropriate cXML documents when they’re sent. Most punchouts will be configured similarly, but there will always be one that is slightly different which warrants having a way to accommodate the differences. There are some that are unlikely to be different from seller to seller which are stored in BasePunchoutConfig.cs. It’s possible they’ll have to be overridden in the individual config classes. 

* **Create a seller application in the OrderCloud dashboard**. Configure the application to have a client secret and back office user. For simplicity in this demo, the values are stored in officemart.cs AdminAppId and AdminAppSecret. These 2 pieces of information should be considered sensitive and storing it in code may not be the most secure way of handling them.

* **Configure officemart.cs BrowserFormPost.** For debugging locally, it’s set at [http://localhost:29692/PunchoutOrdermessage/{PunchoutConfigName](http://localhost:29692/PunchoutOrdermessage/%7BPunchoutConfigName)} for public hosting the domain, port, and likely changing to https would be needed, but the path after the domain stays as is.

* **Configure officemart.cs with OrderRequestUrl and SetupRequestUrl.** They will be provided by the seller. 

* **Configure officemart.cs with SecretHashKey.** One of the functions of punchout manager is to securely remember the user between the outgoing SetupRequest and the returning PunchoutOrderMessage. Instead of using a datasource and sending the ID along with the SetupRequest, this demo simply signs the cookie with the SecretHashKey as a way to know the values have not been tampered with. Any short secret phrase will do and it should also be considered a sensitive piece of data. You may decide a server side datasource is a smarter and more secure way to store session values. It’s also possible a seller system will have field size limits on the BuyerCookie which would also be solved by using a datasource.

* **Configure officemart.cs with BrowserFormPostRedirect.** When the user returns from the external session with the PunchoutOrderMessage, the punchout manager needs to know where in the OrderCloud buying application to redirect the browser.

* **Configure officemart.cs with a PunchoutProductID.** An order in OrderCloud requires a configured product (it’s on the short term road map to allow inserting a lineitem without an existing product). Create one and assign it to the buyer company. All the line items will be this product, but will have product details from the seller the LineItem xp. 

* **Configure officemart.cs with the AllowedBuyerClientIds.** This is simply a check of the OrderCloud buyer application id to make sure the buyer is authorized to use the punchout.

* **Configure offciemart.cs with xpath mappings and values.** This is where some cXML knowledge is helpful, but demopunchout.cs is a good starting point for data that will likely have to be exchanged with the seller. 

* **Configure order submit webhook.** This demo includes a controller to take an OrderCloud order submit web hook (configured in the dashboard) as a trigger to send along any orders to the punchout sellers via the OrderRequestUrl. If that’s the point when your seller’s OrderRequest should be posted, configure a web hook to point to http://{your.project.domain}/webhookordersubmit

* **Configure buyer application for setup request.** The point at which the user goes to the external seller is entirely up to the buyer app developer. POST the following JSON to http://{your.project.domain}/OutBoundSetupRequest 

{ 

  "punchoutName": "officemart",

  "buyerID": null,

  "access_token": null, //buyers ordercloud api access token

  "currentOrderID": null, //order id where items should be added on return from the external seller

  "shiptoID": null, //optional if a default shipping address should be put into the setuprequest

  "selectedItemID": null, //optional data for the setuprequest

  "selectedItemAuxID": null //optional data for the setuprequest

}



