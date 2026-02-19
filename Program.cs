using System;
using easySave_BMT.ViewModel_;

namespace easySave_BMT
{
    /// <summary>
    /// Console application entry point.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Starts the console workflow.
        /// </summary>
        /// <param name="args">Process arguments.</param>
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var viewModel = new ViewModel();
            viewModel.RunApp();
        }
    }
}
