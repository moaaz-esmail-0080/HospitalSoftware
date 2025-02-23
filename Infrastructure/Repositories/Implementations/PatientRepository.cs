using BaseLibrary.Entites;
using BaseLibrary.Responses;
using Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using AppContext = Infrastructure.Data.AppContext;

namespace Infrastructure.Repositories.Implementations
{
    public class PatientRepository : IGenericRepositoryInterface<Patient>
    {
        private readonly AppContext _appContext;

        public PatientRepository(AppContext appContext)
        {
            _appContext = appContext;
        }

        public async Task<List<Patient>> GetAll() =>
            await _appContext.Patients.Include(p => p.Appointments).ToListAsync();

        public async Task<Patient?> GetById(int id) =>
            await _appContext.Patients.Include(p => p.Appointments).FirstOrDefaultAsync(p => p.Id == id);

        public async Task<GeneralResponse> Insert(Patient item)
        {
            _appContext.Patients.Add(item);
            await Commit();
            return Success();
        }

        public async Task<GeneralResponse> Update(Patient item)
        {
            var patient = await _appContext.Patients.FindAsync(item.Id);
            if (patient is null) return NotFound();

            patient.Name = item.Name;
            patient.Age = item.Age;
            patient.Gender = item.Gender;
            patient.Address = item.Address;
            patient.PhoneNumber = item.PhoneNumber;

            await Commit();
            return Success();
        }

        public async Task<GeneralResponse> DeleteById(int id)
        {
            var patient = await _appContext.Patients.FindAsync(id);
            if (patient is null) return NotFound();

            _appContext.Patients.Remove(patient);
            await Commit();
            return Success();
        }

        private static GeneralResponse NotFound() => new(false, "Patient not found");
        private static GeneralResponse Success() => new(true, "Process Completed");
        private async Task Commit() => await _appContext.SaveChangesAsync();
    }
}
