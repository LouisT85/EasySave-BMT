using System;
using easySave_BMT.ViewModel_;

namespace easySave_BMT
{
    class Program
    {
        static void Main(string[] args) // Method who runs the app
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // Initialize the application
            ViewModel viewModel = new ViewModel();
            
            // Run the application
            viewModel.RunApp();
        }
    }
}