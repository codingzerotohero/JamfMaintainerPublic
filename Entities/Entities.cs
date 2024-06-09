using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JamfMaintainer.Entities
{

    public class LocationRootobject
    {
        public List<Location> locations { get; set; }
    }

    public class Location
    {
        public int id { get; set; }
        public string name { get; set; }
        public bool isDistrict { get; set; }
        public string street { get; set; }
        public string streetNumber { get; set; }
        public string postalCode { get; set; }
        public string city { get; set; }
        public string source { get; set; }
        public object asmIdentifier { get; set; }
        public string schoolNumber { get; set; }
    }


    public class UsersRootobject
    {
        public int code { get; set; }
        public int count { get; set; }
        public List<User> users { get; set; }
    }
    public class GroupRootobject
    {
        public int code { get; set; }
        public int count { get; set; }
        public Group[] groups { get; set; }
    }

    public class ApiResponse
    {
        public int code { get; set; }
        public string message { get; set; }
        public int? id { get; set; }
    }

    public class SingleUserRoot
    {
        public int code { get; set; }
        public User user { get; set; }
    }

    public class SingleGroupRoot
    {
        public int code { get; set; }
        public Group group { get; set; }
    }

    public class User
    {
        public int id { get; set; }
        public int locationId { get; set; }
        public string status { get; set; }
        public int deviceCount { get; set; }
        public string email { get; set; }
        public string username { get; set; }
        public string domain { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string name { get; set; }
        public int?[] groupIds { get; set; }
        public string[] groups { get; set; }
        public int?[] teacherGroups { get; set; }
        public object[] children { get; set; }
        public object[] vpp { get; set; }
        public string notes { get; set; }
        public bool exclude { get; set; }
        public string modified { get; set; }
    }

    public class Group
    {
        [Key]
        public int id { get; set; }
        public int locationId { get; set; }
        public string name { get; set; }
        public string? description { get; set; }
        public int userCount { get; set; }
        public string? modified { get; set; }

    }

    public class JamfUser
    {
        public string username { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public int locationId { get; set; }
        public string domain { get; set; }
        public string[] memberOf { get; set; }
        public int[] teacher { get; set; }
        public string[] children { get; set; }
    }

    public class ArchiveUser : JamfUser
    {
        [Key]
        public string? ADObjectID { get; set; }
        public int? JamfID { get; set; }

    }

    public class RelationInfo
    {
        public Guid RelationInfoID { get; set; }
        public Guid ObjectID { get; set; } //Jamf ID for bruker eller gruppe, starter med brukere så ser vi hvordan det går og legger på grupper
        public Guid RelatedObjectID { get; set; }
        public string Location { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class JsonContent : StringContent
    {
        public JsonContent(JObject data) : base(data.ToString(), Encoding.UTF8, "application/json")
        { }
    }
}
