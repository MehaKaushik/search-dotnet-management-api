using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Threading;

using Microsoft.Azure.Management.CosmosDB;
using Microsoft.Azure.Management.CosmosDB.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;

using Microsoft.Rest.Azure.Authentication;

namespace ManagementAPI
{
    // The Azure CosmosDB Management SDK authentication uses a service principal in Azure Active Directory.  Before you can run the sample, you'll need to
    // create a service principal and give it the proper permissions in your subscription.  
    // This is discussed in detail here: https://docs.microsoft.com/azure/active-directory/develop/howto-create-service-principal-portal

    class Program
    {
        public static async Task Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            // Updated this information in the appsettings.json in this project.
            // See the instructions at the link above for the service principal details and the subscription id.
            var tenantId = configuration["TenantId"];
            var clientId = configuration["ClientId"];
            var clientSecret = configuration["ClientSecret"];
            var subscriptionId = configuration["SubscriptionId"];

            if (new[] { tenantId, clientId, clientSecret, subscriptionId }.Any(i => i.StartsWith("[")))
            {
                Console.WriteLine("Please provide values for tenantId, clientId, secret and subscriptionId.");
            }
            else
            {
                // Build the service credentials using the service principal.
                var creds = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, clientSecret);

                // Create the various management clients using the credentials instantiated above.
                var subscriptionClient = new SubscriptionClient(creds);
                var resourceClient = new ResourceManagementClient(creds);
                resourceClient.SubscriptionId = subscriptionId;

                var cosmosDBClient = new CosmosDBManagementClient(creds);
                cosmosDBClient.SubscriptionId = subscriptionId;

                // Get some general subscription information, not Azure Search specific
                var subscription = await subscriptionClient.Subscriptions.GetAsync(subscriptionId);
                DisplaySubscriptionDetails(subscription);

                // Register the Azure Search resource provider with the subscription. In the Azure Resource Manager model, you need
                // to register a resource provider in a subscription before you can use it. 
                // You only need to do this once per subscription/per resource provider.
                // More details on registering a resource provider here: https://docs.microsoft.com/rest/api/resources/Providers/Register
                var provider = resourceClient.Providers.Register("Microsoft.Search");
                DisplayProviderDetails(provider);

                // List all search services in the subscription by resource group.  
                // More details on listing resources here: https://docs.microsoft.com/rest/api/resources/resources/list
                var groups = await resourceClient.ResourceGroups.ListAsync();

                Console.WriteLine("----------------------------------------------------");
                Console.WriteLine("List all database accounts in the subscription by resource group");
                Console.WriteLine("----------------------------------------------------");

                foreach (var group in groups)
                {
                    var searchServices = cosmosDBClient.DatabaseAccounts.ListByResourceGroup(group.Name);
                    if (searchServices.Count() > 0)
                        Console.WriteLine("resourceGroup: {0}", group.Name);
                    {
                        foreach (var service in searchServices)
                        {
                            Console.WriteLine("   database name: {0}, type: {1}, location: {2}", service.Name, service.Kind, service.Location);
                        }
                    }
                }
                Console.WriteLine();
            }
        }

        private static void DisplaySubscriptionDetails(Subscription sub)
        {
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Subscription Details");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("id: {0}", sub.Id);
            Console.WriteLine("subscriptionId: {0}", sub.SubscriptionId);
            Console.WriteLine("displayName: {0}", sub.DisplayName);
            Console.WriteLine("state: {0}", sub.State);
            Console.WriteLine("subscriptionPolicies:");
            Console.WriteLine("   locationPlacementId: {0}", sub.SubscriptionPolicies.LocationPlacementId);
            Console.WriteLine("   quotaId: {0}", sub.SubscriptionPolicies.QuotaId);
            Console.WriteLine("   spendingLimit: {0}", sub.SubscriptionPolicies.SpendingLimit);
            Console.WriteLine();
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();
        }

        private static void DisplayProviderDetails(Provider provider)
        {
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Azure Search Provider Details");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine("id: {0}", provider.Id);
            Console.WriteLine("namespace: {0}", provider.NamespaceProperty);
            Console.WriteLine("registrationPolicy: {0}", provider.RegistrationPolicy);
            Console.WriteLine("resourceTypes:");
            foreach (var rt in provider.ResourceTypes)
            {
                Console.WriteLine("   resourceType: {0}", rt.ResourceType);
                Console.WriteLine("      locations:");
                foreach (var loc in rt.Locations) Console.WriteLine("         {0}", loc);
                Console.WriteLine("      apiVersions:");
                foreach (var api in rt.ApiVersions) Console.WriteLine("         {0}", api);
            }
            Console.WriteLine("registrationState: {0}", provider.RegistrationState);
            Console.WriteLine();
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();
        }
    }
}