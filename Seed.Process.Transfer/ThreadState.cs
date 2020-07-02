using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Seed.Process.Transfer.Model;
using static Seed.Process.Transfer.ConfigTranfer;

namespace Seed.Process.Transfer
{
    public class ThreadState
    {
        private int RowsPerPage { get; set; }
        private int Id { get; set; }
        private int InitialRowPerThread { get; set; }
        private int EndRowThread { get; set; }
        private int RowsPerThreads { get; set; }
        private int TotalRows { get; set; }
        private dynamic Program { get; set; }
        private ProcessConfig Config { get; set; }
        private int Pages { get; set; }

        public ThreadState(int id, int initialRowPerThread, int rowsPerPage, int rowsPerThread, int totalRows, int pages, dynamic program, ProcessConfig config)
        {
            this.Id = id;
            this.Program = program;
            this.Config = config;
            this.RowsPerPage = rowsPerPage;
            this.RowsPerThreads = rowsPerThread;
            this.TotalRows = totalRows;
            this.InitialRowPerThread = initialRowPerThread;
            this.Pages = pages;
        }



        public void Run()
        {
            var threadId = this.Id;
            var skip = this.InitialRowPerThread;
            var endRowPerThread = this.InitialRowPerThread + this.RowsPerThreads;

            var page = 1;
            var attempts = 0;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{this.Config.InfoConsole(Program, this.Config, threadId)} Total de Paginas da Thread {threadId}: {Pages}");

            while (page <= Pages)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    var result = this.Config.GetData(threadId, this.Config, skip, this.RowsPerPage);
                    var packageCount = this.Config.Execute(result, Config, page == Pages);

                    if (page >= Pages)
                        Console.ForegroundColor = ConsoleColor.Red;

                    if (page == 0)
                        Console.ForegroundColor = ConsoleColor.DarkYellow;

                    if (page <= Pages)
                        Console.ForegroundColor = ConsoleColor.White;

                    if (page == Pages)
                        Console.ForegroundColor = ConsoleColor.Green;


                    Console.WriteLine($"{this.Config.InfoConsole(Program, this.Config, threadId)}  Pagina: {page} de {Pages}  with {packageCount} Rows , Skip : {skip} - {DateTime.Now}");
                    attempts = 0;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ERROR : {this.Config.InfoConsole(Program, this.Config, threadId)}   Pagina: {page} de {Pages}  ex: {ex.Message}");

                    if (attempts < Config.Retry)
                    {
                        Console.WriteLine($"RETRY : {this.Config.InfoConsole(Program, this.Config, threadId)}  Pagina: {page} de {Pages} , Skip : {skip} - {DateTime.Now}");
                        attempts++;
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                        attempts = 0;

                    Console.ForegroundColor = ConsoleColor.White;
                }
                finally
                {
                    if (attempts == 0)
                    {
                        page++;
                        skip = skip + this.RowsPerPage;
                        System.Threading.Thread.Sleep(300);
                    }
                    else {
                        Console.WriteLine($"ATTEMPTS {attempts} : {this.Config.InfoConsole(Program, this.Config, threadId)}  Pagina: {page} de {Pages}");
                    }
                }

            }
        }

    }
}
