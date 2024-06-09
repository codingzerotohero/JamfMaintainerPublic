using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JamfMaintainer
{
    public class SettingsManager
    {
        private static SettingsManager _instance;
        private readonly IConfiguration _configuration;

        public string LCSConnectionString { get; private set; }
        public string SkoleLCSTable { get; private set; }
        public string SkoleSchoolTable { get; private set; }
        public string ADMLCSConnectionString { get; private set; }
        public string ADMLCSTable { get; private set; }
        public string ADMSchoolTable { get; private set; }
        public string UserStorageConnectionString { get; private set; }
        public string ArchiveConnectionString { get; private set; }
        public string JamfUsername { get; private set; }
        public string JamfPassword { get; private set; }
        public string JamfBaseAPIUrl { get; private set; }
        public List<string> CustomUsernames { get; private set; }
        public List<string> ExcludeSchools { get; private set; }

        public SettingsManager()
        {
            _configuration = new ConfigurationBuilder()
                .AddXmlFile("AppSettings.xml", optional: false, reloadOnChange: true)
                .Build();

            LoadConfigValues();
        }

        public static SettingsManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new SettingsManager();
            }
            return _instance;
        }

        private void LoadConfigValues()
        {
            LCSConnectionString = _configuration["ConnectionStrings:LCSConnection"];
            SkoleLCSTable = _configuration["ConnectionStrings:SkoleLCSTable"];
            SkoleSchoolTable = _configuration["ConnectionStrings:SkoleSchoolTable"];
            ADMLCSConnectionString = _configuration["ConnectionStrings:ADMLCSConnection"];
            ADMLCSTable = _configuration["ConnectionStrings:ADMLCSTable"];
            ADMSchoolTable = _configuration["ConnectionStrings:ADMSchoolTable"];
            UserStorageConnectionString = _configuration["ConnectionStrings:UserStorageConnection"];
            ArchiveConnectionString = _configuration["ConnectionStrings:ArchiveConnection"];
            JamfUsername = _configuration["ConnectionStrings:JamfUsername"];
            JamfPassword = _configuration["ConnectionStrings:JamfPassword"];
            JamfBaseAPIUrl = _configuration["ConnectionStrings:JamfBaseAPIUrl"];
            CustomUsernames = _configuration["CustomUsernames:Usernames"].Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
            ExcludeSchools = _configuration["ExcludeSchools:Schools"].Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public string GetSetting(string key)
        {
            return _configuration[key];
        }
    }

    
}
