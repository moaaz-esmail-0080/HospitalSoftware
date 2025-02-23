using BaseLibrary.Entites;
using Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSoftware.API.Controllers
{
 
[Route("api/[controller]")]
[ApiController]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentRepository _appointmentRepository;

    public AppointmentController(IAppointmentRepository appointmentRepository)
    {
            _appointmentRepository = appointmentRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments()
    {
        var appontments = await _appointmentRepository.GetAllAppointments();
        return Ok(appontments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Patient>> GetAppintment(int id)
    {
        var appointment = await _appointmentRepository.GetAppointmentById(id);
        if (appointment == null) return NotFound();
        return Ok(appointment);
    }

    [HttpPost]
    public async Task<ActionResult> AddAppontment(Appointment appointment)
    {
        await _appointmentRepository.AddAppointment(appointment);
        return CreatedAtAction(nameof(GetAppintment), new { id = appointment.Id }, appointment);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAppointment(int id, Appointment appointment)
    {
        if (id != appointment.Id) return BadRequest();
        await _appointmentRepository.UpdateAppointment(appointment);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        await _appointmentRepository.DeleteAppointment(id);
        return NoContent();
    }
}

}