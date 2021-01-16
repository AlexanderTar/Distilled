using System;
using dotnet_etcd;

namespace DistilledAPI
{
    public class Coordinator
    {
        private readonly EtcdClient _etcdClient;

        public EtcdClient Client
        {
            get
            {
                return _etcdClient;
            }
        }

        public Coordinator(AppConfig appConfig)
        {
            _etcdClient = new EtcdClient($"https://{appConfig.CoordinatorHost}:{appConfig.CoordinatorPort}");
        }
    }
}
