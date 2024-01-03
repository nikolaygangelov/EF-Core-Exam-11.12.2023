namespace Cadastre.DataProcessor
{
    using Cadastre.Data;
    using Cadastre.Data.Enumerations;
    using Cadastre.Data.Models;
    using Cadastre.DataProcessor.ImportDtos;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using System.Xml.Serialization;

    public class Deserializer
    {
        private const string ErrorMessage =
            "Invalid Data!";
        private const string SuccessfullyImportedDistrict =
            "Successfully imported district - {0} with {1} properties.";
        private const string SuccessfullyImportedCitizen =
            "Succefully imported citizen - {0} {1} with {2} properties.";

        public static string ImportDistricts(CadastreContext dbContext, string xmlDocument)
        {
            var serializer = new XmlSerializer(typeof(ImportDistrictsDTO[]), new XmlRootAttribute("Districts"));
            using StringReader inputReader = new StringReader(xmlDocument);
            var districtsArrayDTOs = (ImportDistrictsDTO[])serializer.Deserialize(inputReader);

            StringBuilder sb = new StringBuilder();
            List<District> districtsXML = new List<District>();

            foreach (ImportDistrictsDTO districtDTO in districtsArrayDTOs)
            {

                if (!IsValid(districtDTO))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                if (districtsXML.Any(d => d.Name == districtDTO.Name))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                District districtToAdd = new District
                {
                    Region = (Region) Enum.Parse(typeof(Region), districtDTO.Region),
                    Name = districtDTO.Name,
                    PostalCode = districtDTO.PostalCode
                };

                foreach (var property in districtDTO.Properties)
                {
                    if (!IsValid(property))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    if (districtToAdd.Properties.Any(p => p.Address == property.Address))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    // проверка за валидност на датите 
                    DateTime dateOfAcquisition;

                    if (!DateTime.TryParseExact(property.DateOfAcquisition, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfAcquisition))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    if (districtToAdd.Properties.Any(p => p.PropertyIdentifier == property.PropertyIdentifier))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    districtToAdd.Properties.Add(new Property()
                    {
                        PropertyIdentifier = property.PropertyIdentifier,
                        Area = property.Area,
                        Details = property.Details,
                        Address = property.Address,
                        DateOfAcquisition = dateOfAcquisition
                    });

                }


                districtsXML.Add(districtToAdd);
                sb.AppendLine(string.Format(SuccessfullyImportedDistrict, districtToAdd.Name, 
                    districtToAdd.Properties.Count));
            }

            dbContext.Districts.AddRange(districtsXML);

            dbContext.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        public static string ImportCitizens(CadastreContext dbContext, string jsonDocument)
        {
            var citizensArray = JsonConvert.DeserializeObject<ImportCitizensDTO[]>(jsonDocument);

            StringBuilder sb = new StringBuilder();
            List<Citizen> citizenList = new List<Citizen>();

            var existingPropertyIds = dbContext.Properties
               .Select(p => p.Id)
               .ToArray();

            foreach (ImportCitizensDTO citizenDTO in citizensArray)
            {

                if (!IsValid(citizenDTO))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                // проверка за валидност на датите 
                DateTime birthDate;

                if (!DateTime.TryParseExact(citizenDTO.BirthDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out birthDate))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                Citizen citizenToAdd = new Citizen()
                {
                    FirstName = citizenDTO.FirstName,
                    LastName = citizenDTO.LastName,
                    BirthDate = birthDate,
                    MaritalStatus = (MaritalStatus)Enum.Parse(typeof(MaritalStatus), citizenDTO.MaritalStatus)
                };

                foreach (int propertyId in citizenDTO.Properties.Distinct())
                {

                    if (!existingPropertyIds.Contains(propertyId))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    citizenToAdd.PropertiesCitizens.Add(new PropertyCitizen()
                    {
                        Citizen = citizenToAdd,
                        PropertyId = propertyId
                    });


                }

                citizenList.Add(citizenToAdd);
                sb.AppendLine(string.Format(SuccessfullyImportedCitizen, citizenToAdd.FirstName, citizenToAdd.LastName,
                    citizenToAdd.PropertiesCitizens.Count));
            }

            dbContext.AddRange(citizenList);
            dbContext.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        private static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
    }
}
