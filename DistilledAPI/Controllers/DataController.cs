using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DistilledAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataController : ControllerBase
    {
        private readonly ClusterInfo _cluster;

        public DataController(ClusterInfo cluster)
        {
            _cluster = cluster;
        }

        [HttpPost]
        public WriteDataResponse Write(WriteDataRequest request)
        {
            using var channel = GrpcChannel.ForAddress($"http://{_cluster.Leader}:5000");
            var client = new Leader.LeaderClient(channel);

            var response = client.Write(new WriteRequest
            {
                Message = Google.Protobuf.ByteString.CopyFromUtf8(request.Message)
            });

            return new WriteDataResponse { Offset = response.Offset };
        }

        [HttpGet]
        public MessageData Read(long offset)
        {
            var readers = _cluster.ClusterMembers;
            var index = new Random().Next(0, readers.Count());
            var reader = readers.ElementAt(index);

            using var channel = GrpcChannel.ForAddress($"http://{reader}:5000");
            var client = new Follower.FollowerClient(channel);

            var response = client.Read(new ReadRequest
            {
                Offset = offset
            });

            return new MessageData
            {
                Message = response.Message.ToStringUtf8(),
                Offset = response.Offset
            };
        }
    }
}
