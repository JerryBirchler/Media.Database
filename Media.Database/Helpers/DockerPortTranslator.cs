using Media.Common.Models;
using System.Net;

namespace Media.Database.Helpers;

public class DockerPortTranslator
{
    private readonly ScyllaSettings _settings;

    public DockerPortTranslator(ScyllaSettings settings)
    {
        _settings = settings;
    }

    public IPEndPoint Translate(IPEndPoint endpoint)
    {
        // Find if the incoming address matches any external contact point
        var incomingAddress = endpoint.Address.ToString();

        for (int i = 0; i < _settings.ExternalContactPoints.Count; i++)
        {
            var externalContactPoint = _settings.ExternalContactPoints[i];
            var externalIp = externalContactPoint.Host;

            if (incomingAddress == externalIp)
            {
                // Match found - translate to corresponding contact point
                var contactPoint = _settings.ContactPoints[i];
                var translatedAddress = IPAddress.Parse(contactPoint.Host);
                var translatedPort = _settings.Port + i;

                return new IPEndPoint(translatedAddress, translatedPort);
            }
        }

        // No match found - return first contact point with original port
        var defaultContactPoint = _settings.ContactPoints[0];
        var defaultAddress = IPAddress.Parse(defaultContactPoint.Host);

        return new IPEndPoint(defaultAddress, endpoint.Port);
    }
}
