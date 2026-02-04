using System;
using easySave_BMT.ViewModel_;

namespace easySave_BMT
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // Initialize the application
            ViewModel viewModel = new ViewModel();
            
            // Run the application
            viewModel.RunApp();
        }
    }
}