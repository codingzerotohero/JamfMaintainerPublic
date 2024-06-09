using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using JamfMaintainer.Entities;
using Newtonsoft.Json;
using NLog;
using Newtonsoft.Json.Linq;

namespace JamfMaintainer
{
    public class Processor
    {
        private readonly Microsoft.Extensions.Logging.ILogger _consoleLogger;
        private readonly NLog.Logger _logger;
        private readonly APIConfig _api;
        private readonly SettingsManager _settingsManager = new SettingsManager();
        private readonly ArchiveContext _archiveContext;

        public Processor(APIConfig api, Logger logger, Microsoft.Extensions.Logging.ILogger consoleLogger, ArchiveContext archiveContext) 
        {
            _consoleLogger = consoleLogger;
            _logger = logger;
            _api = api;
            _archiveContext = archiveContext;
        }
        
        public async Task CheckForChangesAndUpdateUsers()
        {
            var checkedUsers = new List<string>();

            _logger.Info("Checking every 1 seconds if users in Master stores have changed");
            bool receivedSkole = false;
            bool receivedAdm = false;

            while (true)
            {
                receivedSkole = await CheckMessageQueue(_settingsManager.LCSConnectionString, "Skole");

                receivedAdm = await CheckMessageQueue(_settingsManager.ADMLCSConnectionString, "Admin");

                if (!receivedSkole && !receivedAdm)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public async Task<bool> CheckMessageQueue(string connectionString, string source)
        {
            bool received = false;

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    if (connection.State == System.Data.ConnectionState.Closed)
                    {
                        connection.Open();
                    }

                    string receiveQuery = "RECEIVE TOP(1) message_type_name, CAST(message_body AS NVARCHAR(MAX)) as Message FROM UpdateQueue";

                    var command = new SqlCommand(receiveQuery, connection);

                    var dataReader = command.ExecuteReader();

                    if (dataReader.HasRows)
                    {
                        while (dataReader.Read())
                        {

                            string messageType = dataReader.GetString(0);
                            string messageBody = dataReader.GetString(1);
                            await ProcessMessage(messageType, messageBody, source);
                        }
                        received = true;
                        dataReader.Close();
                    }
                    else
                    {
                        received = false;
                    }

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                received = false;
                _logger.Error(ex);
                _logger.Info("Could not check for messages. Possible network error. Sleeping for 60 seconds and trying again.");
            }
            return received;
        }

        public async Task ProcessMessage(string messageType, string messageBody, string source)
        {
            _logger.Info($"Got a message from {source} SQL! {messageType} - {messageBody}");
            if (messageType.ToLower() == "updatemessagetype" && !string.IsNullOrWhiteSpace(messageBody))
            {
                try
                {
                    var usersToUpdate = JsonConvert.DeserializeObject<List<UpdatedUser>>(messageBody);
                    
                    if (usersToUpdate != null && usersToUpdate.Count > 0)
                    {
                        foreach (var updatedUser in usersToUpdate)
                        {
                            _logger.Info("Here is our object values:");
                            _logger.Info($"ADObjectID:{updatedUser.ADObjectID}");
                            _logger.Info($"SamAccountName: {updatedUser.samaccountname}");

                            if (source == "Skole")
                            {
                                using (var context = new Context())
                                {
                                    var user = context.LCSUsers.FirstOrDefault(x => x.ADObjectID.Value.ToString() == updatedUser.ADObjectID);

                                    if (user != null)
                                    {
                                        if ((_settingsManager.ExcludeSchools.Any(x => x == user.school_code) == false))
                                        {
                                            _logger.Info($"Successfully found user {updatedUser.ADObjectID} in Master store. Will process user.");
                                            await ProcessUsers(user, context);
                                        }
                                    }
                                    else
                                    {
                                        _logger.Info($"Could not find ADObjectID {updatedUser.ADObjectID} in School Master store. Checking if its a teacher.");
                                    }
                                }
                            }

                            if (source == "Admin")
                            {
                                using (var context = new ADMLCSContext())
                                {
                                    var user = context.ADMLCSUsers.FirstOrDefault(x => x.ADObjectID.Value.ToString() == updatedUser.ADObjectID);

                                    if (user != null)
                                    {
                                        if (user.JobTitle.ToLower().Contains("adjunkt") || user.JobTitle.ToLower().Contains("lærer") || user.JobTitle.ToLower().Contains("rektor"))
                                        {
                                            if ((_settingsManager.ExcludeSchools.Any(x => x == user.School_Code_Primary) == false))
                                            {
                                                _logger.Info($"Successfully found user {updatedUser.ADObjectID} in Master store. Will process user.");
                                                await ProcessUsers(null, null, user, context);
                                            }
                                        }
                                        else if (_settingsManager.CustomUsernames.Any(x => x == user.ad_samaccountname))
                                        {
                                            await ProcessUsers(null, null, user, context);
                                        }
                                        else
                                        {
                                            _logger.Info("User is not a teacher, rektor, or custom user. Will not process.");
                                        }
                                    }
                                    else
                                    {
                                        _logger.Info($"Could not find ADObjectID {updatedUser.ADObjectID} in ADM Master store");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    _logger.Info($"Message type: {messageType} - Messagebody: {messageBody}");
                }
            }
        }

        public async Task FullRunAll()
        {
            await MaintainJamfGroups();

            _consoleLogger.LogInformation("Getting students and teachers from Master stores for processing");
            _logger.Info("Getting users from Master stores for processing");

            _logger.Info("Processing all students");
            int counter = 0;
            using (var context = new Context())
            {
                foreach (var user in GetMasterStoreUsers(context))
                {
                    counter++;
                    await ProcessUsers(user, context);
                }
            }

            _consoleLogger.LogInformation($"Processed {counter} students");

            counter = 0;

            _logger.Info("Processing all teachers");

            using (var admContext = new ADMLCSContext())
            {
                foreach (var user in GetADMMasterStoreUsers(admContext))
                {
                    counter++;
                    await ProcessUsers(null, null, user, admContext);
                }
            }

            _consoleLogger.LogInformation($"Processed {counter} teachers");

            _consoleLogger.LogInformation("Processing custom..");
            
        }

        public async Task FullRunStudents()
        {
            _consoleLogger.LogInformation("Getting students from Master store for processing");
            _logger.Info("Getting students from Master store for processing");

            _logger.Info("Processing all students");
            int counter = 0;
            using (var context = new Context())
            {
                foreach (var user in GetMasterStoreUsers(context))
                {
                    counter++;
                    await ProcessUsers(user, context);
                }
            }

            _consoleLogger.LogInformation($"Processed {counter} students");
            _logger.Info($"Processed {counter} students");
        }
        
        public async Task FullRunTeachers()
        {
            _consoleLogger.LogInformation("Getting teachers from Master store for processing");
            _logger.Info("Getting teachers from Master store for processing");

            _logger.Info("Processing all teachers");
            int counter = 0;
            using (var context = new ADMLCSContext())
            {
                foreach (var user in GetADMMasterStoreUsers(context))
                {
                    counter++;
                    await ProcessUsers(null,null,user, context);
                }
            }

            _consoleLogger.LogInformation($"Processed {counter} teachers");
            _logger.Info($"Processed {counter} teachers");
        }
        
        public async Task FullRunVO()
        {
            _consoleLogger.LogInformation("Getting VO students from Master store for processing");
            _logger.Info("Getting VO students from Master store for processing");

            _logger.Info("Processing all VO students");
            int counter = 0;
            using (var context = new Context())
            {
                foreach (var user in GetVOStudents(context))
                {
                    counter++;
                    await ProcessUsers(user, context);        
                }
            }

            _consoleLogger.LogInformation($"Processed {counter} VO students");
            _logger.Info($"Processed {counter} VO students");
        }


        public IEnumerable<ADMLCSUser> GetADMMasterStoreUsers(ADMLCSContext admContext)
        {

            var lcsUsers = admContext.ADMLCSUsers.Where(x => 
                (x.JobTitle.ToLower().Contains("adjunkt") && !_settingsManager.ExcludeSchools.Contains(x.School_Code_Primary)) 
            || (x.JobTitle.ToLower() == "lærer" && !_settingsManager.ExcludeSchools.Contains(x.School_Code_Primary)) 
            || (x.JobTitle.ToLower() == "rektor") && !_settingsManager.ExcludeSchools.Contains(x.School_Code_Primary)
            || _settingsManager.CustomUsernames.Contains(x.ad_samaccountname))
                .ToList();

            _logger.Info($"Got {lcsUsers.Count} teachers from Admin Master store");
            _consoleLogger.LogInformation($"Got {lcsUsers.Count} teachers from Admin Master store");

            if (lcsUsers.Count == 0)
            {
                _logger.Info("No teachers found in Admin Master store");
                _consoleLogger.LogInformation("No teachers found in Admin Master store");                
            }

            foreach (var user in lcsUsers)
            {
                yield return user;
            }
        }

        public IEnumerable<LCSUser> GetMasterStoreUsers(Context context)
        {
            var lcsUsers = context.LCSUsers.Where(x => !_settingsManager.ExcludeSchools.Contains(x.school_code)).ToList();
            
            _logger.Info($"Got {lcsUsers.Count} students from Skole Master store");
            _consoleLogger.LogInformation($"Got {lcsUsers.Count} students from Skole Master store");

            if (lcsUsers.Count == 0)
            {
                _logger.Info("No students found in Skole Master store");
                _consoleLogger.LogInformation("No students found in Skole Master store");
            }

            foreach (var user in lcsUsers)
            {
                yield return user;
            }
        }

        public IEnumerable<LCSUser> GetVOStudents(Context context)
        {
            var lcsUsers = context.LCSUsers.Where(x => x.school_code == "1VO").ToList();

            _logger.Info($"Got {lcsUsers.Count} VO students from Skole Master store");
            _consoleLogger.LogInformation($"Got {lcsUsers.Count} VO students from Skole Master store");

            if (lcsUsers.Count == 0)
            {
                _logger.Info("No VO students found in Skole Master store");
                _consoleLogger.LogInformation("No VO students found in Skole Master store");
            }

            foreach (var user in lcsUsers)
            {
                yield return user;
            }
        }

        public async Task RunSpecificUser(string adobjectid)
        {
            if (string.IsNullOrWhiteSpace(adobjectid))
            {
                _logger.Info("ADObjectID paramter is missing. Will not run module.");
                _consoleLogger.LogInformation("ADObjectID paramter is missing. Will not run module.");
                return;
            }

            using (var context = new Context())
            { 
                var schooluser = context.LCSUsers.FirstOrDefault(x => x.ADObjectID.ToString() == adobjectid);

                if (schooluser != null)
                {
                    _logger.Info("Found specified user in Skole Master store. Will process user.");
                    _consoleLogger.LogInformation("Found specified user in Skole Master store. Will process user.");

                    await ProcessUsers(schooluser,context);
                    return;
                }
            }
            
            using (var admContext = new ADMLCSContext())
            {
                var admuser = admContext.ADMLCSUsers.FirstOrDefault(x => x.ADObjectID.ToString() == adobjectid);

                if (admuser != null)
                {
                    _logger.Info("Found specified user in Admin Master store. Will process user.");
                    _consoleLogger.LogInformation("Found specified user in Admin Master store. Will process user.");

                    await ProcessUsers(null,null,admuser, admContext);
                }
            }
        }

        public async Task MaintainJamfGroups()
        {
            _consoleLogger.LogInformation("Maintaining Group archive");

            string response = await _api.GetRequestAsync("users/groups");
            if (!string.IsNullOrWhiteSpace(response))
            {
                try
                {
                    var groupsroot = JsonConvert.DeserializeObject<GroupRootobject>(response);
                    if (groupsroot != null)
                    {
                        _consoleLogger.LogInformation($"Got {groupsroot.count} groups from Jamf API. Updating archive and setting ACL for all of them.");
                        for (int i = 0; i < groupsroot.count; i++)
                        {
                            var group = groupsroot.groups[i];

                            dynamic updateGroup = new JObject();
                            dynamic acl = new JObject();

                            if (!group.name.ToLower().Contains("lærere"))
                            {
                                acl["teacher"] = "deny";
                                _logger.Info("Setting ACL to deny for group " + group.name);
                            }
                            else
                            {
                                acl["teacher"] = "allow";
                                _logger.Info("Setting ACL to allow for group " + group.name);
                            }
                            
                            updateGroup["acl"] = acl;

                            var groupResponse = await _api.PutRequestAsync($"users/groups/{group.id}", updateGroup);

                            if (groupResponse == null)
                            {
                                _logger.Error($"Could not set ACL for group: {group.name} for some reason.");
                            }

                            bool exists = _archiveContext.Groups.Any(x => x.id == group.id);

                            if (!exists)
                            {
                                _logger.Info($"Adding group {group.name} to archive!!");
                                _consoleLogger.LogInformation($"Adding group {group.name} to archive!!");
                                _archiveContext.Groups.Add(group);

                            }
                        }
                        await _archiveContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Info(ex.Message);
                }
            }
            else
            {
                _logger.Info("Could not get groups from Jamf API!");
            }
        }

        public async Task ProcessUsers(LCSUser user = null, Context context = null, ADMLCSUser admUser = null, ADMLCSContext admContext = null)
        {
            bool shouldUpdate = false;

            string nin = "";
            string samaccountname = "";
            string userPrincipalName = "";
            string firstname = "";
            string lastname = "";
            string adObjectID = "";
            string schoolCode = "";
            string jsonGroups = "";
            int locationId = 0;
            string domain = "";
            string isActive = "";
            string[] children = new string[] { };
            List<string> schoolCodes = new List<string>();
            List<int> teacherArray = new List<int>();
            List<string> memberOfArray = new List<string>();

            //Set values
            if (user != null)
            {
                if (user.IsActive.ToLower() == "false")
                {
                    _logger.Info("User has IsActive = False. Checking if we should delete or skip creation.");
                    if (!string.IsNullOrWhiteSpace(user.ADObjectID.ToString()))
                    {
                        var existingUser = _archiveContext.ArchiveUsers.FirstOrDefault(x => x.ADObjectID == user.ADObjectID.ToString());
                        if (existingUser != null)
                        {
                            bool gotId = Int32.TryParse(existingUser.JamfID.ToString(), out var id);
                            if (gotId)
                            {
                                bool deleted = await DeleteUser(id);
                                if (deleted)
                                {
                                    _logger.Info($"DELETED user {user.samaccountname} from Jamf. Will now delete from archive.");

                                    _archiveContext.Remove(existingUser);
                                    await _archiveContext.SaveChangesAsync();
                                }
                                else
                                {
                                    _logger.Error($"FAILED at deleting inactive user: {user.samaccountname}");
                                }
                            }
                            else
                            {
                                _logger.Error($"Was not able to delete user: {user.samaccountname} in Jamf because JamfID in archive not valid. Please check this user in archive: {user.ADObjectID.ToString()}");
                            }
                            return;
                        }   
                    }
                    _logger.Info("Will neither delete or create user because ADObjetID is null or empty. Skipping user.");
                    return;
                }

                nin = user.socialsecuritynumber ?? "";
                samaccountname = user.samaccountname + "@xxx.no" ?? "";
                userPrincipalName = user.samaccountname + "@xxx.no" ?? "";
                firstname = user.firstname ?? "";
                lastname = user.lastname ?? "";
                adObjectID = user.ADObjectID.ToString() ?? "";
                schoolCode = user.school_code ?? "";
                jsonGroups = user.JsonGroups ?? "";
                locationId = GetLocationId(schoolCode, context, null);
                memberOfArray = await GetMemberOfStudent(schoolCode, adObjectID, jsonGroups, context);
                domain = "";
                isActive = user.IsActive;
            }

            if (admUser != null)
            {
                if (admUser.IsActive.ToLower() == "false")
                {
                    _logger.Info("User has IsActive = False. Checking if we should delete or skip creation.");
                    if (!string.IsNullOrWhiteSpace(admUser.ADObjectID.ToString()))
                    {
                        var existingUser = _archiveContext.ArchiveUsers.FirstOrDefault(x => x.ADObjectID == admUser.ADObjectID.ToString());
                        if (existingUser != null)
                        {
                            bool gotId = Int32.TryParse(existingUser.JamfID.ToString(), out var id);
                            if (gotId)
                            {
                                bool deleted = await DeleteUser(id);
                                if (deleted)
                                {
                                    _logger.Info($"DELETED user {admUser.ad_samaccountname} from Jamf. Will now delete from archive.");

                                    _archiveContext.Remove(existingUser);
                                    await _archiveContext.SaveChangesAsync();
                                }
                                else
                                {
                                    _logger.Error($"FAILED at deleting inactive user: {admUser.ad_samaccountname}");
                                }
                            }
                            else
                            {
                                _logger.Error($"Was not able to delete user: {admUser.ad_samaccountname} in Jamf because JamfID in archive not valid. Please check this user in archive: {admUser.ADObjectID.ToString()}");
                            }
                            return;
                        }
                    }
                    _logger.Info("Will neither delete or create user because ADObjetID is null or empty. Skipping user.");
                    return;
                }

                nin = admUser.NIN;
                samaccountname = admUser.ad_samaccountname + "@xxx.no" ?? "";
                userPrincipalName = admUser.ad_upn ?? "";
                firstname = admUser.Firstname ?? "";
                lastname = admUser.Lastname ?? "";
                adObjectID = admUser.ADObjectID.ToString() ?? "";
                schoolCode = admUser.School_Code_Primary ?? "";
                schoolCodes = admUser.School_Code_Others.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                jsonGroups = admUser.JsonGroups ?? "";
                locationId = GetLocationId(schoolCode, null, admContext);
                memberOfArray = await GetMemberOfTeacher(schoolCode,schoolCodes, samaccountname, locationId, admContext);
                teacherArray = GetTeacher(jsonGroups, admContext);
                domain = "";
                isActive = admUser.IsActive;
            }


            if (!string.IsNullOrWhiteSpace(samaccountname) &&
                !string.IsNullOrWhiteSpace(userPrincipalName) &&
                !string.IsNullOrWhiteSpace(firstname) &&
                !string.IsNullOrWhiteSpace(lastname) &&
                !string.IsNullOrWhiteSpace(adObjectID) && 
                locationId > 0)
            {
                _logger.Info($"Processing user: {samaccountname}");

                try
                {
                    var existingUser = _archiveContext.ArchiveUsers.FirstOrDefault(x => x.ADObjectID == adObjectID);
                    if (existingUser == null)
                    {
                        _logger.Info($"User {samaccountname} is not in archive, will create in Jamf.");

                        var jamfUser = new JamfUser()
                        {
                            username = samaccountname,
                            password = "",
                            email = userPrincipalName,
                            firstName = firstname,
                            lastName = lastname,
                            locationId = locationId,
                            memberOf = memberOfArray.ToArray(),
                            teacher = teacherArray.ToArray(),
                            domain = domain,
                            children = children,
                        };

                        string jsonObject = JsonConvert.SerializeObject(jamfUser);

                        _logger.Info("Serialized version:" + jsonObject);

                        var response = await _api.PostUserAsync(jamfUser);
                        
                        if (response != null)
                        {
                            _logger.Info($"Successfully created user in Jamf with user ID: {response.id}");

                            _logger.Info($"Adding user to archive with ADObjectID {adObjectID}.");

                            var archiveObject = JsonConvert.DeserializeObject<ArchiveUser>(jsonObject);

                            archiveObject.JamfID = response.id;
                            archiveObject.ADObjectID = adObjectID.ToString();

                            if (string.IsNullOrWhiteSpace(archiveObject.JamfID.Value.ToString()))
                            {
                                _logger.Info("Did not get back JamfID from API after creation. Something is wrong. Will not save object to archive.");
                                _logger.Info($"Please inspect this user in Jamf, and delete it manually from Jamf API using Postman. {samaccountname}");
                                return;
                            }

                            string archiveobjStr = JsonConvert.SerializeObject(archiveObject);

                            _logger.Info($"Saving this in archive: {archiveobjStr}");

                            _archiveContext.ArchiveUsers.Add(archiveObject);
                            await _archiveContext.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        _logger.Info("User already exists in archive and also in Jamf. Checking if we should update user.");

                        shouldUpdate = false;

                        var propertiesToUpdate = new Dictionary<string, object>();

                        try
                        {
                            var newUser = new JamfUser()
                            {
                                username = samaccountname,
                                password = "",
                                email = userPrincipalName,
                                firstName = firstname,
                                lastName = lastname,
                                locationId = locationId,
                                memberOf = memberOfArray.ToArray(),
                                teacher = teacherArray.ToArray(),
                                domain = domain,
                                children = children,
                            };

                            if ((newUser.locationId == existingUser.locationId) == false)
                            {
                                await ReCreateUser(newUser, existingUser);
                                return;
                            }

                            var currentUserType = existingUser.GetType();
                            var newUserType = newUser.GetType();

                            foreach (var newProperty in newUserType.GetProperties())
                            {
                                var newValue = newProperty.GetValue(newUser);

                                var existingProperty = currentUserType.GetProperty(newProperty.Name);

                                var existingValue = existingProperty?.GetValue(existingUser);

                                bool wasArray = false;

                                if (newProperty.PropertyType == typeof(string[]))
                                {
                                    wasArray = true;

                                    string[] newstrinArr = (string[])newValue;
                                    string[] oldstringArr = (string[])existingValue;

                                    bool equal = newstrinArr.SequenceEqual(oldstringArr);
                                    if (!equal)
                                    {
                                        shouldUpdate = true;
                                        propertiesToUpdate[newProperty.Name] = newValue;
                                        
                                        _logger.Info($"Updated property {newProperty.Name}");
                                        for (int i = 0; i < newstrinArr.Length; i++)
                                        {
                                            _logger.Info("NEW GROUP " + newstrinArr[i]);

                                            if (oldstringArr.Length - 1 >= i)
                                            {
                                                _logger.Info("OLD GROUP " + oldstringArr[i]);
                                            }
                                        }
                                    }
                                }
                                else if (newProperty.PropertyType == typeof(int[]))
                                {
                                    wasArray = true;

                                    int[] newstrinArr = (int[])newValue;
                                    int[] oldstringArr = (int[])existingValue;

                                    bool equal = newstrinArr.SequenceEqual(oldstringArr);
                                    if (!equal)
                                    {
                                        shouldUpdate = true;
                                        propertiesToUpdate[newProperty.Name] = newValue; //
                                        _logger.Info($"Updated property");
                                        for (int i = 0; i < newstrinArr.Length; i++)
                                        {
                                            _logger.Info("NEW GROUP " + newstrinArr[i]);

                                            if (oldstringArr.Length - 1 >= i)
                                            {
                                                _logger.Info("OLD GROUP " + oldstringArr[i]);
                                            }
                                        }
                                    }
                                }

                                if (!Equals(newValue, existingValue) && !wasArray)
                                {
                                    shouldUpdate = true;
                                    propertiesToUpdate[newProperty.Name] = newValue;
                                    _logger.Info($"Updated property and value: {newProperty.Name} - {newValue} + Old property: {existingProperty.Name} - {existingValue}");
                                }
                            }

                            if (shouldUpdate)
                            {   
                                _logger.Info($"Some values have been changed for user. Will update user {samaccountname} in Jamf!");

                                dynamic updateObject = new JObject();

                                foreach (var kvp in propertiesToUpdate)
                                {
                                    var propertyInfo = newUserType.GetProperty(kvp.Key);
                                    var type = propertyInfo.PropertyType;

                                    JToken convertedValue = null;

                                    if (type == typeof(string))
                                    {
                                        if (!string.IsNullOrWhiteSpace(kvp.Value.ToString()))
                                        {
                                            convertedValue = new JValue(kvp.Value.ToString());
                                        }
                                        else
                                        {
                                            convertedValue = new JValue("");
                                        }
                                    }
                                    else if (type == typeof(int))
                                    {
                                        int intValue;
                                        if (int.TryParse(kvp.Value.ToString(), out intValue))
                                        {
                                            convertedValue = new JValue(intValue);
                                        }
                                        else
                                        {
                                            convertedValue = new JValue(0);
                                        }
                                    }
                                    else if (type == typeof(string[]))
                                    {
                                        string[] stringArrayValue = kvp.Value as string[];
                                        if (stringArrayValue != null)
                                        {
                                            convertedValue = JArray.FromObject(stringArrayValue);
                                        }
                                        else
                                        {
                                            convertedValue = JArray.FromObject(new string[] { });
                                        }
                                    }
                                    else if (type == typeof(int[]))
                                    {
                                        if (kvp.Value as int[] != null)
                                        {
                                            int[] IntArrVal = kvp.Value as int[];
                                            convertedValue = JArray.FromObject(IntArrVal);
                                        }
                                        else
                                        {
                                            convertedValue = JArray.FromObject(new int[] { });
                                        }
                                    }

                                    updateObject[kvp.Key] = convertedValue;
                                }

                                string updatedObjString = JsonConvert.SerializeObject(updateObject);

                                if (!string.IsNullOrWhiteSpace(updatedObjString))
                                {
                                    _logger.Info($"UPDATED Jamf object: {updatedObjString}");
                                }

                                var response = await _api.PutRequestAsync("users/" + existingUser.JamfID, updateObject);

                                if (response != null)
                                {
                                    _logger.Info("Successfully updated user in Jamf with new values! Updating archive.");

                                    existingUser.username = newUser.username;
                                    existingUser.firstName = newUser.firstName;
                                    existingUser.lastName = newUser.lastName;
                                    existingUser.email = newUser.email;
                                    existingUser.memberOf = newUser.memberOf;
                                    existingUser.teacher = newUser.teacher;
                                    existingUser.locationId = newUser.locationId;
                                    existingUser.domain = newUser.domain;
                                    existingUser.children = newUser.children;

                                    await _archiveContext.SaveChangesAsync();
                                }
                                else
                                {
                                    _logger.Error("FAILED at updating user in Jamf with new values. Not updating archive.");
                                }
                            }
                            else
                            {
                                _logger.Info($"No values have been updated. Will not update user {samaccountname} in Jamf.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
            else
            {
                _logger.Info("Not processing users as either one or multiple critical Jamf properties are missing!");
                _logger.Info($"User values: NIN: {nin} - ADObjectID: {adObjectID} - samaccountname: {samaccountname} - UPN: {userPrincipalName}");
            }
        }

        public async Task ReCreateUser(JamfUser newUser, ArchiveUser archiveUser)
        {
            var result = await _api.GetRequestAsync("users/" + archiveUser.JamfID);

            var groupsToMaintain = new List<string>();

            if (!string.IsNullOrWhiteSpace(result))
            {
                _logger.Info("RECREATE - FOUND USER IN JAMF.");
                try
                {
                    var apiUser = JsonConvert.DeserializeObject<SingleUserRoot>(result).user;
                    
                    if (apiUser != null)
                    {

                        int id = Int32.Parse(archiveUser.JamfID.ToString());

                        var deleted = await DeleteUser(id);

                        if (deleted)
                        {
                            _logger.Info("Deleted user because of a change in Location ID. Will now recreate user with new Location ID.");

                            var response = await _api.PostUserAsync(newUser);

                            if (response != null)
                            {
                                _logger.Info("Successfully recreated user in Jamf. Saving in archive. This is what we are saving to archive:");
                                
                                archiveUser.username = newUser.username;
                                archiveUser.email = newUser.email;
                                archiveUser.firstName = newUser.firstName;
                                archiveUser.lastName = newUser.lastName;
                                archiveUser.locationId = newUser.locationId;
                                archiveUser.memberOf = newUser.memberOf;
                                archiveUser.teacher = newUser.teacher;
                                archiveUser.children = newUser.children;
                                archiveUser.JamfID = response.id;

                                _logger.Info(JsonConvert.SerializeObject(archiveUser));

                                await _archiveContext.SaveChangesAsync();
                            }  
                        }
                        else
                        {
                            _logger.Error("FAILED at deleting user. Will NOT attempt to recreate.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    _logger.Info("Something failed here at attempt to re-create!");
                }
            }
        }

        public async Task<bool> DeleteUser(int id)
        {
            bool deleted = false;

            if (id != 0)
            {
                var response = await _api.DeleteRequestAsync("users/" + id);
                if (response != null)
                {
                    deleted = true;
                }
                else
                {
                    _logger.Error("Could not delete user from Jamf.");
                }
            }
            else
            {
                _logger.Info("User ID is 0. This is not allowed. Will not delete user.");            
            }
            return deleted;
        }

        public int GetLocationId(string schoolCode, Context context, ADMLCSContext admContext)
        {
            _logger.Info("Now setting locationid");
            int result = 0;

            if (!string.IsNullOrWhiteSpace(schoolCode))
            {
                try
                {
                    if (context != null)
                    {
                        LCSLocation school = null;

                        if (schoolCode.StartsWith("NO"))
                        {
                            school = context.LCSLocations.FirstOrDefault(x => x.Organization_Number.Equals(schoolCode));
                        }
                        else
                        {
                            school = context.LCSLocations.FirstOrDefault(x => x.UniqueID.Equals(schoolCode));
                        }
                        
                        if (school != null)
                        {
                            bool parsed = Int32.TryParse(school.JamfLocationId, out var intResult);
                            if (parsed)
                            {
                                result = intResult;
                            }
                            else
                            {
                                _logger.Error($"Could not get JamfLocationId from Dynamic Datastore for school Id: {schoolCode}");
                                result = 0;
                            }
                        }
                        else
                        {
                            _logger.Error($"Could not get JamfLocationId from Dynamic Datastore for school Id: {schoolCode}");
                            result = 0;
                        }
                    }
                    else if (admContext != null)
                    {
                        ADMLCSLocation school = null;

                        if (schoolCode.StartsWith("NO"))
                        {
                            school = admContext.ADMLCSLocations.FirstOrDefault(x => x.Organization_Number.Equals(schoolCode));
                        }
                        else
                        {
                            school = admContext.ADMLCSLocations.FirstOrDefault(x => x.UniqueID.Equals(schoolCode));
                        }

                        if (school != null)
                        {
                            bool parsed = Int32.TryParse(school.JamfLocationId, out var intResult);
                            if (parsed)
                            {
                                result = intResult;
                            }
                            else
                            {
                                _logger.Error($"Could not get JamfLocationId from Dynamic Datastore for school Id: {schoolCode}");
                                result = 0;
                            }
                        }
                        else
                        {
                            _logger.Error($"Could not get JamfLocationId from Dynamic Datastore for school Id: {schoolCode}");
                            result = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed at getting locationID for user.");
                    _logger.Error(ex);

                }
            }
            else
            {
                _logger.Error("Schoolcode for current user is blank or null. Will set location Id to 0.");
            }
            return result;
        }

        public List<int> GetTeacher(string jsonGroups, ADMLCSContext context)
        {
            var teacher = new List<int>();

            if (string.IsNullOrWhiteSpace(jsonGroups))
            {
                _logger.Info("Nothing in JsonGroups. Teacher[] will be empty.");
                return teacher;
            }

            try
            {
                var root = JsonConvert.DeserializeObject<List<JsonGroup>>(jsonGroups);
                
                if (root != null)
                {
                    List<JsonGroup> laererGrupper = root.Where(x => x.Type == "basisgruppe" || x.Type == "subject_group" && x.RoleType == "Instructor" && x.OwnerID.Contains("VO")).ToList();

                    foreach (var group in laererGrupper)
                    {
                        var school = context.ADMLCSLocations.FirstOrDefault(x => x.UniqueID == group.OwnerID);
                        if (school != null)
                        {
                            try
                            {
                                var id = _archiveContext.Groups.FirstOrDefault(x => x.name == group.Name + " - " + school.Name && x.locationId.ToString() == school.JamfLocationId).id;
                                
                                if (id > 1)
                                {
                                    teacher.Add(id);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex);
                                _logger.Error("Could not get ID from relevant group for Teacher[]");
                            } 
                        }
                        else
                        {
                            _logger.Info($"Could not find school for group: {group.Name} with OwnerID {group.OwnerID}. Not adding to teacher[]");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                _logger.Error("Unable to build teacher[] for this user");
            }

            return teacher;
        }

        public async Task<List<string>> GetMemberOfTeacher(string schoolCode, List<string> schoolCodes, string samaccountname, int locationId, ADMLCSContext context)
        {
            var memberOf = new List<string>();

            try
            {
                if (!string.IsNullOrWhiteSpace(schoolCode))
                {
                    ADMLCSLocation school = null;

                    if (schoolCode.StartsWith("NO"))
                    {
                        school = context.ADMLCSLocations.FirstOrDefault(x => x.Organization_Number.Equals(schoolCode));
                    }
                    else
                    {
                        school = context.ADMLCSLocations.FirstOrDefault(x => x.UniqueID.Equals(schoolCode));
                    }

                    if (school != null)
                    {
                        memberOf.Add(school.Name + " " + "Lærere");
                    }
                    
                }
            }
            catch (Exception ex)
            {
                _logger.Info($"Unable to build Jamf groups for user: {samaccountname} - {ex.Message}");
                _logger.Info(ex);
            }

            foreach (var group in memberOf)
            {
                try
                {
                    var existingGroup = _archiveContext.Groups.FirstOrDefault(x => x.name == group && x.locationId == locationId);
                    if (existingGroup == null)
                    {
                        _logger.Info($"Creating group: {group} in API because it doesn't exist, or it exists but locationId is different from teacher main location.");

                        dynamic newGroup = new JObject();
                        dynamic acl = new JObject();

                        acl["teacher"] = "allow";

                        newGroup.Add("name", group);

                        newGroup.Add("locationId", locationId);

                        newGroup["acl"] = acl;

                        ApiResponse response = await _api.PostGroupAsync(newGroup);
                        if (response != null)
                        {
                            _logger.Info($"Successfully created group: {group} in Jamf. Adding to archive.");

                            var archiveGroup = new Group()
                            {
                                id = (int)response.id,
                                locationId = locationId,
                                name = group,
                                userCount = 0
                            };

                            _archiveContext.Groups.Add(archiveGroup);

                        }
                        else
                        {
                            _logger.Error($"Could not create group {group} in Jamf. Not adding to archive. Will still create user.");
                        }
                    }
                    else
                    {
                        dynamic updateGroup = new JObject();
                        dynamic acl = new JObject();

                        acl["teacher"] = "allow";

                        updateGroup["acl"] = acl;

                        var groupResponse = await _api.PutRequestAsync($"users/groups/{existingGroup.id}", updateGroup);

                        if (groupResponse == null)
                        {
                            _logger.Error($"Could not set ACL for group: {group} for some reason.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    _logger.Error("Something went wrong when checking if one the teacher groups already exists in archive or not, or when creating in Jamf, or when saving in archive.");
                    _logger.Info("Will still continue with user.");
                }
            }

            await _archiveContext.SaveChangesAsync();

            return memberOf;
        }

        public async Task<List<string>> GetMemberOfStudent(string schoolcode, string adobjectid, string jsonGroups, Context context)
        {
            _logger.Info("Setting memberOf");
            var memberOf = new List<string>();

            LCSLocation school = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(schoolcode))
                {
                    

                    if (schoolcode.StartsWith("NO"))
                    {
                        school = context.LCSLocations.FirstOrDefault(x => x.Organization_Number.Equals(schoolcode));
                    }
                    else
                    {
                        school = context.LCSLocations.FirstOrDefault(x => x.UniqueID.Equals(schoolcode));
                    }

                    if (school != null)
                    {
                        memberOf = BuildJamfGroupsFromJsonGroups(jsonGroups, school.Name, adobjectid);
                    }
                    else
                    {
                        _logger.Error($"Was not able to find school for current user. User schoolcode: {schoolcode}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Info($"Unable to build Jamf groups for user: {adobjectid} - {ex.Message}");
                _logger.Info(ex);
            }

            if (school != null)
            {
                bool parsed = Int32.TryParse(school.JamfLocationId, out var locationId);

                foreach (var group in memberOf)
                {
                    if (!string.IsNullOrWhiteSpace(group))
                    {
                        _logger.Info($"Setting ACL for student group: {group}");

                        var existingGroup = _archiveContext.Groups.FirstOrDefault(x => x.name == group);
                        if (existingGroup != null)
                        {
                            dynamic updateGroup = new JObject();
                            dynamic acl = new JObject();

                            acl["teacher"] = "deny";

                            updateGroup["acl"] = acl;

                            var groupResponse = await _api.PutRequestAsync($"users/groups/{existingGroup.id}", updateGroup);

                            if (groupResponse == null)
                            {
                                _logger.Error($"Could not set ACL for group: {group} for some reason.");
                            }
                        }
                        else
                        {
                            dynamic newGroup = new JObject();
                            dynamic acl = new JObject();

                            acl["teacher"] = "deny";

                            newGroup["name"] = group;

                            newGroup["locationId"] = locationId;

                            newGroup["acl"] = acl;
                            
                            ApiResponse createGroupResponse = await _api.PostGroupAsync(newGroup);

                            if (createGroupResponse != null)
                            {
                                var archiveGroup = new Group()
                                {
                                    id = (int)createGroupResponse.id,
                                    locationId = locationId,
                                    name = group,
                                    userCount = 0
                                };

                                _archiveContext.Groups.Add(archiveGroup);  
                            }
                            else
                            {
                                _logger.Error("Was not able to create group in Jamf!");
                            }

                        }
                    }
                }

                await _archiveContext.SaveChangesAsync();
            }

            return memberOf;
        }

        public List<string> BuildJamfGroupsFromJsonGroups(string jsongroups, string schoolname, string adobjectid)
        {
            List<string> builderStrings = new List<string>();
            var root = new List<JsonGroup>();

            builderStrings.Add(schoolname + " Elever");

            if (string.IsNullOrWhiteSpace(jsongroups) && string.IsNullOrWhiteSpace(schoolname))
            { 
                return builderStrings;
            }
            try
            {
                root = JsonConvert.DeserializeObject<List<JsonGroup>>(jsongroups);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                _logger.Info("Could not deserialize JsonGroups to objects. Will not add any relevant class groups to memberOf.");
                return builderStrings;
            }

            if (schoolname == "xxx yyy")
            {
                _logger.Info("Student is a VO student. Will attempt to find Trinn in the relevant JsonGroups types.");

                var voKlasseGruppe = root.FirstOrDefault(x => x.Type.ToLower().Contains("klassegruppe") && x.OwnerID.ToLower().Contains("vo"));

                if (voKlasseGruppe != null)
                {
                    try
                    {
                        var parsed = Int32.TryParse(voKlasseGruppe.Name, out var parseResult);
                        if (parsed)
                        {
                            builderStrings.Add(parseResult + $".Trinn - {schoolname}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                    }
                }

                if(builderStrings.Count == 2)
                {
                    _logger.Info("VO student did not have any VO klassegruppe in JsonGroups. Will check for Trinn gruppe.");

                    var voTrinnGruppe = root.FirstOrDefault(x => x.Type.ToLower().Contains("trinn") && x.OwnerID.ToLower().Contains("vo"));

                    if (voTrinnGruppe != null)
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(voTrinnGruppe.Name))
                            {
                                string[] names = voTrinnGruppe.Name.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                if (names.Length > 0)
                                {
                                    var parsed = Int32.TryParse(names[1], out var parseResult);
                                    if (parsed)
                                    {
                                        builderStrings.Add(parseResult + $".Trinn - {schoolname}");
                                    }
                                    else
                                    {
                                        _logger.Info("Could not parse Trinn from Trinngruppe Name in JsonGroups. Will try Subject_group instead.");
                                    }
                                }
                                else
                                {
                                    _logger.Info("Found a VO Trinn gruppe in JsonGroups but we could not split into an array.Will try Subject_group instead.");
                                }
                            }
                            else
                            {
                                _logger.Info("VO Trinngruppe Name was null or empty. Moving on to Subject_group.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);   
                        }   
                    }  
                }

                if(builderStrings.Count == 2)
                {
                    _logger.Info("VO student did not have any VO Trinn gruppe in JsonGroups. Will check for Subject_group.");

                    var voGruppe = root.FirstOrDefault(x => x.Type.ToLower().Contains("subject_group") && x.OwnerID.ToLower().Contains("vo"));

                    if (voGruppe != null)
                    {
                        _logger.Info("VO STUDENT: We found a Subject_group. Will attempt to parse it.");

                        string trinn = "";

                        try
                        {
                            if (!string.IsNullOrWhiteSpace(voGruppe.Name))
                            {
                                trinn = voGruppe.Name.Substring(0, 2);

                                var parsed = Int32.TryParse(trinn, out var parseResult);
                                if (parsed)
                                {
                                    builderStrings.Add(parseResult + $".Trinn - {schoolname}");
                                }
                                else
                                {
                                    trinn = voGruppe.Name.Substring(0, 1);
                                    var secondParsed = Int32.TryParse(trinn, out var secondParsedResult);
                                    if (secondParsed)
                                    {
                                        builderStrings.Add(secondParsedResult + $".Trinn - {schoolname}");
                                    }
                                    else
                                    {
                                        _logger.Info("Could not get substring from subject_group Name beacuse its length is 0. Will get from class_level in MasterStore instead.");
                                    }
                                }
                            }
                            else
                            {
                                _logger.Info("VO subject_group Name was empty or null. Moving on to check directly on class_level in MasterStore instead.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                        }
                    }
                }

                if(builderStrings.Count == 2)
                {
                    _logger.Info("VO student does not have any subject_group groups in JsonGroups. Will check class_level in MasterStore.");
                    using (var context = new Context())
                    {
                        var currentUser = context.LCSUsers.FirstOrDefault(x => x.ADObjectID.ToString() == adobjectid);
                        if (currentUser != null)
                        {
                            if (!string.IsNullOrWhiteSpace(currentUser.class_level))
                            {
                                var lcsTrinn = currentUser.class_level;
                                var parsed = Int32.TryParse(lcsTrinn, out var parseResult);
                                if (parsed)
                                {
                                    builderStrings.Add(parseResult + $".Trinn - {schoolname}");
                                }
                            }
                        }
                        else
                        {
                            _logger.Info($"While trying to build student's memberOf attribute, we could not find the user in MasterStore. Will not add Trinngruppe to memberOf.");
                        }
                    }
                }

                if (builderStrings.Count == 2)
                {
                    _logger.Info($"Exhausted all possible options for finding Trinn for user but could not find it. Will not add Trinn gruppe to memberOf for user {adobjectid}");
                }
            }

            var trinnGruppe = root.FirstOrDefault(x => x.Type.ToLower().Contains("trinn"));
            var basisGruppe = root.FirstOrDefault(x => x.Type.ToLower().Contains("basisgruppe"));

            if (trinnGruppe != null && schoolname != "xxx yyy" && !schoolname.Contains("ooo"))
            {   
                string newLine = string.Empty;
                bool parsed = false;
                bool secondParsed = false;
                try
                {
                    if (trinnGruppe.GrepCode.Length > 0)
                    {
                        newLine = trinnGruppe.GrepCode.First().Substring(0, 2);
                        parsed = Int32.TryParse(newLine, out var parseResult);
                        if (parsed)
                        {
                            newLine = parseResult.ToString();
                        }
                        else
                        {
                            newLine = trinnGruppe.GrepCode.First().Substring(0, 1);
                            secondParsed = Int32.TryParse(newLine, out var secondParsedResult);
                            if (secondParsed)
                            {
                                newLine = secondParsedResult.ToString();
                            }
                        }
                    }
                    else
                    {
                        _logger.Info("Grepcode is empty for trinn gruppe in JsonGroups.");
                    }
                }
                catch(Exception ex)
                {
                    _logger.Error(ex);
                    _logger.Error($"Could not get substring from Trinngruppe in JsonGroups");
                }
                finally
                {
                    if (parsed || secondParsed)
                    {
                        builderStrings.Add(newLine + $".Trinn - {schoolname}");
                    }
                }
            }

            if (basisGruppe != null && schoolname != "xxx yyy" && !schoolname.Contains("ooo"))
            {
                if (basisGruppe.Name != "5.tr")
                {
                    builderStrings.Add(basisGruppe.Name + $" - {schoolname}");
                }
                else
                {
                    _logger.Info("This student is student at Berg Skole which has a weird Basisgruppe '5.tr' - which is not a valid basisgruppe.");
                }
            }

            return builderStrings;
        }
    }
}
