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
        public DTOSession SetSeccion(string nick, string pass, int minutos)
        {
            // Aquí podrías agregar lógica adicional de L20 si fuera necesario
            // antes de pasar la estafeta a L10.
            return _mngSeguridadL10.SetSeccion(nick, pass, minutos);
        }

        /// <summary>
        /// Intermedio para validar el estado de la sesión.
        /// </summary>
        public DTOSession ValidateSeccion(string token, int minutos, short rolLevel)
        {
            return _mngSeguridadL10.ValidateSeccion(token, minutos, rolLevel);
        }
    }
}
