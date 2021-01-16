using System;
namespace DistilledAPI
{
    public class WriteDataRequest
    {
        public string Message { get; set; }
    }

    public class WriteDataResponse
    {
        public long Offset { get; set; }
    }
}
