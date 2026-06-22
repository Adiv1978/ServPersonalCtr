using ServPersonalCtr.Managers.L10;
using ServPersonalCtr.Types;

namespace ServPersonalCtr.Managers.L20
{
    public class mngPersonal
    {
        private readonly ServPersonalCtr.Managers.L10.mngPersonal _mngPersonalL10;

        public mngPersonal(ServPersonalCtr.Managers.L10.mngPersonal mngPersonalL10)
        {
            _mngPersonalL10 = mngPersonalL10;
        }

        /// <summary>
        /// Acceso intermedio para registrar o actualizar personal.
        /// </summary>
        public int SetPersonal(string token, DTOPersonal personal)
        {
            return _mngPersonalL10.SetPersonal(token, personal);
        }

        /// <summary>
        /// Acceso intermedio para consultar el listado de personal.
        /// </summary>
        public List<DTOPersonal> GetPersonal(string token, int id = 0, string busqueda = "")
        {
            return _mngPersonalL10.GetPersonal(token, id, busqueda);
        }
    }
}
