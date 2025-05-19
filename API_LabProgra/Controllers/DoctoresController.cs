using API_LabProgra.Models;
using API_LabProgra.Servicios;
using API_LabProgra.DTOS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;


namespace API_LabProgra.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctoresController : ControllerBase
    {
        private readonly SistemaMedicoContext _context;
        private readonly ILogger<DoctoresController> _logger;
        private readonly ITokenService _tokenService;
        public DoctoresController(SistemaMedicoContext context, ILogger<DoctoresController> logger, ITokenService tokenService)
        {
            _context = context;
            _logger = logger;
            _tokenService = tokenService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DoctorDTO>>> GetDoctores()
        {
            try
            {
                var doctores = await _context.Doctores
                    .Include(d => d.IdAreaNavigation)
                    .Include(d => d.IdUsuarioNavigation)
                    .ToListAsync();

                return doctores.Select(d => DoctorDTO.FromEntity(d)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los doctores");
                return StatusCode(500, "Error interno del servidor");
            }
        }
        [HttpGet("Areas")]
        [Authorize] // Cualquier usuario autenticado
        public async Task<ActionResult<IEnumerable<AreasMedica>>> GetAllAreas()
        {
            try
            {
                var areas = await _context.AreasMedicas.ToListAsync();

                if (areas == null || !areas.Any())
                {
                    return NotFound("No se encontraron áreas médicas");
                }

                return areas.Select(a => new AreasMedica
                {
                    IdArea = a.IdArea,
                    Nombre = a.Nombre
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las áreas médicas");
                return StatusCode(500, "Error interno del servidor");
            }
        }



        [HttpGet("Usuario/{idUsuario}")]
        public async Task<ActionResult<DoctorDTO>> GetDoctorByUsuario(int idUsuario)
        {
            var doctor = await _context.Doctores
                .Include(d => d.IdAreaNavigation)
                .Include(d => d.IdUsuarioNavigation)
                .FirstOrDefaultAsync(d => d.IdUsuario == idUsuario);

            if (doctor == null)
            {
                return NotFound($"No se encontró doctor para el usuario con ID {idUsuario}");
            }

            return DoctorDTO.FromEntity(doctor);
        }

        [HttpGet("{id}")]
        [Authorize] // Cualquier usuario autenticado puede ver un doctor
        public async Task<ActionResult<DoctorDTO>> GetDoctor(int id)
        {
            try
            {
                var doctor = await _context.Doctores
                    .Include(d => d.IdAreaNavigation)
                    .Include(d => d.IdUsuarioNavigation)
                    .FirstOrDefaultAsync(d => d.IdDoctor == id);

                if (doctor == null)
                {
                    return NotFound($"Doctor con ID {id} no encontrado");
                }

                return DoctorDTO.FromEntity(doctor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener doctor con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }


    }

    [Route("api/Admin/Doctores")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminDoctoresController : ControllerBase
    {
        private readonly SistemaMedicoContext _context;
        private readonly ILogger<AdminDoctoresController> _logger;

        public AdminDoctoresController(SistemaMedicoContext context, ILogger<AdminDoctoresController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DoctorDetalleDTO>>> GetDoctoresDetalle()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Unauthorized(new { message = "Usuario no autenticado" });
                }

                var doctores = await _context.Doctores
                    .Include(d => d.IdAreaNavigation)
                    .Include(d => d.IdUsuarioNavigation)
                    .ToListAsync();

                return Ok(doctores.Select(d => DoctorDetalleDTO.FromEntity(d)).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetDoctoresDetalle");
                return StatusCode(500, new { message = ex.Message });
            }
        }
        

        [HttpGet("{id}")] //ID del Doctor no de usuario
        public async Task<ActionResult<DoctorDetalleDTO>> GetDoctorDetalle(int id)
        {
            try
            {
                var doctor = await _context.Doctores
                    .Include(d => d.IdAreaNavigation)
                    .Include(d => d.IdUsuarioNavigation)
                    .FirstOrDefaultAsync(d => d.IdDoctor == id);

                if (doctor == null)
                {
                    return NotFound($"Doctor con ID {id} no encontrado");
                }

                return DoctorDetalleDTO.FromEntity(doctor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del doctor con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost]
        public async Task<ActionResult<DoctorDetalleDTO>> CreateDoctor(DoctorCreateUpdateDTO doctorDTO)
        {
            try
            {
                var usuarioExists = await _context.Cuenta.AnyAsync(u => u.IdUsuario == doctorDTO.IdUsuario);
                if (!usuarioExists)
                {
                    return BadRequest($"El usuario con ID {doctorDTO.IdUsuario} no existe");
                }

                var areaExists = await _context.AreasMedicas.AnyAsync(a => a.IdArea == doctorDTO.IdArea);
                if (!areaExists)
                {
                    return BadRequest($"El área médica con ID {doctorDTO.IdArea} no existe");
                }

                var doctorExists = await _context.Doctores.AnyAsync(d => d.IdUsuario == doctorDTO.IdUsuario);
                if (doctorExists)
                {
                    return BadRequest($"El usuario con ID {doctorDTO.IdUsuario} ya está asignado a un doctor");
                }

                var usuario = await _context.Cuenta.FindAsync(doctorDTO.IdUsuario);
                if (usuario.Rol != 2)
                {
                    return BadRequest($"El usuario con ID {doctorDTO.IdUsuario} no tiene rol de doctor");
                }

                var doctor = doctorDTO.ToEntity();

                _context.Doctores.Add(doctor);
                await _context.SaveChangesAsync();

                var doctorCreado = await _context.Doctores
                    .Include(d => d.IdAreaNavigation)
                    .Include(d => d.IdUsuarioNavigation)
                    .FirstOrDefaultAsync(d => d.IdDoctor == doctor.IdDoctor);

                return CreatedAtAction("GetDoctor", "Doctores", new { id = doctor.IdDoctor }, DoctorDetalleDTO.FromEntity(doctorCreado));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear doctor");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, DoctorCreateUpdateDTO doctorDTO)
        {
            try
            {

                var doctorExists = await _context.Doctores.AnyAsync(d => d.IdDoctor == id);
                if (!doctorExists)
                {
                    return NotFound($"Doctor con ID {id} no encontrado");
                }

                var areaExists = await _context.AreasMedicas.AnyAsync(a => a.IdArea == doctorDTO.IdArea);
                if (!areaExists)
                {
                    return BadRequest($"El área médica con ID {doctorDTO.IdArea} no existe");
                }

                var doctorActual = await _context.Doctores.AsNoTracking().FirstOrDefaultAsync(d => d.IdDoctor == id);
                if (doctorActual.IdUsuario != doctorDTO.IdUsuario)
                {
                    return BadRequest("No se permite cambiar el usuario asignado al doctor");
                }

                var doctor = doctorDTO.ToEntity(id);

                _context.Entry(doctor).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DoctorExists(id))
                {
                    return NotFound($"Doctor con ID {id} no encontrado");
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar doctor con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            try
            {
                var doctor = await _context.Doctores.FindAsync(id);
                if (doctor == null)
                {
                    return NotFound($"Doctor con ID {id} no encontrado");
                }

                var tieneCitas = await _context.Citas.AnyAsync(c => c.IdDoctor == id);
                if (tieneCitas)
                {
                    return BadRequest("No se puede eliminar el doctor porque tiene citas asociadas");
                }

                var tieneHistoriales = await _context.HistorialMedicos.AnyAsync(h => h.IdDoctor == id);
                if (tieneHistoriales)
                {
                    return BadRequest("No se puede eliminar el doctor porque tiene historiales médicos asociados");
                }

                _context.Doctores.Remove(doctor);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar doctor con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctores.Any(e => e.IdDoctor == id);
        }
    }

}