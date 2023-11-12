using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using Company.Function.ExtensionMethods;
using System.Net;
using System.Collections.Generic;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;

namespace Company.Function
{

    namespace ExtensionMethods
    {
        public static class MyJObject
        {
            public static string TryGetStringValue(this JObject _JObject, string key)
            {
                if (_JObject.TryGetValue(key, StringComparison.InvariantCulture, out JToken? jdata))
                {
                    return jdata.ToString();
                }
                else
                {
                    return "";
                }
            }

            public static int TryGetIntValue(this JObject _JObject, string key)
            {
                if (_JObject.TryGetValue(key, StringComparison.InvariantCulture, out JToken? jdata))
                {
                    return (int)jdata;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
    public class MyDocument
    {
        public string id { get; set; } = "";
        public string LicenseId { get; set; } = "";
        public string Userhash { get; set; } = "";
    }

    public class OrdersDocument
    {
        public string id { get; set; } = "";
        public string LicenseId { get; set; } = "";
        public string Code { get; set; } = "";
        public bool Confirmed { get; set; } = false;
    }

    public class KeysDocument
    {
        public string id { get; set; } = "";
        public string LicenseId { get; set; } = "";
        public string OrderId { get; set; } = "";
        public bool Used { get; set; } = false;
    }

    public class POST_Json
    {
        public string CMD { get; set; } = "";
        public string Key { get; set; } = "";
        public string Userhash { get; set; } = "";
    }

    public class CoinbaseOrder
    {
        public string EventId { get; set; } = "";
        public string Type { get; set; } = "";
        public string Code { get; set; } = "";
        public bool Confirmed { get; set; } = false;
        public string Payments { get; set; } = "";
        public bool Parsed { get; set; } = false;
    }

    public static class WalletCode
    {
        private static readonly CosmosClient Cosmosclient = new("AccountEndpoint=https://piri.documents.azure.com:443/;AccountKey=ELWerGct9t79GdCnndLG5ydekb9kMjEPWr8TjprLdq3a7tixxU5BIuZVqKlUlclATXsKtOQSX2TkACDb3z3Fyw==");

        [FunctionName("WalletAPI")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            string responseMessage = "Error";

            try
            {
                if (req.Method == "POST")
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                    if (requestBody.Contains("scheduled_for", StringComparison.CurrentCulture))
                    {
                        CoinbaseOrder cbo = new();
                        JObject? webhook = JsonConvert.DeserializeObject<JObject>(requestBody);
                        if (webhook != null)
                        {
                            if (webhook.TryGetValue("event", StringComparison.InvariantCulture, out JToken? jdata) && (jdata != null))
                            {
                                JObject? jodata = jdata.ToObject<JObject>();
                                if (jodata != null)
                                {
                                    cbo.EventId = jodata.TryGetStringValue("id");
                                    cbo.Type = jodata.TryGetStringValue("type");

                                    if (jodata.TryGetValue("data", StringComparison.InvariantCulture, out jdata) && (jdata != null))
                                    {
                                        jodata = jdata.ToObject<JObject>();
                                        if (jodata != null)
                                        {
                                            cbo.Code = jodata.TryGetStringValue("code");
                                            if (jodata.TryGetValue("timeline", StringComparison.InvariantCulture, out jdata) && (jdata != null))
                                            {
                                                foreach (JToken it in jdata)
                                                {
                                                    JObject? oit = it.ToObject<JObject>();
                                                    if (oit != null)
                                                    {
                                                        oit.TryGetStringValue("status");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (cbo.Parsed == true)
                        {
                            Container orders = Cosmosclient.GetContainer("PiriWallet", "Orders");
                            Container keys = Cosmosclient.GetContainer("PiriWallet", "keys");
                            try
                            {
                                ItemResponse<OrdersDocument> ResponseRecord = await orders.ReadItemAsync<OrdersDocument>(cbo.Code, new PartitionKey(cbo.Code));
                            }
                            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                            {
                                OrdersDocument i = new();
                                ItemResponse<OrdersDocument> ResponseRecord = await orders.CreateItemAsync<OrdersDocument>(i, new PartitionKey(cbo.Code));
                            }
                        }
                    }
                    else if (requestBody.Contains("cron", StringComparison.CurrentCulture))
                    {
                        var sqlQueryText = "SELECT * FROM c";

                        QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
                        Container orders = Cosmosclient.GetContainer("PiriWallet", "Orders");
                        FeedIterator<OrdersDocument> queryResultSetIterator = orders.GetItemQueryIterator<OrdersDocument>(queryDefinition);

                        List<OrdersDocument> orders = new List<OrdersDocument>();


                        while (queryResultSetIterator.HasMoreResults)
                        {
                            FeedResponse<OrdersDocument> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                            foreach (OrdersDocument order in currentResultSet)
                            {
                                orders.Add(order);
                            }
                        }

                        foreach (OrdersDocument order in orders)
                        {
                            wakefieldFamilyResponse = await container.ReplaceItemAsync<OrdersDocument>(itemBody, itemBody.Id, new PartitionKey(itemBody.PartitionKey));

                            wakefieldFamilyResponse = await container.ReplaceItemAsync<OrdersDocument>(itemBody, itemBody.Id, new PartitionKey(itemBody.PartitionKey));

                            MailjetClient client = new MailjetClient(
                                      Environment.GetEnvironmentVariable("MJ_APIKEY_PUBLIC"),
                                      Environment.GetEnvironmentVariable("MJ_APIKEY_PRIVATE"));

                            var email = new TransactionalEmailBuilder()
                                   .WithFrom(new SendContact("from@test.com"))
                                   .WithSubject("Test subject")
                                   .WithHtmlPart("<h1>Header</h1>")
                                   .WithTo(new SendContact("to@test.com"))
                                   .Build();

                            var response = await client.SendTransactionalEmailAsync(email);
                            if (response.Messages.Length == 1)
                            {

                            }

                        }
                    }
                    else
                    {
                        Container container = Cosmosclient.GetContainer("PiriWallet", "Users");
                        POST_Json? data = JsonConvert.DeserializeObject<POST_Json>(requestBody);
                        if (data != null)
                        {
                            if (data.CMD == "check")
                            {
                                ItemResponse<MyDocument> ResponseRecord = await container.ReadItemAsync<MyDocument>(data.Key, new PartitionKey(data.Key));
                                MyDocument itemBody = ResponseRecord;
                                responseMessage = JsonConvert.SerializeObject(itemBody);
                            }
                            else if (data.CMD == "update")
                            {
                                ItemResponse<MyDocument> ResponseRecord = await container.ReadItemAsync<MyDocument>(data.Key, new PartitionKey(data.Key));
                                MyDocument itemBody = ResponseRecord;

                                itemBody.Userhash = data.Userhash;
                                ResponseRecord = await container.ReplaceItemAsync<MyDocument>(itemBody, itemBody.LicenseId, new PartitionKey(data.Key));
                                itemBody = ResponseRecord;
                                responseMessage = JsonConvert.SerializeObject(itemBody);
                            }
                            else if (data.CMD == "version")
                            {
                                responseMessage = JsonConvert.SerializeObject(new { version = "1.0.3.0" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }

            return new OkObjectResult(responseMessage);
        }
    }
}
