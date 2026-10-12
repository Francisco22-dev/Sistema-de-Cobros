using Entidades;

namespace CapaNegocio
{
    /// <summary>
    /// Mantiene la sesión activa del usuario.
    /// </summary>
    public class SesionService
    {
        private static SesionService? _instancia;
        public Usuario? UsuarioActual { get; private set; }

        private SesionService() { }

        public static SesionService Instancia
        {
            get
            {
                if (_instancia == null)
                    _instancia = new SesionService();
                return _instancia;
            }
        }

        public void IniciarSesion(Usuario usuario)
        {
            UsuarioActual = usuario;
        }

        public void CerrarSesion()
        {
            UsuarioActual = null;
        }

        public bool EsAdministrador()
        {
            return UsuarioActual != null && (UsuarioActual.Rol == "Admin" || UsuarioActual.Rol == "Programador");
        }
    }
}
