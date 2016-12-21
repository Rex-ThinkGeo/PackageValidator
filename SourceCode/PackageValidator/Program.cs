using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Configuration;
using System.Collections.Generic;

namespace PackageValidator
{
    class Program
    {
        private static string input = ConfigurationManager.AppSettings["PackageSource"];
        private static List<string> ignoreList = ConfigurationManager.AppSettings["IgnoreList"].Split(',').ToList();
        private static string output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PVTemp");

        static void Main(string[] args)
        {
            if (Directory.Exists(output))
                Directory.Delete(output, true);
            Directory.CreateDirectory(output);

            Console.WriteLine("Start validate..");
            Console.WriteLine();
            string[] packages = Directory.GetFiles(input, "*.nupkg", SearchOption.AllDirectories);
            for (int i = 0; i < packages.Length; i++)
            {
                string packageName = Path.GetFileNameWithoutExtension(packages[i]);
                string tempFolder = Path.Combine(output, packageName);
                ZipArchive zip = ZipFile.Open(packages[i], ZipArchiveMode.Read);
                zip.ExtractToDirectory(tempFolder);

                string[] assemblies = Directory.GetFiles(tempFolder, "ThinkGeo.MapSuite*.dll", SearchOption.AllDirectories).Where(a =>
                {
                    if (!ignoreList.Any(d => (a.Contains(d))))
                        return true;
                    else
                        return false;
                }).ToArray();

                for (int j = 0; j < assemblies.Length; j++)
                {
                    Type type = Assembly.LoadFile(assemblies[j]).GetType("SecureTeam.Attributes.ObfuscatedByAgileDotNetAttribute", false, true);
                    if (type == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(packageName + " isn't enciphered.");
                        Console.ResetColor();
                    }
                }
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new String(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write($"\rChecking({i + 1}/{packages.Length}): {packageName} ...");
            }

            Console.WriteLine();
            Console.WriteLine("Complete..");
            Console.ReadLine();
        }
    }
}
