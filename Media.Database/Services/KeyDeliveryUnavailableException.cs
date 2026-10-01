namespace Media.Database.Services;

/// <summary>
/// The channel chosen for an encryption key has no destination on file -- SMS with no cell phone
/// number, or email with no address. An ordinary exception rather than an HTTP one, because the
/// Worker delivers keys too (WORKER-16); Media.Api turns it into a 400 at its own boundary.
/// </summary>
public class KeyDeliveryUnavailableException(string message) : InvalidOperationException(message);
