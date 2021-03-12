
using Opc.Ua.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Opc.Ua.Sample.Simulation
{
    public class ApplicationMessageDlg : IApplicationMessageDlg
    {
        private string message = string.Empty;
        private bool ask = false;

        public override void Message(string text, bool ask)
        {
            this.message = text;
            this.ask = ask;
        }

        public override async Task<bool> ShowAsync()
        {
            if (ask)
            {
                message += " (y/n, default y): ";
                Console.Write(message);
            }
            else
            {
                Console.WriteLine(message);
            }
            if (ask)
            {
                try
                {
                    ConsoleKeyInfo result = Console.ReadKey();
                    Console.WriteLine();
                    return await Task.FromResult((result.KeyChar == 'y') || (result.KeyChar == 'Y') || (result.KeyChar == '\r'));
                }
                catch
                {
                    // intentionally fall through
                }
            }
            return await Task.FromResult(true);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                ConsoleServer(args).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
            }
        }

        private static async Task ConsoleServer(string[] args)
        {
            ApplicationInstance.MessageDlg = new ApplicationMessageDlg();
            ApplicationInstance application = new ApplicationInstance();

            // load the application configuration.
            application.ConfigSectionName = "Opc.Ua.Station";
            application.ApplicationType = ApplicationType.Server;
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false).ConfigureAwait(false);

            // check the application certificate.
            await application.CheckApplicationInstanceCertificate(false, 0).ConfigureAwait(false);

            // create cert validator
            config.CertificateValidator = new CertificateValidator();
            config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

            // start the server.
            await application.Start(new FactoryStationServer()).ConfigureAwait(false);

            Console.WriteLine("Server started.");
            Thread.Sleep(Timeout.Infinite);
        }

        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                // accept all OPC UA client certificates
                Console.WriteLine("Automatically trusting client certificate " + e.Certificate.Subject);
                e.Accept = true;
            }
        }
    }
}
