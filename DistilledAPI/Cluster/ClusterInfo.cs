using System;
using System.Collections.Generic;
using System.Linq;

namespace DistilledAPI
{
    public class ClusterInfo
    {
        private readonly Coordinator _coordinator;

        public ClusterInfo(Coordinator coordinator)
        {
            _coordinator = coordinator;
        }

        public string Leader
        {
            get
            {
                var leader = _coordinator.Client.GetVal("host/leader");
                return leader == "" ? null : leader;
            }
        }

        public IEnumerable<string> ClusterMembers
        {
            get
            {
                var response = _coordinator.Client.GetRange("host/members/");
                return response.Kvs.Select((kv) => kv.Value.ToStringUtf8());
            }
        }
    }
}
