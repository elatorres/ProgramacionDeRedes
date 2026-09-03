// File: Program.cs
using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Enter a directory path to list files (or 'exit' to quit):");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "exit") break;

            if (Directory.Exists(input))
            {
                Console.WriteLine($"Listing files in: {input}");
                try
                {
                    var files = Directory.GetFiles(input, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                        Console.WriteLine(file);
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    Console.ResetColor();
                    Console.WriteLine("Could not scan directory {0}", input);

                }
            }
            else
            {
                Console.WriteLine("Directory does not exist.");
            }
        }
    }
}