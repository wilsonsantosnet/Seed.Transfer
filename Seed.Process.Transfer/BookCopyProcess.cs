using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using Seed.Process.Transfer.Enum;
using static Seed.Process.Transfer.ConfigTranfer;

namespace Seed.Process.Transfer
{
    public abstract class BookCopyProcess
    {

        private readonly string _connectionStringAccount;
        delegate void Transfer(int step, dynamic program, dynamic config, dynamic totalRows, int InitialSkip);
        private readonly IDbConnection _connectionAccount;

        public BookCopyProcess(string connectionStringAccount)
        {
            this._connectionStringAccount = connectionStringAccount;
            this._connectionAccount = new SqlConnection(this._connectionStringAccount);
        }

        protected abstract ConnectionString GetConfigConnectionStringByProgram(int programId);
        protected abstract IEnumerable<ProcessConfig> GetProcessConfig();
        protected abstract ConfigInitial GetConfigInitial();
        protected abstract ConfigSeedParameters GetSeedParameters();
        public async void Execute(string[] args = null)
        {
            //var configInitial = new ConfigInitial();
            var configInitial = this.GetConfigInitial();
            var configSeedParameters = this.GetSeedParameters();
            var program = new
            {

                ProgramId = 5,
                Description = "CNANET",

            };
            var connectionString = this.GetConfigConnectionStringByProgram(program.ProgramId);

            Console.WriteLine($"[{program.Description}] Load data for program {program.Description}");
            IEnumerable<ProcessConfig> configTransfer = new ConfigTranfer(connectionString, configSeedParameters).GetConfig(program);
            Console.WriteLine($"[{program.Description}] Init transfer from program {program.Description}");

            var processConfig = this.GetProcessConfig();

            foreach (var item in processConfig)
            {
                if (configTransfer.Where(_ => _.Name == item.Name).SingleOrDefault() == null)
                    continue;

                configTransfer.Where(_ => _.Name == item.Name).SingleOrDefault().ProgramId = item.ProgramId;
                configTransfer.Where(_ => _.Name == item.Name).SingleOrDefault().RowsPerPage = item.RowsPerPage;
                configTransfer.Where(_ => _.Name == item.Name).SingleOrDefault().Retry = item.Retry;
                configTransfer.Where(_ => _.Name == item.Name).SingleOrDefault().Active = item.Active;
                configTransfer.Where(_ => _.Name == item.Name).SingleOrDefault().Source = item.Source;
                configTransfer.Where(_ => _.Name == item.Name).SingleOrDefault().PreviousSource = item.PreviousSource;
                configTransfer.Where(_ => _.Name == item.Name).SingleOrDefault().AttributeBehavior = item.AttributeBehavior;
            }

            if (configInitial.Process.Contains(EProcessInfoType.Inpunt.ToString().ToLower()))
                DirectionProcess(program, configTransfer, EProcessInfoType.Inpunt);

            if (configInitial.Process.Contains(EProcessInfoType.Output.ToString().ToLower()))
                DirectionProcess(program, configTransfer, EProcessInfoType.Output);
        }



        #region helper 

        private void DirectionProcess(dynamic program, IEnumerable<ProcessConfig> configTransfer, EProcessInfoType direction)
        {
            var process = configTransfer
                                    .Where(_ => _.Direction == direction)
                                    .Where(_ => _.ProgramId.Where(programId => programId == program.ProgramId).Any())
                                    .Where(_ => _.Active)
                                    .ToList();

            if (process.Count() == 0)
                Console.WriteLine($"[{program.Description}] no process active found in program {program.Description} , {direction.ToString()}");

            var attemps = 0;
            for (int i = 0; i < process.Count(); i++)
            {
                var config = process[i];
                try
                {
                    Process(program, config, direction);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{config.InfoConsole(program, config, 0)} ERROR - {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.White;
                    attemps++;
                    if (attemps <= config.Retry)
                        i--;
                    else
                        attemps = 0;
                }
            }

        }

        private void Process(dynamic program, ProcessConfig config, EProcessInfoType direction)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{config.InfoConsole(program, config, 0)} init at {DateTime.Now}");

            var totalRows = config.GetCount(config);
            Console.WriteLine($"{config.InfoConsole(program, config, 0)} Total rows {totalRows}");

            var processDescription = $"Transfer {totalRows} rows to {config.Destination} on {config.Name} process";

            if (totalRows == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{config.InfoConsole(program, config, 0)} Already up to date {DateTime.Now}");
                config.Finished(config);
                return;
            }

            var threads = 1;
            var rowPerPage = config.RowsPerPage;
            var initialSkip = 0;

            var rowsPerThreads = totalRows / threads;
            var pagesPerThread = (int)Math.Ceiling(rowsPerThreads / Convert.ToDecimal(rowPerPage));
            if (!config.Pagination)
            {
                rowsPerThreads = totalRows;
                pagesPerThread = 1;
            }


            Console.ForegroundColor = ConsoleColor.White;
            if (config.ActiveDeleteDestination)
            {
                Console.WriteLine($"{config.InfoConsole(program, config, 0)}  Init Delete as {DateTime.Now}");
                config.DeleteDestination(config);
                Console.WriteLine($"{config.InfoConsole(program, config, 0)}  End Delete  as {DateTime.Now}");
            }

            for (int i = 1; i <= threads; i++)
            {
                var state = new ThreadState(i, initialSkip, rowPerPage, rowsPerThreads, totalRows, pagesPerThread, program, config);
                var t = new Thread(new ThreadStart(state.Run));
                t.Start();
                initialSkip = (i * pagesPerThread) * rowPerPage;
            }
        }



        #endregion


    }
}
