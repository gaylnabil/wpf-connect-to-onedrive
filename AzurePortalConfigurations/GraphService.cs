using Microsoft.Graph;
using System.Threading.Tasks;

namespace ConnectToOneDriveAzurePortal.AzurePortalConfigurations
{
    public sealed class GraphService
    {
        private GraphClientFactory _clientFactory;

        public GraphService(GraphClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public Task<GraphServiceClient> Client
        {
            get
            {
                if (_clientFactory == null)
                {
                    _clientFactory = new GraphClientFactory();
                }

                return _clientFactory.CreateAsync();
            }
        }
    }

}
