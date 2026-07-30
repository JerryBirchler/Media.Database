using Cassandra;
using Media.Database.Models;
using System.Net;

namespace Media.Database.Helpers
{
    public class DockerPortTranslator(ScyllaSettings scyllaSettings) : IAddressTranslator
    {
        public IPEndPoint Translate(IPEndPoint address)
        {
            string contactPoint = scyllaSettings.ContactPoints[0];

            for (int i = 0; i < scyllaSettings.ExternalContactPoints.Count; i++) 
            {
                if (address.Address.ToString() == scyllaSettings.ExternalContactPoints[i]) 
                    return new IPEndPoint(
                        IPAddress.Parse(contactPoint),
                        scyllaSettings.Port + i);
            };

            return new IPEndPoint(IPAddress.Parse(contactPoint), address.Port);
        }
    }
}
