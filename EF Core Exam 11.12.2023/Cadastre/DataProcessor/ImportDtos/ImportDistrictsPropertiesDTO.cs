using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Cadastre.DataProcessor.ImportDtos
{
    [XmlType("Property")]
    public class ImportDistrictsPropertiesDTO
    {
        [Required]
        [MaxLength(20)]
        [MinLength(16)]
        [XmlElement("PropertyIdentifier")]
        public string PropertyIdentifier { get; set; }

        [Required]
        [Range(0, 2000000000)]
        [XmlElement("Area")]
        public int Area { get; set; }

        [MaxLength(500)]
        [MinLength(5)]
        [XmlElement("Details")]
        public string? Details { get; set; }

        [Required]
        [MaxLength(200)]
        [MinLength(5)]
        [XmlElement("Address")]
        public string Address { get; set; }

        [Required]
        [XmlElement("DateOfAcquisition")]
        public string DateOfAcquisition { get; set; }
    }
}
