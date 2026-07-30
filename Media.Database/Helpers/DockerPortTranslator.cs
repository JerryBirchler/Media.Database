using Cassandra;
using Media.Database.Models;
using System.Net;

namespace Media.Database.Helpers
{
    public class DockerPortTranslator(ScyllaSettings scyllaSettings) : IAddressTranslator
    {
        public readonly string _contactPoint = scyllaSettings.ContactPoints[0];
        public readonly int _port = scyllaSettings.Port;
        public readonly List<string> _externalConactPoints = scyllaSettings.ExternalContactPoints;

        public IPEndPoint Translate(IPEndPoint address)
        {
            for (int i = 0; i <  _externalConactPoints.Count; i++) 
            {
                if (address.Address.ToString() == _externalConactPoints[i]) 
                    return new IPEndPoint(IPAddress.Parse(_contactPoint), _port + i);
            };

            return new IPEndPoint(IPAddress.Parse(_contactPoint), address.Port);
        }
    }
}
