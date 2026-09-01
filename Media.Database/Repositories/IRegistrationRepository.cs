using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IRegistrationRepository
{
    Task<SourceMachineRegistrations?> AddBySourceInformation(AddSourceInformationRequest request);
    Task<SourceMachineRegistrations?> UpdateSourceInformation(UpdateSourceInformationRequest request);
    Task<SourceMachineRegistrations?> GetByUuid(Guid uuid);
}
