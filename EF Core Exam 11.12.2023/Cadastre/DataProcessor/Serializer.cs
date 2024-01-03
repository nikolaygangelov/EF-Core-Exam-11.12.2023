using Cadastre.Data;
using Cadastre.Data.Enumerations;
using Cadastre.DataProcessor.ExportDtos;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Cadastre.DataProcessor
{
    public class Serializer
    {
        public static string ExportPropertiesWithOwners(CadastreContext dbContext)
        {
            DateTime givenDate = DateTime.ParseExact("01/01/2000", "dd/MM/yyyy", CultureInfo.InvariantCulture);

            var propertiesAndOwners = dbContext.Properties
                 .Where(p => p.DateOfAcquisition >= givenDate)
                 .OrderByDescending(p => p.DateOfAcquisition)
                 .ThenBy(p => p.PropertyIdentifier)
                 .Select(p => new
                 {
                     PropertyIdentifier = p.PropertyIdentifier,
                     Area = (long)p.Area,
                     Address = p.Address,
                     DateOfAcquisition = p.DateOfAcquisition.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                     Owners = p.PropertiesCitizens
                     .OrderBy(pc => pc.Citizen.LastName)
                     .Select(pc => new
                     {
                         LastName = pc.Citizen.LastName,
                         MaritalStatus = pc.Citizen.MaritalStatus.ToString()
                     })
                     .ToArray()
                 })
                 .ToArray();


            return JsonConvert.SerializeObject(propertiesAndOwners, Newtonsoft.Json.Formatting.Indented);
        }

        public static string ExportFilteredPropertiesWithDistrict(CadastreContext dbContext)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportPropertiesDTO[]), new XmlRootAttribute("Properties"));

            StringBuilder sb = new StringBuilder();

            using var writer = new StringWriter(sb);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = ("\t"),
                //OmitXmlDeclaration = true
            };
            using var xmlwriter = XmlWriter.Create(writer, settings);

            var xns = new XmlSerializerNamespaces();
            xns.Add(string.Empty, string.Empty);

            var propertiesAndDistricts = dbContext.Properties
                .Where(p => p.Area >= 100)
                .OrderByDescending(p => p.Area)
                .ThenBy(p => p.DateOfAcquisition)
                .Select(p => new ExportPropertiesDTO
                {
                    PostalCode = p.District.PostalCode,
                    PropertyIdentifier = p.PropertyIdentifier,
                    Area = (long)p.Area,
                    DateOfAcquisition = p.DateOfAcquisition.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                })
                .ToArray();

            serializer.Serialize(xmlwriter, propertiesAndDistricts, xns);
            writer.Close();

            return sb.ToString();
        }
    }
}
