using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dotnet_etcd;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static DistilledServer.Follower;
using static DistilledServer.Leader;

namespace DistilledServer
{
    public class StorageManager : BackgroundService
    {
        private readonly AppConfig _appConfig;
        private readonly ClusterInfo _cluster;
        private readonly Storage _storage;

        public StorageManager(AppConfig appConfig, ClusterInfo cluster, Storage storage)
        {
            _appConfig = appConfig;
            _cluster = cluster;
            _storage = storage;
        }

        protected async Task Init(CancellationToken stoppingToken)
        {
            var leader = _cluster.Leader;

            // block until leader is elected
            while (leader == null && !stoppingToken.IsCancellationRequested)
            {
                leader = _cluster.Leader;
                await Task.Delay(1000, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested) return;

            // if we are leader, there's nothing to do
            if (leader != _appConfig.HostName)
            {
                using var channel = GrpcChannel.ForAddress($"http://{leader}:5000");
                var leaderClient = new LeaderClient(channel);

                using var streamingCall = leaderClient.CatchUp(new CatchUpRequest
                {
                    Offset = _storage.LatestOffset
                }, cancellationToken: stoppingToken);

                while (await streamingCall.ResponseStream.MoveNext(stoppingToken))
                {
                    var catchUpData = streamingCall.ResponseStream.Current;
                    _storage.Add(catchUpData.Offset, catchUpData.Message.ToByteArray());
                }

                _storage.Drain();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Init(stoppingToken);
        }
    }
}
