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
            //date which has to compare to
            DateTime givenDate = DateTime.ParseExact("01/01/2000", "dd/MM/yyyy", CultureInfo.InvariantCulture); //using culture-independent format

            //turning needed info about properties into a collection using anonymous object
            //using less data
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

            //Serialize method needs object to convert/map
	        //adding Formatting for better reading 
            return JsonConvert.SerializeObject(propertiesAndOwners, Newtonsoft.Json.Formatting.Indented);
        }

        public static string ExportFilteredPropertiesWithDistrict(CadastreContext dbContext)
        {
            //using Data Transfer Object Class to map it with properties
            XmlSerializer serializer = new XmlSerializer(typeof(ExportPropertiesDTO[]), new XmlRootAttribute("Properties"));

            //using StringBuilder to gather all info in one string
            StringBuilder sb = new StringBuilder();

            //"using" automatically closes opened connections
            using var writer = new StringWriter(sb);

            //setting tabular identation
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = ("\t"),
                //OmitXmlDeclaration = true
            };
            using var xmlwriter = XmlWriter.Create(writer, settings);

            var xns = new XmlSerializerNamespaces();

            //one way to display empty namespace in resulted file
            xns.Add(string.Empty, string.Empty);

            var propertiesAndDistricts = dbContext.Properties
                .Where(p => p.Area >= 100)
                .OrderByDescending(p => p.Area)
                .ThenBy(p => p.DateOfAcquisition)
                .Select(p => new ExportPropertiesDTO
                {
                    //using identical properties in order to map successfully
                    PostalCode = p.District.PostalCode,
                    PropertyIdentifier = p.PropertyIdentifier,
                    Area = (long)p.Area,
                    DateOfAcquisition = p.DateOfAcquisition.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                })
                .ToArray();

            //Serialize method needs file, TextReader object and namespace to convert/map
            serializer.Serialize(xmlwriter, propertiesAndDistricts, xns);

            //explicitly closing connection in terms of reaching edge cases
            writer.Close();

            //using TrimEnd() to get rid of white spaces
            return sb.ToString().TrimEnd();
        }
    }
}
