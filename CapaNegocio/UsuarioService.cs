using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    /// <summary>
    /// Servicio de negocio para la gestión de usuarios.
    /// </summary>
    public class UsuarioService
    {
        private readonly UsuarioRepository _repository;
        private readonly AuditorService _auditoria;

        public UsuarioService()
        {
            _repository = new UsuarioRepository();
            _auditoria = new AuditorService();
        }

        public static string HashPassword(string password)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }

        public List<Usuario> ObtenerUsuarios(bool incluirProgramador = false)
        {
            var todos = _repository.ObtenerTodos();
            if (incluirProgramador)
                return todos;
            return todos.FindAll(u => u.Rol != "Programador");
        }

        public bool ValidarCredenciales(string username, string password, out Usuario? usuario)
        {
            usuario = null;
            var encontrados = _repository.ObtenerTodos();
            foreach (var u in encontrados)
            {
                if (u.Username == username && u.PasswordHash == HashPassword(password) && u.Activo)
                {
                    usuario = u;
                    try
                    {
                        _auditoria.Registrar("LOGIN", "Usuarios", username, null, null, true);
                    }
                    catch
                    {
                        // La auditoría no debe bloquear el login
                    }
                    return true;
                }
            }
            try
            {
                _auditoria.Registrar("LOGIN", "Usuarios", username, null, null, false);
            }
            catch
            {
                // La auditoría no debe bloquear el login
            }
            return false;
        }

        public void CrearUsuario(string username, string password, string nombreCompleto, string rol)
        {
            try
            {
                if (_repository.ObtenerTodos().Exists(u => u.Username == username))
                    throw new InvalidOperationException("El nombre de usuario ya existe.");

                _repository.Agregar(new Usuario(username, HashPassword(password), nombreCompleto, rol));
                _auditoria.Registrar("INSERT", "Usuarios", username, null,
                    $"Username:{username}, Nombre:{nombreCompleto}, Rol:{rol}", true);
            }
            catch (Exception)
            {
                _auditoria.Registrar("INSERT", "Usuarios", username, null,
                    $"Username:{username}, Nombre:{nombreCompleto}, Rol:{rol}", false);
                throw;
            }
        }

        public void ActualizarUsuario(string username, string nombreCompleto, string rol, string? nuevaPassword = null)
        {
            try
            {
                var usuario = _repository.ObtenerTodos().Find(u => u.Username == username);
                if (usuario == null) throw new InvalidOperationException("Usuario no encontrado.");
                if (usuario.Rol == "Programador") throw new InvalidOperationException("No se puede modificar el usuario programador.");

                string datosAnteriores = $"Nombre:{usuario.NombreCompleto}, Rol:{usuario.Rol}, Activo:{usuario.Activo}";
                usuario.NombreCompleto = nombreCompleto;
                usuario.Rol = rol;
                if (!string.IsNullOrEmpty(nuevaPassword)) usuario.PasswordHash = HashPassword(nuevaPassword);
                _repository.Actualizar(usuario);

                _auditoria.Registrar("UPDATE", "Usuarios", username, datosAnteriores,
                    $"Nombre:{nombreCompleto}, Rol:{rol}", true);
            }
            catch (Exception)
            {
                _auditoria.Registrar("UPDATE", "Usuarios", username, null,
                    $"Nombre:{nombreCompleto}, Rol:{rol}", false);
                throw;
            }
        }

        public void DesactivarUsuario(string username)
        {
            try
            {
                var usuario = _repository.ObtenerTodos().Find(u => u.Username == username);
                if (usuario == null) throw new InvalidOperationException("Usuario no encontrado.");
                if (usuario.Rol == "Programador" || usuario.Rol == "Admin")
                    throw new InvalidOperationException("No se puede desactivar este usuario.");

                usuario.Activo = false;
                _repository.Actualizar(usuario);
                _auditoria.Registrar("DESACTIVAR", "Usuarios", username, "Activo:True", "Activo:False", true);
            }
            catch (Exception)
            {
                _auditoria.Registrar("DESACTIVAR", "Usuarios", username, null, null, false);
                throw;
            }
        }

        public void ActivarUsuario(string username)
        {
            var usuario = _repository.ObtenerTodos().Find(u => u.Username == username);
            if (usuario == null)
                throw new InvalidOperationException("Usuario no encontrado.");

            usuario.Activo = true;
            _repository.Actualizar(usuario);

            _auditoria.Registrar("ACTIVAR", "Usuarios", username, "Activo:False", "Activo:True", true);
        }

        public void EliminarUsuario(string username)
        {
            try
            {
                var usuario = _repository.ObtenerTodos().Find(u => u.Username == username);
                if (usuario == null) throw new InvalidOperationException("Usuario no encontrado.");
                if (usuario.Rol == "Programador" || usuario.Rol == "Admin")
                    throw new InvalidOperationException("No se puede eliminar este usuario.");

                string datos = $"Username:{username}, Nombre:{usuario.NombreCompleto}, Rol:{usuario.Rol}";
                _repository.Eliminar(username);
                _auditoria.Registrar("DELETE", "Usuarios", username, datos, null, true);
            }
            catch (Exception)
            {
                _auditoria.Registrar("DELETE", "Usuarios", username, null, null, false);
                throw;
            }
        }
    }
}
