using BaseLibrary.Entites;
using BaseLibrary.Responses;
using Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using AppContext = Infrastructure.Data.AppContext;

namespace Infrastructure.Repositories.Implementations
{
    public class AppointmentRepository : IGenericRepositoryInterface<Appointment>
    {
        private readonly AppContext _appContext;

        public AppointmentRepository(AppContext appContext)
        {
            _appContext = appContext;
        }

        public async Task<List<Appointment>> GetAll() =>
            await _appContext.Appointments.Include(a => a.Patient).Include(a => a.Doctor).ToListAsync();

        public async Task<Appointment?> GetById(int id) =>
            await _appContext.Appointments.Include(a => a.Patient).Include(a => a.Doctor).FirstOrDefaultAsync(a => a.Id == id);

        public async Task<GeneralResponse> Insert(Appointment item)
        {
            var patientExists = await _appContext.Patients.AnyAsync(p => p.Id == item.PatientId);
            var doctorExists = await _appContext.Doctors.AnyAsync(d => d.Id == item.DoctorId);
            if (!patientExists || !doctorExists) return new GeneralResponse(false, "Invalid Patient or Doctor ID");

            _appContext.Appointments.Add(item);
            await Commit();
            return Success();
        }

        public async Task<GeneralResponse> Update(Appointment item)
        {
            var appointment = await _appContext.Appointments.FindAsync(item.Id);
            if (appointment is null) return NotFound();

            appointment.PatientId = item.PatientId;
            appointment.DoctorId = item.DoctorId;
            appointment.AppointmentDate = item.AppointmentDate;
            appointment.Status = item.Status;

            await Commit();
            return Success();
        }

        public async Task<GeneralResponse> DeleteById(int id)
        {
            var appointment = await _appContext.Appointments.FindAsync(id);
            if (appointment is null) return NotFound();

            _appContext.Appointments.Remove(appointment);
            await Commit();
            return Success();
        }

        private static GeneralResponse NotFound() => new(false, "Appointment not found");
        private static GeneralResponse Success() => new(true, "Process Completed");
        private async Task Commit() => await _appContext.SaveChangesAsync();
    }
}
