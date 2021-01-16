using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using static DistilledServer.Leader;

namespace DistilledServer
{
    public class Storage
    {
        private readonly ILogger<Storage> _logger;

        private ConcurrentDictionary<long, byte[]> _messages = new ConcurrentDictionary<long, byte[]>();
        private ConcurrentQueue<(long, byte[])> _waitingQueue = new ConcurrentQueue<(long, byte[])>();

        public long LatestOffset {
            get
            {
                return _messages.Count;
            }
        }
        

        public Storage(ILogger<Storage> logger)
        {
            _logger = logger;
        }

        public void Add(long offset, byte[] message)
        {
            if (offset - LatestOffset > 0)
            {
                _waitingQueue.Enqueue((offset, message));
            }
            else
            {
                _messages[offset] = message;
            }
        }

        public byte[] Get(long offset)
        {
            return _messages[offset];
        }

        public void Drain()
        {
            while (_waitingQueue.TryDequeue(out (long, byte[]) top))
            {
                var (offset, message) = top;
                Add(offset, message);
            }
        }
    }
}
