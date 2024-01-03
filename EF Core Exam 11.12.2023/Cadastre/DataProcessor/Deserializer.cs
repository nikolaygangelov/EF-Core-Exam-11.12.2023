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
            //using Data Transfer Object Class to map it with districts
            var serializer = new XmlSerializer(typeof(ImportDistrictsDTO[]), new XmlRootAttribute("Districts"));

            //Deserialize method needs TextReader object to convert/map
            using StringReader inputReader = new StringReader(xmlDocument);
            var districtsArrayDTOs = (ImportDistrictsDTO[])serializer.Deserialize(inputReader);

            //using StringBuilder to gather all info in one string
            StringBuilder sb = new StringBuilder();

            //creating List where all valid districts can be kept
            List<District> districtsXML = new List<District>();

            foreach (ImportDistrictsDTO districtDTO in districtsArrayDTOs)
            {
                //validating info for district from data
                if (!IsValid(districtDTO))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                //checking for duplicates
                if (districtsXML.Any(d => d.Name == districtDTO.Name))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                //creating a valid district
                District districtToAdd = new District
                {
                    //using identical properties in order to map successfully
                    Region = (Region) Enum.Parse(typeof(Region), districtDTO.Region), //parsing from string to reach needed format
                    Name = districtDTO.Name,
                    PostalCode = districtDTO.PostalCode
                };

                foreach (var property in districtDTO.Properties)
                {
                    //validating info for property from data
                    if (!IsValid(property))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    //checking for duplicates
                    if (districtToAdd.Properties.Any(p => p.Address == property.Address))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    //validating dates
                    DateTime dateOfAcquisition;

                    if (!DateTime.TryParseExact(property.DateOfAcquisition, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfAcquisition)) //using culture-independent format
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    //second check for duplicates
                    if (districtToAdd.Properties.Any(p => p.PropertyIdentifier == property.PropertyIdentifier))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    //adding a valid property
                    districtToAdd.Properties.Add(new Property()
                    {
                        //using identical properties in order to map successfully
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

            //actual importing info from data
            dbContext.SaveChanges();

            //using TrimEnd() to get rid of white spaces
            return sb.ToString().TrimEnd();
        }

        public static string ImportCitizens(CadastreContext dbContext, string jsonDocument)
        {
            //using Data Transfer Object Class to map it with citizens
            var citizensArray = JsonConvert.DeserializeObject<ImportCitizensDTO[]>(jsonDocument);

            //using StringBuilder to gather all info in one string
            StringBuilder sb = new StringBuilder();

            //creating List where all valid citizens can be kept
            List<Citizen> citizenList = new List<Citizen>();

            //taking only unique properties
            var existingPropertyIds = dbContext.Properties
               .Select(p => p.Id)
               .ToArray();

            foreach (ImportCitizensDTO citizenDTO in citizensArray)
            {
                //validating info for citizen from data
                if (!IsValid(citizenDTO))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                //validating dates
                DateTime birthDate;

                if (!DateTime.TryParseExact(citizenDTO.BirthDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out birthDate)) //using culture-independent format
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                ////creating a valid citizen
                Citizen citizenToAdd = new Citizen()
                {
                    //using identical properties in order to map successfully
                    FirstName = citizenDTO.FirstName,
                    LastName = citizenDTO.LastName,
                    BirthDate = birthDate,
                    MaritalStatus = (MaritalStatus)Enum.Parse(typeof(MaritalStatus), citizenDTO.MaritalStatus) //parsing from "string" to reach needed format
                };

                foreach (int propertyId in citizenDTO.Properties.Distinct())
                {
                    //validating only unique properties
                    if (!existingPropertyIds.Contains(propertyId))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    //adding a valid PropertyCitizen
                    citizenToAdd.PropertiesCitizens.Add(new PropertyCitizen()
                    {
                        //using identical properties in order to map successfully
                        Citizen = citizenToAdd,
                        PropertyId = propertyId
                    });


                }

                citizenList.Add(citizenToAdd);
                sb.AppendLine(string.Format(SuccessfullyImportedCitizen, citizenToAdd.FirstName, citizenToAdd.LastName,
                    citizenToAdd.PropertiesCitizens.Count));
            }

            dbContext.AddRange(citizenList);

            //actual importing info from data
            dbContext.SaveChanges();

            //using TrimEnd() to get rid of white spaces
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
