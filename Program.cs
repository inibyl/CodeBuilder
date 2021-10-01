using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace protector
{
    class Program
    {
        // словарь замененных имен для того, чтобы не потерять нужные методы при запуске
        static Dictionary<string, string> keyValues = new Dictionary<string, string>();

        // наш код, который можно получить с сервера
        static string CsharpSourceCode = @"
using System.IO;

public class Program
{
    public static void Test()
    {
        string name = ""Hello!"";
        File.Delete(@""C:\Users\Александр\Desktop\default\1"");
    }
}";
        static void Main(string[] args)
        {
            // создаем новый компилятор
            CSharpCodeProvider myCodeProvider = new CSharpCodeProvider();
            ICodeCompiler compiler = myCodeProvider.CreateCompiler();

            // задаем ему параметры: не исполняемый файл, сборка в оперативке, не нужен debug
            CompilerParameters options = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                IncludeDebugInformation = false
            };

            // компилируем нашу сборочку, перед этим пропуская через наш мини-обфускатор
            CompilerResults results = compiler.CompileAssemblyFromSource(options, CsharpSourceCode);
            foreach (CompilerError error in results.Errors) { Console.WriteLine(error.ErrorText); }


            // теперь запустим то, что получилось
            try
            {
                // загружаем нашу сборку
                Assembly asm = results.CompiledAssembly;
                Type t = asm.GetType(keyValues["Program"], true, true);

                // создаем экземпляр класса Program
                object obj = Activator.CreateInstance(t);

                // получаем метод GetResult
                MethodInfo method = t.GetMethod(keyValues["Test"]);

                // вызываем метод
                object result = method.Invoke(obj, null);
                Console.WriteLine((result));

                // чистим мусор, который был создан в процессе работы (там пару папочек с рандомным названием)
                ClearTemp();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            // Успешно!
            Console.ReadLine();

        }

        // классный метод очиски папки temp
        static void ClearTemp()
        {
            string tempPath = Path.GetTempPath();
            foreach (var dirPath in Directory.GetDirectories(tempPath))
            {
                try
                {
                    Directory.Delete(dirPath, recursive: true);
                }
                catch { }
            }

            foreach (var filePath in Directory.EnumerateFiles(tempPath, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(filePath, File.GetAttributes(filePath) & ~FileAttributes.ReadOnly);
                    File.Delete(filePath);
                }
                catch { }
            }

            foreach (var dirPath in Directory.GetDirectories(tempPath, "*.*", SearchOption.AllDirectories).OrderByDescending(path => path))
            {
                try
                {
                    Directory.Delete(dirPath);
                }
                catch { }
            }
        }

    }
}
