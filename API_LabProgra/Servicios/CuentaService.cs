using API_LabProgra.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
namespace API_LabProgra.Servicios
{
    public class CuentaService : ICuentaService
    {
        private readonly SistemaMedicoContext _context;
        private readonly DbSet<Cuentum> _dbSet;
        private readonly ILogger<CuentaService> _logger;

        public CuentaService(SistemaMedicoContext context, ILogger<CuentaService> logger)
        {
            _context = context;
            _dbSet = context.Set<Cuentum>();
            _logger = logger;
        }

        public async Task<List<Cuentum>> Alluser()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<int> AddUser(Cuentum modelo)
        {
            try
            {
                _dbSet.Add(modelo);
                await _context.SaveChangesAsync();
                return modelo.IdUsuario;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> UpdateUser(Cuentum modelo)
        {
            try
            {
                var usuarioExistente = await _dbSet.FindAsync(modelo.IdUsuario);
                if (usuarioExistente == null)
                    return 0;
                _context.Entry(usuarioExistente).CurrentValues.SetValues(modelo);
                if (string.IsNullOrEmpty(modelo.Contraseña))
                {
                    _context.Entry(usuarioExistente).Property(x => x.Contraseña).IsModified = false;
                }
                await _context.SaveChangesAsync();
                return modelo.IdUsuario;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<int> DeleteUser(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var usuario = await _dbSet.FindAsync(id);
                if (usuario == null)
                    return 0;

                var citas = await _context.Citas.Where(c => c.IdUsuario == id).ToListAsync();
                if (citas.Any())
                {
                    _logger.LogInformation("Eliminando {Count} citas del usuario {UserId}", citas.Count, id);
                    _context.Citas.RemoveRange(citas);
                }

                var doctorRecord = await _context.Doctores.FirstOrDefaultAsync(d => d.IdUsuario == id);
                if (doctorRecord != null)
                {
                    var doctorCitas = await _context.Citas.Where(c => c.IdDoctor == doctorRecord.IdDoctor).ToListAsync();
                    if (doctorCitas.Any())
                    {
                        _logger.LogInformation("Eliminando {Count} citas donde el usuario {UserId} es doctor", doctorCitas.Count, id);
                        _context.Citas.RemoveRange(doctorCitas);
                    }

                    _logger.LogInformation("Eliminando registro de doctor para el usuario {UserId}", id);
                    _context.Doctores.Remove(doctorRecord);
                }

                var historiales = await _context.HistorialMedicos.Where(h => h.IdUsuario == id).ToListAsync();
                if (historiales.Any())
                {
                    _logger.LogInformation("Eliminando {Count} registros de historial médico del usuario {UserId}", historiales.Count, id);
                    _context.HistorialMedicos.RemoveRange(historiales);
                }

                _logger.LogInformation("Eliminando el usuario {UserId}", id);
                _dbSet.Remove(usuario);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return id; 
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al eliminar usuario {UserId}", id);

                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Cuentum> GetUserById(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<Cuentum> GetUserByUsername(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Usuario == username);
        }

        public async Task<Cuentum> ValidateUser(string username, string password)
        {
            var usuario = await _dbSet.FirstOrDefaultAsync(u => u.Usuario == username);
            if (usuario == null)
                return null;
            bool passwordIsValid = VerifyPassword(password, usuario.Contraseña);
            if (!passwordIsValid)
                return null;
            return usuario;
        }

        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            string hashedInput = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(inputPassword)
                )
            );
            return hashedInput == storedPassword;
        }

        public async Task<List<Cuentum>> GetUsersByRole(int rolId)
        {
            return await _dbSet.Where(u => u.Rol == rolId).ToListAsync();
        }

        public async Task<bool> UsernameExists(string username)
        {
            return await _dbSet.AnyAsync(u => u.Usuario == username);
        }

        public async Task<bool> EmailExists(string email)
        {
            return await _dbSet.AnyAsync(u => u.Correo == email);
        }
    }
}