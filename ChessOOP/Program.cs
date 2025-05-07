using System.Runtime.InteropServices;
namespace ChessOOP
{
    internal static class Program
    {
        [STAThread]
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();
        static void Main()
        {
            // AllocConsole(); // ������� �������
            Console.WriteLine("����: ������� �������!");

            ApplicationConfiguration.Initialize();
            Application.Run(new ChessForm());
        }
    }
}