using System.ComponentModel.DataAnnotations;

namespace ServPersonalCtr.Types
{
    public class DTOGetLicenciaIniRequest : IValidatableObject
    {
        public DateTime FecIniA { get; set; }
        public DateTime FecIniB { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FecIniA == default)
            {
                yield return new ValidationResult(
                    "La fecha inicial es requerida.",
                    new[] { nameof(FecIniA) });
            }

            if (FecIniB == default)
            {
                yield return new ValidationResult(
                    "La fecha final es requerida.",
                    new[] { nameof(FecIniB) });
            }

            if (FecIniA != default && FecIniB != default && FecIniB.Date <= FecIniA.Date)
            {
                yield return new ValidationResult(
                    "La fecha final debe ser mayor que la fecha inicial.",
                    new[] { nameof(FecIniB) });
            }
        }
    }
}
