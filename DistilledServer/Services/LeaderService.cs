using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace DistilledServer
{
    public class LeaderService : Leader.LeaderBase
    {
        private readonly AppConfig _appConfig;
        private readonly ClusterInfo _cluster;
        private readonly Storage _storage;

        public LeaderService(AppConfig appConfig, ClusterInfo cluster, Storage storage)
        {
            _appConfig = appConfig;
            _cluster = cluster;
            _storage = storage;
        }

        public override async Task CatchUp(CatchUpRequest request, IServerStreamWriter<CatchUpData> responseStream, ServerCallContext context)
        {
            var latestOffset = _storage.LatestOffset;

            var offset = request.Offset;

            while (!context.CancellationToken.IsCancellationRequested && offset < latestOffset)
            {
                var data = new CatchUpData
                {
                    Offset = offset,
                    Message = Google.Protobuf.ByteString.CopyFrom(_storage.Get(offset))
                };

                await responseStream.WriteAsync(data);
                offset++;
            }
        }

        public override async Task<WriteResponse> Write(WriteRequest request, ServerCallContext context)
        {
            long offset;

            lock (_storage)
            {
                offset = _storage.LatestOffset;

                _storage.Add(offset, request.Message.ToByteArray());
            }

            var members = _cluster.ClusterMembers.Except(new List<string> { _appConfig.HostName });

            foreach (var member in members)
            {
                using var channel = GrpcChannel.ForAddress($"http://{member}:5000");
                var client = new Follower.FollowerClient(channel);

                await client.ReplicateAsync(new ReplicationRequest
                {
                    Offset = offset,
                    Message = request.Message
                });
            }

            return new WriteResponse { Offset = offset };
        }
    }
}
