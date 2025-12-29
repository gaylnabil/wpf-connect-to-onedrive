using Microsoft.Graph;
using System.Threading.Tasks;

namespace ConnectToOneDriveAzurePortal.AzurePortalConfigurations
{
    public sealed class AzureGraphService
    {
        private AzureGraphClientFactory _clientFactory;

        public AzureGraphService(AzureGraphClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public Task<GraphServiceClient> Client
        {
            get
            {
                if (_clientFactory == null)
                {
                    _clientFactory = new AzureGraphClientFactory();
                }

                return _clientFactory.CreateAsync();
            }
        }
    }

}
