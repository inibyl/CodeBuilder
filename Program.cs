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
            CompilerResults results = compiler.CompileAssemblyFromSource(options, ObfuscateCode(CsharpSourceCode));
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

        // получение рандомных буковок   / by XpucT
        static string CreateNewGuid()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        }

        // мини обфускация кода, переименование переменных и классов с записью в словарь
        static string ObfuscateCode(string code)
        {
            try
            {
                // namespace
                Match class_name = Regex.Match(code, @"class \S*");
                if (class_name.Groups.Count > 0)
                {
                    foreach (Match match in class_name.Groups)
                    {
                        string name = match.ToString().Replace("class ", "").Replace("\n", "");
                        string hash = CreateNewGuid();
                        keyValues.Add(name, hash);
                        code = code.Replace(name, hash);
                    }
                }
            }
            catch { }

            try
            {
                // string_values
                Match string_name = Regex.Match(code, @"string \S* = ");
                if (string_name.Groups.Count > 0)
                {
                    foreach (Match match in string_name.Groups)
                    {
                        string name = match.ToString().Replace("\n", "").Replace(" = ", "").Replace("string ", "");
                        string hash = CreateNewGuid();
                        keyValues.Add(name, hash);
                        code = code.Replace(name, hash);
                    }
                }
            }
            catch { }

            try
            {
                // int_values
                Match int_name = Regex.Match(code, @"int \S* = ");
                if (int_name.Groups.Count > 0)
                {
                    foreach (Match match in int_name.Groups)
                    {
                        string name = match.ToString().Replace("\n", "").Replace(" = ", "").Replace("int ", "");
                        string hash = CreateNewGuid();
                        keyValues.Add(name, hash);
                        code = code.Replace(name, hash);
                    }
                }
            }
            catch { }

            try
            {
                // void_values
                Match void_name = Regex.Match(code, @"void \S*");
                if (void_name.Groups.Count > 0)
                {
                    foreach (Match match in void_name.Groups)
                    {
                        string name = match.ToString().Replace("\n", "").Replace("void ", "").Replace("()", "");
                        string hash = CreateNewGuid();
                        keyValues.Add(name, hash);
                        code = code.Replace(name, hash);
                    }
                }
            }
            catch { }

            // result
            Console.WriteLine(code);
            return code;
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
