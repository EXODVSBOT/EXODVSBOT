using System.Windows.Forms;

namespace ExodvsBot
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}

//adicionar pre�o do btc autom�tico
//adicionar valor em dolar na carteira
//adicionar endpoint de m�trica para bots rodando
