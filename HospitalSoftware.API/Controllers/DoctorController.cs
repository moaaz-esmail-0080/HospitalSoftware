using BaseLibrary.Entites;
using Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSoftware.API.Controllers
{
[Route("api/[controller]")]
[ApiController]
public class DoctorController : ControllerBase
{
    private readonly IDoctorRepository _dotctorRepository;
        
    public DoctorController(IDoctorRepository doctorRepository)
    {
        _dotctorRepository = doctorRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Doctor>>> GetDoctors()
    {
        var doctors = await _dotctorRepository.GetAllDoctors();
        return Ok(doctors);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Patient>> GetDoctor(int id)
    {
        var doctor = await _dotctorRepository.GetDoctorById(id);
        if (doctor == null) return NotFound();
        return Ok(doctor);
    }

    [HttpPost]
    public async Task<ActionResult> AddDoctor(Doctor doctor)
    {
        await _dotctorRepository.AddDoctor(doctor);
        return CreatedAtAction(nameof(GetDoctor), new { id = doctor.Id }, doctor);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDoctor(int id, Doctor doctor)
    {
        if (id != doctor.Id) return BadRequest();
        await _dotctorRepository.UpdateDoctor(doctor);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDoctor(int id)
    {
        await _dotctorRepository.DeleteDoctor(id);
        return NoContent();
    }
}

}
