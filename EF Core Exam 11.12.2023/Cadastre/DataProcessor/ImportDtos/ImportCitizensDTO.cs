using Cadastre.Data.Enumerations;
using Cadastre.Data.Models;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Cadastre.DataProcessor.ImportDtos
{
    public class ImportCitizensDTO
    {
        [Required]
        [MaxLength(30)]
        [MinLength(2)]
        [JsonProperty("FirstName")]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(30)]
        [MinLength(2)]
        [JsonProperty("LastName")]
        public string LastName { get; set; }

        [Required]
        [JsonProperty("BirthDate")]
        public string BirthDate { get; set; }

        [JsonProperty("MaritalStatus")]
        [RegularExpression(@"^(Unmarried|Married|Divorced|Widowed)\b")]
        public string MaritalStatus { get; set; }

        [JsonProperty("Properties")]
        public int[] Properties { get; set; }
    }
}
