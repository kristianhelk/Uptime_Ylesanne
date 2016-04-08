using AmazoniTooted.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AmazoniTooted.Amazon;
using System.ServiceModel;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace AmazoniTooted.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Index()
        {
            return View();
        }

        //Otsingu tulemuse vaade
        public ActionResult Tulemus(HomeViewModel model)
        {
            //Juhul, kui tulemuse vaatesse satutakse ilma otsingusõna sisestamata
            if (model.Otsing == null)
            {
                return View("Index");
            }

            //Otsingusõna
            string keyword = model.Otsing;

            //Amazon Product Advertising API poole pöördumise ettevalmistamine

            BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
            binding.MaxReceivedMessageSize = int.MaxValue;

            AWSECommerceServicePortTypeClient client = new AWSECommerceServicePortTypeClient(binding, new EndpointAddress("https://webservices.amazon.com/onca/soap?Service=AWSECommerceService"));

            ItemSearch search = new ItemSearch();
            search.AssociateTag = ""; //Amazoni associate tag ning access key vajalikud
            search.AWSAccessKeyId = "";

            client.ChannelFactory.Endpoint.Behaviors.Add(new AmazonSigningEndpointBehavior());

            ItemSearchRequest request = new ItemSearchRequest();

            request.ResponseGroup = new string[] { "ItemAttributes", "Offers" };

            request.SearchIndex = "All";
            request.Keywords = keyword;

            //List otsingu tulemuste jaoks
            List<AmazonItem> itemList = new List<AmazonItem>();

            //Otsingu saatmine ning listi tulemustega täitmine
            for (var j = 0; j < 3; j++)
            {
                request.ItemPage = (j + 1).ToString();
                search.Request = new ItemSearchRequest[] { request };
                ItemSearchResponse response = client.ItemSearch(search);
                for (var i = 0; i < 10; i++)
                {
                    string hind;
                    if (response.Items[0].Item[i].OfferSummary != null)
                    {
                        hind = response.Items[0].Item[i].OfferSummary.LowestNewPrice.FormattedPrice;
                    }
                    else
                    {
                        if (response.Items[0].Item[i].ItemAttributes.ListPrice != null)
                        {
                            hind = response.Items[0].Item[i].ItemAttributes.ListPrice.FormattedPrice;
                        }
                        else
                        {
                            hind = "--";
                        }
                    }
                    itemList.Add(new AmazonItem
                    {
                        Nimi = response.Items[0].Item[i].ItemAttributes.Title,
                        Url = response.Items[0].Item[i].DetailPageURL,
                        Hind = hind
                    });
                }
            }

            //Tulemuse mudeli tekitamine eelnevalt täidetud listiga
            var tulemusModel = new TulemusViewModel();
            tulemusModel.PageCount = 1;
            tulemusModel.AmazonPageCount = 3;
            tulemusModel.AmazonItemList = itemList;
            tulemusModel.Keyword = model.Otsing;

            return View(tulemusModel);
        }

        [HttpPost]
        public ActionResult TulemusLoadPage(int pageCount, TulemusViewModel returnedModel)
        {
            //Amazon Product Advertising API poole pöördumise ettevalmistamine

            BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
            binding.MaxReceivedMessageSize = int.MaxValue;

            AWSECommerceServicePortTypeClient client = new AWSECommerceServicePortTypeClient(binding, new EndpointAddress("https://webservices.amazon.com/onca/soap?Service=AWSECommerceService"));

            ItemSearch search = new ItemSearch();
            search.AssociateTag = ""; //Amazoni associate tag ning access key vajalikud
            search.AWSAccessKeyId = "";

            client.ChannelFactory.Endpoint.Behaviors.Add(new AmazonSigningEndpointBehavior());

            ItemSearchRequest request = new ItemSearchRequest();

            request.ResponseGroup = new string[] { "ItemAttributes", "Offers" };

            request.SearchIndex = "All";
            request.Keywords = returnedModel.Keyword;

            while ((pageCount + 2) * 13 > returnedModel.AmazonPageCount * 10 && returnedModel.AmazonPageCount < 5)
            {
                request.ItemPage = (returnedModel.AmazonPageCount + 1).ToString();
                search.Request = new ItemSearchRequest[] { request };
                ItemSearchResponse response = client.ItemSearch(search);

                //Listi tulemustega täitmine
                for (int i = 0; i < 10; i++)
                {
                    string hind;
                    //Mõnel juhul (nt Kindle tooted) ei ole tootel hinda vastuses
                    if (response.Items[0].Item[i].OfferSummary != null && response.Items[0].Item[i].OfferSummary.LowestNewPrice != null)
                    {
                        hind = response.Items[0].Item[i].OfferSummary.LowestNewPrice.FormattedPrice;
                    } else
                    {
                        if (response.Items[0].Item[i].ItemAttributes.ListPrice != null)
                        {
                            hind = response.Items[0].Item[i].ItemAttributes.ListPrice.FormattedPrice;
                        } else
                        {
                            hind = "--";
                        }
                    }
                    returnedModel.AmazonItemList.Add(new AmazonItem
                    {
                        Nimi = response.Items[0].Item[i].ItemAttributes.Title,
                        Url = response.Items[0].Item[i].DetailPageURL,
                        Hind = hind
                    });
                }
                returnedModel.AmazonPageCount++;
            }

            return Json(new { pageCount = pageCount + 1, model = returnedModel });
        }
    }

    //Amazon request signing
    //Kood on pärit aadressilt https://flyingpies.wordpress.com/2009/08/13/signing-amazon-product-advertising-api-cwcf-part-2/
    public class AmazonSigningEndpointBehavior : IEndpointBehavior
    {
        private string accessKeyId = ""; //access key ning secret key vajalikud
        private string secretKey = "";

        public void Validate(ServiceEndpoint serviceEndpoint) { return; }

        public void AddBindingParameters(ServiceEndpoint serviceEndpoint, BindingParameterCollection bindingParameters) { return; }

        public void ApplyDispatchBehavior(ServiceEndpoint serviceEndpoint, EndpointDispatcher endpointDispatcher) { return; }

        public void ApplyClientBehavior(ServiceEndpoint serviceEndpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new AmazonSigningMessageInspector(accessKeyId, secretKey));
        }
    }

    public class AmazonSigningMessageInspector : IClientMessageInspector
    {
        private string accessKeyId = "";
        private string secretKey = "";

        public AmazonSigningMessageInspector(string accessKeyId, string secretKey)
        {
            this.accessKeyId = accessKeyId;
            this.secretKey = secretKey;
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            string operation = Regex.Match(request.Headers.Action, "[^/]+$").ToString();
            DateTime now = DateTime.UtcNow;
            string timestamp = now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string signMe = operation + timestamp;
            byte[] bytesToSign = Encoding.UTF8.GetBytes(signMe);

            byte[] secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            HMAC hmacSha256 = new HMACSHA256(secretKeyBytes);
            byte[] hashBytes = hmacSha256.ComputeHash(bytesToSign);
            string signature = Convert.ToBase64String(hashBytes);

            request.Headers.Add(new AmazonHeader("AWSAccessKeyId", accessKeyId));
            request.Headers.Add(new AmazonHeader("Timestamp", timestamp));
            request.Headers.Add(new AmazonHeader("Signature", signature));

            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState) { }
    }

    public class AmazonHeader : MessageHeader
    {
        private string name;
        private string value;

        public AmazonHeader(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        public override string Name { get { return name; } }
        public override string Namespace { get { return "http://security.amazonaws.com/doc/2007-01-01/"; } }

        protected override void OnWriteHeaderContents(XmlDictionaryWriter xmlDictionaryWriter, MessageVersion messageVersion)
        {
            xmlDictionaryWriter.WriteString(value);
        }
    }
}