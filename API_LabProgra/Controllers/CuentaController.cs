using API_LabProgra.Models;
using API_LabProgra.Servicios;
using API_LabProgra.DTOS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace API_LabProgra.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CuentaController : ControllerBase
    {
        private readonly ICuentaService _cuentaService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CuentaController> _logger;

        public CuentaController(
            ICuentaService cuentaService,
            IConfiguration configuration,
            ILogger<CuentaController> logger)
        {
            _cuentaService = cuentaService;
            _configuration = configuration;
            _logger = logger;
        }

            [HttpGet]
            [Authorize(Policy = "AdminOnly")] 
            public async Task<ActionResult<IEnumerable<DTOCuenta>>> GetCuentas()
        {
            try
            {
                var cuentas = await _cuentaService.Alluser();
                var cuentasDTO = cuentas.Select(c => DTOCuenta.FromEntity(c)).ToList();
                return Ok(cuentasDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las cuentas");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("{id}")]
        [Authorize] 
        public async Task<ActionResult<DTOCuenta>> GetCuenta(int id)
        {
            try
            {
                var cuenta = await _cuentaService.GetUserById(id);

                if (cuenta == null)
                {
                    return NotFound($"Usuario con ID {id} no encontrado");
                }
                var userId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "0";

                if (userId != id && userRole != "1") 
                {
                    return Forbid();
                }

                return Ok(DTOCuenta.FromEntity(cuenta));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cuenta con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("rol/{rolId}")]
        public async Task<ActionResult<IEnumerable<DTOCuenta>>> GetCuentasByRol(int rolId)
        {
            try
            {
                var cuentas = await _cuentaService.GetUsersByRole(rolId);
                var cuentasDTO = cuentas.Select(c => DTOCuenta.FromEntity(c)).ToList();
                return Ok(cuentasDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cuentas con rol {RolId}", rolId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpGet("username/{username}")]
        public async Task<ActionResult<DTOCuenta>> GetCuentaByUsername(string username)
        {
            try
            {
                var cuenta = await _cuentaService.GetUserByUsername(username);

                if (cuenta == null)
                {
                    return NotFound($"Usuario '{username}' no encontrado");
                }

                return Ok(DTOCuenta.FromEntity(cuenta));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cuenta con username {Username}", username);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<DTOCuenta>> CreateCuenta(CreateDTOCuenta DTOCuenta)
        {
            try
            {

                if (string.IsNullOrEmpty(DTOCuenta.Usuario) || DTOCuenta.Usuario.Length < 4)
                {
                    return BadRequest("El nombre de usuario debe tener al menos 4 caracteres");
                }

                if (string.IsNullOrEmpty(DTOCuenta.Contraseña) || DTOCuenta.Contraseña.Length < 6)
                {
                    return BadRequest("La contraseña debe tener al menos 6 caracteres");
                }

                if (string.IsNullOrEmpty(DTOCuenta.Correo) || !new EmailAddressAttribute().IsValid(DTOCuenta.Correo))
                {
                    return BadRequest("El correo electrónico no es válido");
                }

                if (await _cuentaService.UsernameExists(DTOCuenta.Usuario))
                {
                    return BadRequest("El nombre de usuario ya está en uso");
                }

                // Verificar si el correo ya existe
                if (await _cuentaService.EmailExists(DTOCuenta.Correo))
                {
                    return BadRequest("El correo electrónico ya está registrado");
                }

                // Convertir DTO a entidad
                var cuenta = DTOCuenta.ToEntity();

                // Hashear la contraseña
                cuenta.Contraseña = HashPassword(cuenta.Contraseña);

                var resultado = await _cuentaService.AddUser(cuenta);

                if (resultado > 0)
                {
                    return StatusCode(201, new
                    {
                        message = "Cuenta creada exitosamente",
                        id = resultado
                    });
                }
                else
                {
                    return BadRequest("No se pudo crear la cuenta");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cuenta");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCuenta(int id, UpdateDTOCuenta DTOCuenta)
        {
            if (id != DTOCuenta.IdUsuario)
            {
                return BadRequest(new { success = false, message = "El ID en la ruta no coincide con el ID del usuario" });
            }

            try
            {
                var usuarioExistente = await _cuentaService.GetUserById(id);
                if (usuarioExistente == null)
                {
                    return NotFound(new { success = false, message = $"Usuario con ID {id} no encontrado" });
                }

                List<string> erroresValidacion = new List<string>();

                if (!string.IsNullOrEmpty(DTOCuenta.Correo) && !new EmailAddressAttribute().IsValid(DTOCuenta.Correo))
                {
                    erroresValidacion.Add("El formato del correo electrónico no es válido");
                }

                if (string.IsNullOrWhiteSpace(DTOCuenta.Nombre))
                {
                    erroresValidacion.Add("El nombre es obligatorio");
                }

                if (string.IsNullOrWhiteSpace(DTOCuenta.Apellidos))
                {
                    erroresValidacion.Add("Los apellidos son obligatorios");
                }

                if (DTOCuenta.Edad <= 0)
                {
                    erroresValidacion.Add("La edad debe ser mayor que cero");
                }

                if (erroresValidacion.Count > 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Error de validación",
                        errors = erroresValidacion
                    });
                }

                var cuenta = DTOCuenta.ToEntity();

                if (string.IsNullOrEmpty(cuenta.Contraseña))
                {
                    cuenta.Contraseña = usuarioExistente.Contraseña; 
                }
                else
                {
                    cuenta.Contraseña = HashPassword(cuenta.Contraseña); 
                }

                var resultado = await _cuentaService.UpdateUser(cuenta);

                if (resultado == 0)
                {
                    return BadRequest(new { success = false, message = "No se pudo actualizar el usuario" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Usuario actualizado correctamente",
                    data = new
                    {
                        idUsuario = cuenta.IdUsuario,
                        nombre = cuenta.Nombre,
                        apellidos = cuenta.Apellidos,
                        correo = cuenta.Correo,
                        rol = cuenta.Rol
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cuenta con ID {Id}", id);
                if (ex.InnerException != null)
                {
                    if (ex.InnerException.Message.Contains("UNIQUE constraint") ||
                        ex.InnerException.Message.Contains("duplicate key"))
                    {
                        string campoConflicto = "un campo único";

                        if (ex.InnerException.Message.ToLower().Contains("correo"))
                        {
                            campoConflicto = "correo electrónico";
                        }
                        else if (ex.InnerException.Message.ToLower().Contains("usuario"))
                        {
                            campoConflicto = "nombre de usuario";
                        }

                        return BadRequest(new
                        {
                            success = false,
                            message = $"Ya existe una cuenta con este {campoConflicto}"
                        });
                    }
                }

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor al actualizar el usuario"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCuenta(int id)
        {
            try
            {
                var usuarioExistente = await _cuentaService.GetUserById(id);
                if (usuarioExistente == null)
                {
                    return NotFound(new { success = false, message = $"Usuario con ID {id} no encontrado" });
                }

                var resultado = await _cuentaService.DeleteUser(id);

                if (resultado == 0)
                {
                    return BadRequest(new { success = false, message = "No se pudo eliminar el usuario" });
                }

                return Ok(new { success = true, message = $"Usuario con ID {id} eliminado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cuenta con ID {Id}", id);

                if (ex.InnerException != null && ex.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No se puede eliminar este usuario porque tiene registros relacionados (citas, historiales médicos, etc.)"
                    });
                }

                return StatusCode(500, new { success = false, message = "Error interno del servidor" });
            }
        }

        // Método para hashear contraseñas
        private string HashPassword(string password)
        {
            return Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(password)
                )
            );
        }
    }
}