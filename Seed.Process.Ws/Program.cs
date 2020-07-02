using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Seed.Process.Transfer.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                var configuration = builder.Build();

                var bcp = new BookCopyProcessResultCustom(configuration.GetSection("ConfigConnectionString:Account").Value);
                bcp.Execute(args);
                System.Console.Read();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                System.Console.Read();
            }
          

        }
    }
}
