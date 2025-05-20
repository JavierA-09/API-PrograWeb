using API_LabProgra.Models;
using API_LabProgra.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_LabProgra.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitasController : ControllerBase
    {
        private readonly SistemaMedicoContext _context;
        private readonly ILogger<CitasController> _logger;

        public CitasController(SistemaMedicoContext context, ILogger<CitasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CitaDTO>>> GetCitas()
        {
            try
            {
                var citas = await _context.Citas
                    .Include(c => c.IdDoctorNavigation)
                        .ThenInclude(d => d.IdUsuarioNavigation)
                    .Include(c => c.IdUsuarioNavigation)
                    .Include(c => c.EstadoCitaNavigation)
                    .ToListAsync();

                return citas.Select(c => CitaDTO.FromEntity(c)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las citas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CitaDTO>> GetCita(int id)
        {
            try
            {
                var cita = await _context.Citas
                    .Include(c => c.IdDoctorNavigation)
                        .ThenInclude(d => d.IdUsuarioNavigation)
                    .Include(c => c.IdUsuarioNavigation)
                    .Include(c => c.EstadoCitaNavigation)
                    .FirstOrDefaultAsync(c => c.IdCita == id);

                if (cita == null)
                {
                    return NotFound($"Cita con ID {id} no encontrada");
                }

                return CitaDTO.FromEntity(cita);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cita con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }


        [HttpGet("Doctor/{doctorId}")]
        public async Task<ActionResult<IEnumerable<CitaDTO>>> GetCitasByDoctor(int doctorId)
        {
            try
            {
                var doctorExists = await _context.Doctores.AnyAsync(d => d.IdDoctor == doctorId);
                if (!doctorExists)
                {
                    return NotFound($"Doctor con ID {doctorId} no encontrado");
                }

                var citas = await _context.Citas
                    .Include(c => c.IdDoctorNavigation)
                        .ThenInclude(d => d.IdUsuarioNavigation)
                    .Include(c => c.IdUsuarioNavigation)
                    .Include(c => c.EstadoCitaNavigation)
                    .Where(c => c.IdDoctor == doctorId)
                    .ToListAsync();

                return citas.Select(c => CitaDTO.FromEntity(c)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener citas del doctor {DoctorId}", doctorId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("Paciente/{pacienteId}")]
        public async Task<ActionResult<IEnumerable<CitaDTO>>> GetCitasByPaciente(int pacienteId)
        {
            try
            {

                var pacienteExists = await _context.Cuenta.AnyAsync(u => u.IdUsuario == pacienteId);
                if (!pacienteExists)
                {
                    return NotFound($"Paciente con ID {pacienteId} no encontrado");
                }

                var citas = await _context.Citas
                    .Include(c => c.IdDoctorNavigation)
                        .ThenInclude(d => d.IdUsuarioNavigation)
                    .Include(c => c.IdUsuarioNavigation)
                    .Include(c => c.EstadoCitaNavigation)
                    .Where(c => c.IdUsuario == pacienteId)
                    .ToListAsync();

                return citas.Select(c => CitaDTO.FromEntity(c)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener citas del paciente {PacienteId}", pacienteId);
                return StatusCode(500, "Error interno del servidor");
            }
        }


        [HttpPost]
        public async Task<ActionResult<CitaDTO>> CreateCita(CitaCreateDTO citaDTO)
        {
            try
            {

                var doctorExists = await _context.Doctores.AnyAsync(d => d.IdDoctor == citaDTO.IdDoctor);
                if (!doctorExists)
                {
                    return BadRequest($"El doctor con ID {citaDTO.IdDoctor} no existe");
                }

                var pacienteExists = await _context.Cuenta.AnyAsync(u => u.IdUsuario == citaDTO.IdUsuario);
                if (!pacienteExists)
                {
                    return BadRequest($"El paciente con ID {citaDTO.IdUsuario} no existe");
                }

                var estadoExists = await _context.EstadoCita.AnyAsync(e => e.IdEstado == citaDTO.EstadoCita);
                if (!estadoExists)
                {
                    return BadRequest($"El estado de cita con ID {citaDTO.EstadoCita} no existe");
                }

                if (citaDTO.FechaHora < DateTime.Now)
                {
                    return BadRequest("No se puede agendar una cita en el pasado");
                }

                var citaExistente = await _context.Citas
                    .AnyAsync(c => c.IdDoctor == citaDTO.IdDoctor &&
                                   c.FechaHora == citaDTO.FechaHora &&
                                   c.EstadoCita != 3); 

                if (citaExistente)
                {
                    return BadRequest("El doctor ya tiene una cita agendada para ese horario");
                }

                var cita = citaDTO.ToEntity();

                _context.Citas.Add(cita);
                await _context.SaveChangesAsync();

                var citaCreada = await _context.Citas
                    .Include(c => c.IdDoctorNavigation)
                        .ThenInclude(d => d.IdUsuarioNavigation)
                    .Include(c => c.IdUsuarioNavigation)
                    .Include(c => c.EstadoCitaNavigation)
                    .FirstOrDefaultAsync(c => c.IdCita == cita.IdCita);

                return CreatedAtAction(nameof(GetCita), new { id = cita.IdCita }, CitaDTO.FromEntity(citaCreada));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cita");
                return StatusCode(500, "Error interno del servidor");
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCita(int id, CitaUpdateDTO citaDTO)
        {
            try
            {
                if (id != citaDTO.IdCita)
                {
                    return BadRequest("El ID de la cita no coincide con el proporcionado en la URL");
                }
                var cita = await _context.Citas.FindAsync(id);
                if (cita == null)
                {
                    return NotFound($"Cita con ID {id} no encontrada");
                }

                var doctorExists = await _context.Doctores.AnyAsync(d => d.IdDoctor == citaDTO.IdDoctor);
                if (!doctorExists)
                {
                    return BadRequest($"El doctor con ID {citaDTO.IdDoctor} no existe");
                }

                var pacienteExists = await _context.Cuenta.AnyAsync(u => u.IdUsuario == citaDTO.IdUsuario);
                if (!pacienteExists)
                {
                    return BadRequest($"El paciente con ID {citaDTO.IdUsuario} no existe");
                }

                var estadoExists = await _context.EstadoCita.AnyAsync(e => e.IdEstado == citaDTO.EstadoCita);
                if (!estadoExists)
                {
                    return BadRequest($"El estado de cita con ID {citaDTO.EstadoCita} no existe");
                }

                cita.IdDoctor = citaDTO.IdDoctor;
                cita.IdUsuario = citaDTO.IdUsuario;
                cita.FechaHora = citaDTO.FechaHora;
                cita.EstadoCita = citaDTO.EstadoCita;
                if (citaDTO.Notas != null)
                {
                    cita.Notas = citaDTO.Notas;
                }

                if (citaDTO.EstadoCita == 3)
                {
                    if (!cita.FechaCalificacion.HasValue)
                    {
                        cita.FechaCalificacion = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                var citaActualizada = await _context.Citas
                    .Include(c => c.IdDoctorNavigation)
                        .ThenInclude(d => d.IdUsuarioNavigation)
                    .Include(c => c.IdUsuarioNavigation)
                    .Include(c => c.EstadoCitaNavigation)
                    .FirstOrDefaultAsync(c => c.IdCita == id);

                return Ok(CitaDTO.FromEntity(citaActualizada));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cita con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPatch("{id}/Estado/{estadoId}")]
        public async Task<IActionResult> UpdateEstadoCita(int id, int estadoId)
        {
            try
            {
                var cita = await _context.Citas.FindAsync(id);
                if (cita == null)
                {
                    return NotFound($"Cita con ID {id} no encontrada");
                }

                var estadoExists = await _context.EstadoCita.AnyAsync(e => e.IdEstado == estadoId);
                if (!estadoExists)
                {
                    return BadRequest($"El estado de cita con ID {estadoId} no existe");
                }

                cita.EstadoCita = estadoId;

                if (estadoId == 4) 
                {
                    cita.FechaCalificacion = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado de cita {Id} a {EstadoId}", id, estadoId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPatch("{id}/Calificar")]
        public async Task<IActionResult> CalificarCita(int id, [FromBody] CalificacionCitaDTO calificacion)
        {
            try
            {
                var cita = await _context.Citas.FindAsync(id);
                if (cita == null)
                {
                    return NotFound($"Cita con ID {id} no encontrada");
                }

                if (cita.EstadoCita != 4) 
                {
                    return BadRequest("Solo se pueden calificar citas completadas");
                }

                if (calificacion.Calificacion < 1 || calificacion.Calificacion > 5)
                {
                    return BadRequest("La calificación debe estar entre 1 y 5");
                }

                cita.Calificacion = calificacion.Calificacion;
                cita.Comentarios = calificacion.Comentarios;
                cita.FechaCalificacion = DateTime.Now;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calificar cita con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class EstadoCitaController : ControllerBase
    {
        private readonly SistemaMedicoContext _context;
        private readonly ILogger<EstadoCitaController> _logger;

        public EstadoCitaController(SistemaMedicoContext context, ILogger<EstadoCitaController> logger)
        {
            _context = context;
            _logger = logger;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<EstadoCitaDTO>>> GetEstadosCita()
        {
            try
            {
                var estados = await _context.EstadoCita.ToListAsync();
                return estados.Select(e => EstadoCitaDTO.FromEntity(e)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los estados de cita");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EstadoCitaDTO>> GetEstadoCita(int id)
        {
            try
            {
                var estado = await _context.EstadoCita.FindAsync(id);

                if (estado == null)
                {
                    return NotFound($"Estado de cita con ID {id} no encontrado");
                }

                return EstadoCitaDTO.FromEntity(estado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado de cita con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("Citas/{estadoId}")]
        public async Task<ActionResult<IEnumerable<CitaDTO>>> GetCitasByEstado(int estadoId)
        {
            try
            {

                var estadoExists = await _context.EstadoCita.AnyAsync(e => e.IdEstado == estadoId);
                if (!estadoExists)
                {
                    return NotFound($"Estado de cita con ID {estadoId} no encontrado");
                }

                var citas = await _context.Citas
                    .Include(c => c.IdDoctorNavigation)
                        .ThenInclude(d => d.IdUsuarioNavigation)
                    .Include(c => c.IdUsuarioNavigation)
                    .Where(c => c.EstadoCita == estadoId)
                    .ToListAsync();

                return citas.Select(c => CitaDTO.FromEntity(c)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener citas con estado {EstadoId}", estadoId);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}