using BaseLibrary.Entites;
using BaseLibrary.Responses;
using Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using AppContext = Infrastructure.Data.AppContext;

namespace Infrastructure.Repositories.Implementations
{
    public class DoctorRepository : IGenericRepositoryInterface<Doctor>
    {
        private readonly AppContext _appContext;

        public DoctorRepository(AppContext appContext)
        {
            _appContext = appContext;
        }

        public async Task<List<Doctor>> GetAll() =>
            await _appContext.Doctors.Include(d => d.Appointments).ToListAsync();

        public async Task<Doctor?> GetById(int id) =>
            await _appContext.Doctors.Include(d => d.Appointments).FirstOrDefaultAsync(d => d.Id == id);

        public async Task<GeneralResponse> Insert(Doctor item)
        {
            _appContext.Doctors.Add(item);
            await Commit();
            return Success();
        }

        public async Task<GeneralResponse> Update(Doctor item)
        {
            var doctor = await _appContext.Doctors.FindAsync(item.Id);
            if (doctor is null) return NotFound();

            doctor.Name = item.Name;
            doctor.Specialty = item.Specialty;
            doctor.PhoneNumber = item.PhoneNumber;
            doctor.Email = item.Email;
            doctor.ExperienceYears = item.ExperienceYears;

            await Commit();
            return Success();
        }

        public async Task<GeneralResponse> DeleteById(int id)
        {
            var doctor = await _appContext.Doctors.FindAsync(id);
            if (doctor is null) return NotFound();

            _appContext.Doctors.Remove(doctor);
            await Commit();
            return Success();
        }

        private static GeneralResponse NotFound() => new(false, "Doctor not found");
        private static GeneralResponse Success() => new(true, "Process Completed");
        private async Task Commit() => await _appContext.SaveChangesAsync();
    }
}
