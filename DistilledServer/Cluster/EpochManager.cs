using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DistilledServer
{
    public class EpochManager : BackgroundService
    {
        private readonly ILogger<EpochManager> _logger;
        private readonly AppConfig _appConfig;
        private readonly Coordinator _coordinator;
        private long? _leaseID;

        public EpochManager(ILogger<EpochManager> logger, AppConfig appConfig, Coordinator coordinator)
        {
            _logger = logger;
            _appConfig = appConfig;
            _coordinator = coordinator;
        }

        protected async Task<long> StartLease(CancellationToken stoppingToken)
        {
            var leaseResponse = await _coordinator.Client.LeaseGrantAsync(new Etcdserverpb.LeaseGrantRequest()
            {
                TTL = 1
            }, cancellationToken: stoppingToken);

            _logger.LogInformation("Starting new lease, ID: {leaseID}", leaseResponse.ID);

            _leaseID = leaseResponse.ID;

            return leaseResponse.TTL;
        }

        protected async Task RenewLease(long leaseID, Action<long, long> onRenewal, CancellationToken stoppingToken)
        {
            await _coordinator.Client.LeaseKeepAlive(new Etcdserverpb.LeaseKeepAliveRequest()
            {
                ID = leaseID
            }, (response) => onRenewal(response.ID, response.TTL), cancellationToken: stoppingToken);
        }

        protected string TryGetLeader()
        {
            var leader = _coordinator.Client.GetVal("host/leader");
            return leader == "" ? null : leader;
        }

        protected bool ProposeLeader(long leaseID, CancellationToken stoppingToken)
        {
            var txr = new Etcdserverpb.TxnRequest();
            txr.Compare.Add(new Etcdserverpb.Compare()
            {
                Result = Etcdserverpb.Compare.Types.CompareResult.Equal,
                Target = Etcdserverpb.Compare.Types.CompareTarget.Mod,
                Key = Google.Protobuf.ByteString.CopyFromUtf8("host/leader"),
                ModRevision = 0
            });
            txr.Success.Add(new Etcdserverpb.RequestOp()
            {
                RequestPut = new Etcdserverpb.PutRequest()
                {
                    Key = Google.Protobuf.ByteString.CopyFromUtf8("host/leader"),
                    Value = Google.Protobuf.ByteString.CopyFromUtf8(_appConfig.HostName),
                    Lease = leaseID
                }
            });
            var response = _coordinator.Client.Transaction(txr, cancellationToken: stoppingToken);
            _logger.LogInformation("Leader election: {response}", response);
            return response.Succeeded;
        }

        protected async Task JoinCluster(CancellationToken stoppingToken)
        {
            await _coordinator.Client.PutAsync(new Etcdserverpb.PutRequest()
            {
                Key = Google.Protobuf.ByteString.CopyFromUtf8($"host/members/{_appConfig.HostName}"),
                Value = Google.Protobuf.ByteString.CopyFromUtf8(_appConfig.HostName),
                Lease = _leaseID.Value
            }, cancellationToken: stoppingToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var leaseTTL = await StartLease(stoppingToken);

            await JoinCluster(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var leader = TryGetLeader();
                _logger.LogDebug("Leader: {leader}", leader);
                if (leader == null)
                {
                    ProposeLeader(_leaseID.Value, stoppingToken);
                }

                await RenewLease(_leaseID.Value, (ID, TTL) =>
                {
                    _logger.LogDebug("Lease renewed. ID: {ID}, TTL: {TTL}" , ID, TTL);
                }, stoppingToken);

                await Task.Delay((int)leaseTTL * 1000 / 2, stoppingToken);
            }
        }
    }
}
