using ServPersonalCtr.Managers.L10;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L20
{
    public class mngSeguridad
    {
        // Inyectamos la lógica de negocio (L10)
        private readonly ServPersonalCtr.Managers.L10.mngSeguridad _mngSeguridadL10;

        public mngSeguridad(ServPersonalCtr.Managers.L10.mngSeguridad mngSeguridadL10)
        {
            _mngSeguridadL10 = mngSeguridadL10;
        }

        /// <summary>
        /// Intermedio para iniciar sesión.
        /// </summary>
        public DTOSession SetSeccion(string nick, string pass)
        {
            // Aquí podrías agregar lógica adicional de L20 si fuera necesario
            // antes de pasar la estafeta a L10.
            return _mngSeguridadL10.SetSeccion(nick, pass);
        }

        /// <summary>
        /// Intermedio para validar el estado de la sesión.
        /// </summary>
        public DTOSession ValidateSeccion(string token, short rolLevel)
        {
            return _mngSeguridadL10.ValidateSeccion(token, rolLevel);
        }

        /// <summary>
        /// Intermedio para actualizar la contraseña del usuario.
        /// </summary>
        public bool UpdatePasswordUser(string token, string nick, string passActual, string passNuevo)
        {
            if (string.IsNullOrWhiteSpace(passNuevo))
                throw new ArgumentException("La nueva contraseña no puede estar en blanco.");
            if (passActual == passNuevo)
                throw new ArgumentException("La nueva contraseña no puede ser igual a la contraseña actual.");
            return _mngSeguridadL10.UpdatePasswordUser(token, nick, passActual, passNuevo);
        }
    }
}
