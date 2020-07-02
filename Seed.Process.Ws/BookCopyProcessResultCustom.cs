using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using static Seed.Process.Transfer.ConfigTranfer;

namespace Seed.Process.Transfer
{
    public class BookCopyProcessResultCustom : BookCopyProcess
    {
        public BookCopyProcessResultCustom(string connectionStringAccount) : base(connectionStringAccount)
        {

        }

        protected override ConnectionString GetConfigConnectionStringByProgram(int programId)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();
            

            return new ConnectionString
            {
                ConnectionStringSource = configuration.GetSection($"ConfigConnectionString:Source").Value,
                ConnectionStringDestination = configuration.GetSection($"ConfigConnectionString:Destination").Value
            };
        }

        protected override IEnumerable<ProcessConfig> GetProcessConfig()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();

            ConfigRoot configRoot = new ConfigRoot();
            configuration.GetSection($"ProcessConfiguration").Bind(configRoot);
            return configRoot.ProcessConfig;
        }
        protected override ConfigInitial GetConfigInitial()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();

            ConfigRoot configRoot = new ConfigRoot();
            configuration.GetSection($"ConfigInitial").Bind(configRoot.ConfigInitial);
            return configRoot.ConfigInitial;
        }

        protected override ConfigSeedParameters GetSeedParameters()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();

            ConfigRoot configRoot = new ConfigRoot();
            configuration.GetSection($"ConfigSeedParameters").Bind(configRoot.ConfigSeedParameters);
            return configRoot.ConfigSeedParameters;
        }
    }
}
