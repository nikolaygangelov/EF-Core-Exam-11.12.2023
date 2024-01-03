using Cadastre.Data.Enumerations;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Cadastre.DataProcessor.ImportDtos
{
    [XmlType("District")]
    public class ImportDistrictsDTO
    {
        [Required]
        [XmlAttribute("Region")]
        [RegularExpression(@"^(SouthEast|SouthWest|NorthEast|NorthWest)\b")]
        public string Region { get; set; }

        [Required]
        [MaxLength(80)]
        [MinLength(2)]
        [XmlElement("Name")]
        public string Name { get; set; }

        [Required]
        [MaxLength(8)]
        [RegularExpression(@"^([A-Z][A-Z]-\d{5})\b")]
        [XmlElement("PostalCode")]
        public string PostalCode { get; set; }

        public ImportDistrictsPropertiesDTO[] Properties { get; set; }

    }
}
