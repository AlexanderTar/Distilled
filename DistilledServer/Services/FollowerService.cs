using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace DistilledServer
{
    public class FollowerService : Follower.FollowerBase
    {
        private readonly Storage _storage;

        public FollowerService(Storage storage)
        {
            _storage = storage;
        }

        public override Task<Empty> Replicate(ReplicationRequest request, ServerCallContext context)
        {
            if (request.Offset >= _storage.LatestOffset)
            {
                _storage.Add(request.Offset, request.Message.ToByteArray());
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Data> Read(ReadRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Data
            {
                Offset = request.Offset,
                Message = Google.Protobuf.ByteString.CopyFrom(_storage.Get(request.Offset))
            });
        }

        public override async Task Subscribe(SubscribeRequest request, IServerStreamWriter<Data> responseStream, ServerCallContext context)
        {
            var offset = request.Offset;

            while (!context.CancellationToken.IsCancellationRequested)
            {
                if (offset <= _storage.LatestOffset)
                {
                    var data = new Data
                    {
                        Offset = offset,
                        Message = Google.Protobuf.ByteString.CopyFrom(_storage.Get(offset))
                    };

                    await responseStream.WriteAsync(data);
                    offset++;
                }
            }
        }
    }
}
