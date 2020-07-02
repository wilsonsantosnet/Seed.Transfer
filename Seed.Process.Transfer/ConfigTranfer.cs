using Common.Cripto;
using Dapper;
using Seed.Process.Transfer.Enum;
using Seed.Process.Transfer.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Seed.Process.Transfer
{

    public class ConfigTranfer
    {

        public class ConfigInitial
        {

            public string[] Programids { get; set; }
            public string[] Process { get; set; }
            public string SeedParameters { get; set; }

         
        }

        public class ConfigSeedParameters
        {
            public string Procedure { get; set; }
            public int IdCampanha { get; set; }

            public ConfigSeedParameters()
            {
                this.Procedure = string.Empty;
                this.IdCampanha = 0;
            }
        }

        public class ConnectionString
        {
            public string ConnectionStringSource { get; set; }
            public string ConnectionStringDestination { get; set; }
        }

        public class ConfigRoot
        {
            public ConfigRoot()
            {
                this.ConfigInitial = new ConfigInitial();
                this.ConfigSeedParameters = new ConfigSeedParameters();
            }
            public List<ProcessConfig> ProcessConfig { get; set; }
            public ConfigInitial ConfigInitial { get; set; }
            public ConfigSeedParameters ConfigSeedParameters { get; set; }
        }

        public class ProcessConfig
        {
            public ProcessConfig()
            {
                this.RowsPerPage = 1000;
                this.MemoryData = new List<dynamic>();
                this.Retry = 0;
                this.Pagination = true;

            }

            public String AttributeBehavior { get; set; }
            public string Name { get; set; }
            public Guid Guid { get; set; }
            public Guid LastGuid { get; set; }
            public EProcessInfoType Direction { get; set; }
            public Boolean Active { get; set; }

            public Boolean ActiveDeleteDestination { get; set; }
            public string Destination { get; set; }
            public string Source { get; set; }
            public string PreviousSource { get; set; }

            public Func<int, ProcessConfig, int, int, IEnumerable<BaseDto>> GetData { get; set; }
            public Func<IEnumerable<BaseDto>, ProcessConfig, Boolean, int> Execute { get; set; }
            public Action<ProcessConfig> DeleteDestination { get; set; }
            public Action<ProcessConfig> Finished { get; set; }
            public Func<ProcessConfig, int> GetCount { get; set; }
            public ConnectionString ConnectionString { get; set; }
            public IEnumerable<dynamic> MemoryData { get; set; }
            public string FieldEhTransferido { get; set; }
            public int RowsPerPage { get; set; }
            public IEnumerable<int> ProgramId { get; set; }
            public int Retry { get; set; }
            public bool Pagination { get; set; }

            public string InfoConsole(dynamic program, ProcessConfig config, int threadIndex)
            {
                return $"[{program.Description} Thread {threadIndex} - { config.Direction} - {config.Destination} - {config.Name}] -";
            }
        }

        private readonly ConnectionString _cns;

        private readonly Cripto _cripto;

        private readonly ConfigSeedParameters _configSeedParameters;


        public ConfigTranfer(ConnectionString cns, ConfigSeedParameters configSeedParameters)
        {
            this._cns = cns;
            this._cripto = new Cripto();
            this._configSeedParameters = configSeedParameters;
        }

        public IEnumerable<ProcessConfig> GetConfig(dynamic program)
        {
            var programId = program.ProgramId;

            var input = new List<ProcessConfig>
            {
                new ProcessConfig
                {
                    Name = "Import_course_course",
                    Active = false,
                    ProgramId = new List<int> { (int)EProgram.SEED },
                    Direction = EProcessInfoType.Inpunt,
                    Source  = "course",
                    Destination  = "course",
                    ConnectionString = this._cns,
                    Execute = (data, config, finished)=> {

                        var result = data.Select(_ => _ as TRF_Course);
                        var Bc = new BookCopy<TRF_Course>(config.ConnectionString.ConnectionStringDestination);

                        Bc.Copy(result, config.Destination).Wait();

                        if (finished)
                           config.Finished(config);

                        return  (result as IEnumerable<BaseDto>).Count();
                    },
                    Finished = (config)=>{
                    },
                    GetCount = (config) => {
                        return CountDataSource(config);
                    },
                    ActiveDeleteDestination = true,
                    DeleteDestination = (config) => {
                       DeleteDataDestination(config);
                    },
                    GetData = (threadId,config, skip, step) => {

                        using (var connectionFeatures = new Connection(config.ConnectionString.ConnectionStringSource).GetInstancePG())
                        {
                            var data = connectionFeatures.Query(GetDataSource(config,skip, step), new
                            {
                             
                            }, commandType: CommandType.Text);

                            return data.Select(_ => new TRF_Course
                            {
                                Id = _.id,
                                Name = _.name,
                                CourseOrder = _.course_order,
                                Cover = _.cover,
                                Locale = _.locale
                            });
                        }
                    }
                },
            };


            var all = new List<ProcessConfig>();
            var control = new Dictionary<string, object>();
            all.AddRange(input);
            all.ForEach(_ => control.Add(_.Name, _));

            return all.OrderBy(_ => _.Direction);
        }

        #region helpers

        

        private static int CountDataSource(ProcessConfig config)
        {
            using (var connectionSource = new Connection(config.ConnectionString.ConnectionStringSource).GetInstancePG())
            {
                return connectionSource.ExecuteScalar<int>($"Select Count(*) from {config.Source}", new { }, commandType: CommandType.Text);
            }
        }


        private static void DeleteDataDestination(ProcessConfig config)
        {
            using (var connectionDestinantion = new Connection(config.ConnectionString.ConnectionStringDestination).GetInstanceSql())
            {
                var commmandText = $"DELETE FROM {config.Destination}";
                connectionDestinantion.ExecuteScalar(commmandText, new
                {
                    config.ProgramId
                }, commandType: CommandType.Text, commandTimeout: 0);
            }
        }


        #endregion

        #region Querys 

        private static string GetDataSource(ProcessConfig config, int skip, int step)
        {
            var sql = $"Select * from {config.Source} { QueryBasePaging(skip, step)}";
            return sql;
        }

        private static string QueryBasePaging(int skip, int step)
        {
            if (skip == 0 && step == 0)
                return string.Empty;

            var sql = $" OFFSET {skip} ROWS FETCH NEXT {step} ROWS ONLY;";
            return sql;
        }


        #endregion



    }
}
