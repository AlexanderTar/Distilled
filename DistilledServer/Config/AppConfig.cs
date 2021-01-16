using System;
namespace DistilledServer
{
    public class AppConfig
    {
        public string HostName
        {
            get
            {
                return System.Net.Dns.GetHostName();
            }
        }

        public string CoordinatorHost
        {
            get
            {
                return Environment.GetEnvironmentVariable("COORDINATOR_HOST");
            }
        }

        public string CoordinatorPort
        {
            get
            {
                return Environment.GetEnvironmentVariable("COORDINATOR_PORT");
            }
        }
    }
}
