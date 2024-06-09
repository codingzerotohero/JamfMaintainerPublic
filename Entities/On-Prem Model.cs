using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JamfMaintainer.Entities
{

    public class Messagebody
    {
        public List<UpdatedUser> JsonData { get; set; }
    }

    public class UpdatedUser
    {
        public string ADObjectID { get; set; }
        public string? socialsecuritynumber { get; set; }
        public string? samaccountname { get; set; }
    }
    public class LCSUser
    {
        [Key]
        public Guid? ADObjectID { get; set; }
        public string? LCS_Operation { get; set; }
        public DateTime? LCS_TimeStamp { get; set; }
        public string? LCS_Status { get; set; }
        public string? LCS_Message { get; set; }
        public string? LCS_FirstTimePassword { get; set; }
        public string? LCS_Error { get; set; }
        [Column("class")]
        public string? _class { get; set; }
        public string? class_level { get; set; }
        public string? firstname { get; set; }
        public string? IsActive { get; set; }
        public string? lastname { get; set; }
        public string? municipal { get; set; }
        public string? objectsid { get; set; }
        public string? Operationstatus { get; set; }
        public string? role { get; set; }
        public string? samaccountname { get; set; }
        public string? school_code { get; set; }
        public string? socialsecuritynumber { get; set; }
        public string? UniqueID { get; set; }
        public string? source_domain_dn { get; set; }
        public string? leave_year { get; set; }
        public string? birthdate_Feide { get; set; }
        public string? onetimepassword { get; set; }
        public string? whencreated { get; set; }
        public string? gender { get; set; }
        public string? validated_socialsecuitynumber { get; set; }
        public string? userPrincipalName { get; set; }
        public string? JsonGroups { get; set; }
    }


    public class LCSLocation
    {

        public int ItemID { get; set; }
        [Key]
        public string? UniqueID { get; set; }
        public string? MunicipalID { get; set; }
        public string? code { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Organization_Number { get; set; }
        public string? ad_lokasjon_aktiv_OU { get; set; }
        public string? ad_group { get; set; }
        public string? ShortName { get; set; }
        public string? JamfLocationId { get; set; }
    }

    public class VFSUser
    {
        public string? socialsecuritynumber { get; set; }
        public string? firstname { get; set; }
        public string? lastname { get; set; }
        public string? birthdate { get; set; }
        public string? gender { get; set; }
        public string? street { get; set; }
        public string? postcode { get; set; }
        public string? city { get; set; }
        public string? country { get; set; }
        public string? mobile { get; set; }
        public string? email { get; set; }
        public string? telephone { get; set; }
        public string? school_code { get; set; }
        public string? schools { get; set; }
        public string? primary_role { get; set; }
        public string? municipal { get; set; }
        [Key]
        public Guid unique_id { get; set; }
        [Column("class")]
        public string? _class { get; set; }
        public bool? IsActive { get; set; }
        public string? contactteacher { get; set; }
        public DateTime? modified { get; set; }
        public string? classes { get; set; }
        public string? clgid { get; set; }
        public string? contactteacherids { get; set; }
        public string? schoolname { get; set; }
        public string? parent1 { get; set; }
        public string? parent2 { get; set; }
        public string? parent_childrenids { get; set; }
        public string? startyear { get; set; }
        public string? endyear { get; set; }
        public string? contactperson { get; set; }
        public string? Groups { get; set; }
        public string? JsonGroups { get; set; }
        public string? Grade { get; set; }
        public string? Title { get; set; }
    }

    public class UserInfoUser
    {
        public DateTime? Created { get; set; }
        public DateTime? Changed { get; set; }
        [Key]
        public Guid Global_EmployeeID { get; set; }
        public bool? IsCurrent { get; set; }
        public string? SourceSystem { get; set; }
        public int? InstanceID { get; set; }
        public string? InstanceName { get; set; }
        public string? account_type { get; set; }
        public string? ObjectClass { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? Fullname { get; set; }
        public string? Birthdate { get; set; }
        public string? Gender { get; set; }
        public string? Street { get; set; }
        public string? Region { get; set; }
        public string? Postcode { get; set; }
        public string? Locality { get; set; }
        public string? Country { get; set; }
        public string? Email { get; set; }
        public string? Telephone { get; set; }
        public string? Workphone { get; set; }
        public string? Mobile { get; set; }
        public string? UserIDs { get; set; }
        public string? SourcedID { get; set; }
        public string? NIN { get; set; }
        public string? PrimaryRole { get; set; }
        public string? PrimarySchoolCode { get; set; }
        public string? PrimarySchoolName { get; set; }
        public string? SchoolCodes { get; set; }
        public string? Class { get; set; }
        public string? Grade { get; set; }
        public string? ContactTeachers { get; set; }
        public string? JsonGroups { get; set; }
        public string? ExtensionFields { get; set; }
        public string? ChildrenIDs { get; set; }
        public string? ParentIDs { get; set; }
        public string? MunicipalName { get; set; }
        public string? workforceID { get; set; }
        public string? GroupType { get; set; }
        public string? GroupTypeLevel { get; set; }
        public string? Description_Full { get; set; }
        public string? Description_Long { get; set; }
        public string? Description_Short { get; set; }
        public string? ParentID { get; set; }
    }

    public class ADMLCSUser
    {
        [Key]
        public Guid? ADObjectID { get; set; }
        public string? LCS_Operation { get; set; }
        public DateTime? LCS_TimeStamp { get; set; }
        public string? LCS_Status { get; set; }
        public string? LCS_Message { get; set; }
        public string? LCS_FirstTimePassword { get; set; }
        public string? LCS_Error { get; set; }
        public string? source_domain_dn { get; set; }
        public string? ObjectInfoId { get; set; }
        public string? OrgUnit_ID_Primary { get; set; }
        public string? OrgUnit_ID_All { get; set; }
        public string? JobTitle { get; set; }
        public string? Mobile { get; set; }
        public string? WorkPhone { get; set; }
        public string? Phone { get; set; }
        public string? PositionCode_Primary { get; set; }
        public string? PositionCode_All { get; set; }
        public string? PositionName_Primary { get; set; }
        public string? PositionName_All { get; set; }
        public string? MunicipalName { get; set; }
        public string? SourceInstanceId { get; set; }
        public string? Firstname { get; set; }
        public string? FirstTimePassword { get; set; }
        public string? Lastname { get; set; }
        public string? NIN { get; set; }
        public string? ID_Store { get; set; }
        public string? IsActive { get; set; }
        public string? IsCurrent { get; set; }
        public string? TerminateDate { get; set; }
        public string? QuarantineLevel { get; set; }
        public string? ad_username { get; set; }
        public string? ad_upn { get; set; }
        public string? ad_description { get; set; }
        public string? ad_mail { get; set; }
        public string? ad_sid { get; set; }
        public string? ad_objectid { get; set; }
        public string? ad_whencreated { get; set; }
        public string? OperationStatus { get; set; }
        public string? QuarantineQueue { get; set; }
        public string? ad_objectsid { get; set; }
        public string? LegalLastName { get; set; }
        public string? LegalFirstName { get; set; }
        public string? tlfWorkDirectHide { get; set; }
        public string? tlfMobileHidePrivate { get; set; }
        public string? ThirdPartyId { get; set; }
        public string? DelegateAccessTokens { get; set; }
        public string? BusinessApplication2AccessTokens { get; set; }
        public string? School_Code_Primary { get; set; }
        public string? School_Code_Others { get; set; }
        public string? JsonGroups { get; set; }
        public string? feide_authenticator { get; set; }
        public string? feide_authenticator_key { get; set; }
        public string? Employeeid { get; set; }
        public string? OneTimePassword { get; set; }
        public string? ad_samaccountname { get; set; }
        public string? MFA_Enabled { get; set; }
        public string? MFA_Pin { get; set; }
        public string? MD5 { get; set; }
        public string? feide_authenticator_unencryptedkey { get; set; }
    }

    public class ADMLCSLocation
    {

        public int ItemID { get; set; }
        [Key]
        public string? UniqueID { get; set; }
        public string? MunicipalID { get; set; }
        public string? code { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Organization_Number { get; set; }
        public string? JamfLocationId { get; set; }
    }
}
