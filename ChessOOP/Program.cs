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
            // AllocConsole(); // Создать консоль
            Console.WriteLine("Тест: консоль активна!");

            ApplicationConfiguration.Initialize();
            Application.Run(new ChessForm());
        }
    }
}