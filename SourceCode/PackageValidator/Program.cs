using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Configuration;
using System.Collections.Generic;
using System.Text;

namespace PackageValidator
{
    class Program
    {
        private static string input = ConfigurationManager.AppSettings["PackageSource"];
        private static List<string> ignoreList = ConfigurationManager.AppSettings["IgnoreList"].Split(',').ToList();
        private static string output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PVTemp");
        private static StringBuilder log = new StringBuilder();

        static void Main(string[] args)
        {
            if (Directory.Exists(output))
                Directory.Delete(output, true);
            Directory.CreateDirectory(output);

            Console.WriteLine("Start validate..");
            log.AppendLine("Start validate..");
            Console.WriteLine();
            string[] packages = Directory.GetFiles(input, "*.nupkg", SearchOption.AllDirectories);
            for (int i = 0; i < packages.Length; i++)
            {
                string packageName = Path.GetFileNameWithoutExtension(packages[i]);
                string tempFolder = Path.Combine(output, packageName);
                ZipArchive zip = ZipFile.Open(packages[i], ZipArchiveMode.Read);
                zip.ExtractToDirectory(tempFolder);

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new String(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write($"\rChecking({i + 1}/{packages.Length}): {packageName} ...");
                log.AppendLine($"Checking({i + 1}/{packages.Length}): {packageName} ...");
                //CheckPackageEnciphering(tempFolder, packageName);

                CheckStrongName(tempFolder, packageName);
            }

            Console.WriteLine();
            Console.WriteLine("Complete..");
            log.AppendLine("Complete..");
            File.WriteAllText("log.txt", log.ToString());
            Console.ReadLine();
        }

        private static void CheckPackageEnciphering(string sourveFolder, string packageName)
        {
            string[] assemblies = Directory.GetFiles(sourveFolder, "ThinkGeo.MapSuite*.dll", SearchOption.AllDirectories).Where(a =>
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
        }

        private static void CheckStrongName(string sourveFolder, string packageName)
        {
            string[] assemblies = Directory.GetFiles(sourveFolder, "*.dll", SearchOption.AllDirectories);

            for (int j = 0; j < assemblies.Length; j++)
            {
                try
                {
                    byte[] token = Assembly.LoadFile(assemblies[j]).GetName().GetPublicKeyToken();
                    if (token.Length > 0)
                        continue;

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"    {Path.GetFileName(assemblies[j])}: isn't strang name dll");
                    log.AppendLine($"      {Path.GetFileName(assemblies[j])}: isn't strang name dll");
                    Console.ResetColor();
                }
                catch (Exception e)
                {
                    //Console.WriteLine();
                    //Console.ForegroundColor = ConsoleColor.Yellow;
                    //Console.WriteLine($"{Path.GetFileName(assemblies[j])}: {e.Message}");
                    //log.AppendLine($"{Path.GetFileName(assemblies[j])}: {e.Message}");
                    //Console.ResetColor();
                }
            }
        }
    }
}
