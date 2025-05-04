using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BypassReplacer
{
    public partial class MainWindow : Window
    {
        [Flags]
        public enum ThreadAccess
        {
            TERMINATE = 1,
            SUSPEND_RESUME = 2,
            GET_CONTEXT = 8,
            SET_CONTEXT = 0x10,
            SET_INFORMATION = 0x20,
            QUERY_INFORMATION = 0x40,
            SET_THREAD_TOKEN = 0x80,
            IMPERSONATE = 0x100,
            DIRECT_IMPERSONATION = 0x200,
            THREAD_ALL_ACCESS = 0x1F03FF
        }

        [DllImport("kernel32.dll")] private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")] private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")] private static extern int ResumeThread(IntPtr hThread);
        
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);

        public MainWindow()
        {
            InitializeComponent();
            this.title.Text = "Обход by drazz & NANO & Serega007";

            Task.Run(() =>
            {

                Process currProcess = null;
                List<Process> multiProcess = new List<Process>();
                string filePath = "";
                string filePathMinecraft = "";
                string replacePath = "\\Minigames\\libraries\\";
                string replaceName = "feder-live-SNAPSHOT.jar";

                if (!File.Exists("C:\\Cristalix\\" + replaceName))
                {
                    Dispatcher.Invoke(() => inform.Text = "Ошибка: Не найден файл " + "C:\\Cristalix\\" + replaceName + "\n\nПерезапустите BypassReplacer для повторной попытки");
                    return;
                }

                Console.WriteLine("Запустите Cristalix...");
                Dispatcher.Invoke(() => inform.Text = "Запустите Cristalix...");

                while (true)
                {
                    try
                    {
                        double currTime = new TimeSpan(DateTime.Now.Ticks).TotalSeconds;
                        multiProcess.Clear();
                        Process[] process = Process.GetProcessesByName("java");
                        foreach (Process p in process)
                        {
                            // TODO Serega007 сомнительная фигня с HandleCount, но это сделано так как Cristalix при закрытии майнкрафта какого-то хрена на время создаёт ещё один процесс java.exe (который ничего не делает?)
                            if (p.HandleCount == 0) continue;

                            string path = p.MainModule.FileName;

                            // TODO лучше это смотреть в CommandLine но сделать это без WMI весьма сложно и не надёжно
                            if (!path.Contains("24-jre-win-64\\bin")) continue;

                            //string cmdLine = GetCommandLine(p);
                            //if (!cmdLine.Contains("ru.cristalix")) continue;

                            string pathSunEc = path.Substring(0, path.IndexOf("\\updates\\")) + "\\updates" + replacePath + replaceName;

                            if (!File.Exists(pathSunEc)) continue;
                            if (currTime - new TimeSpan(p.StartTime.Ticks).TotalSeconds > 3)
                            {
                                multiProcess.Add(p);
                                continue;
                            }

                            Console.WriteLine("Cristalix найден! ждёмс чудо...");
                            Dispatcher.Invoke(() => inform.Text = "Cristalix найден! ждёмс чудо...");
                            filePath = pathSunEc;
                            filePathMinecraft = path.Substring(0, path.IndexOf("\\updates\\")) + "\\updates\\Minigames\\minecraft.jar";
                            currProcess = p;
                        }
                        if (currProcess != null) break;
                        Thread.Sleep(50);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        Dispatcher.Invoke(() => inform.Text = $"Ошибка: {ex.GetType()} {ex.Message}" + "\nПерезапустите BypassReplacer для повторной попытки");
                        break;
                    }
                }
                if (currProcess == null) return;
                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sunec_temp");
                string tempFile = System.IO.Path.Combine(tempPath, replaceName);

                Console.WriteLine("Проверка на вшивость...");
                Dispatcher.Invoke(() => inform.Text = "Проверка на вшивость...");

                if (!File.Exists("C:\\Xenoceal\\" + replaceName))
                {
                    Dispatcher.Invoke(() => inform.Text = "Ошибка: Не найден файл " + "C:\\Xenoceal\\" + replaceName + "\n\nПерезапустите BypassReplacer для повторной попытки");
                    return;
                }
                if (IsSymbolicLink(filePath))
                {
                    File.Delete(filePath);
                    Dispatcher.Invoke(() => inform.Text = "Ошибка: Похоже предыдущая попытка подмена была не успешной, перезапустите лаунчер (кристаликс) без BypassReplacer и попробуйте снова\n\nПерезапустите BypassReplacer для повторной попытки");
                    return;
                }

                if (IsFileEqual("C:\\Xenoceal\\" + replaceName, filePath))
                {
                    Dispatcher.Invoke(() => inform.Text = "Ошибка: Похоже файл " + "C:\\Xenoceal\\" + replaceName + " был перезаписан лаунчером, верните модифиваронный " + replaceName + "\n\nПерезапустите BypassReplacer для повторной попытки");
                    return;
                }

                try
                {
                    Directory.CreateDirectory(tempPath);
                    File.Copy(filePath, tempFile, overwrite: true);

                    Console.WriteLine("Подмена 1...");
                    Dispatcher.Invoke(() => inform.Text = "Подмена 1...");
                    JavaProcess(currProcess, false);
                    if (multiProcess.Count > 0)
                    {
                        Dispatcher.Invoke(() => inform.Text += "\n\nОбнаружено что запущено несколько майнкрафтов (кристаликса), они будут заморожены на момент инжекта во избежания краша");
                    }
                    foreach (Process p in multiProcess)
                    {
                        p.Refresh();
                        if (!p.HasExited)
                        {
                            JavaProcess(p, false);
                        }
                    }
                    File.Delete(filePath);
                    File.Copy("C:\\Xenoceal\\" + replaceName, filePath, overwrite: true);
                    JavaProcess(currProcess, true);

                    Console.WriteLine("Ждём запуска майнкрафта...");
                    Dispatcher.Invoke(() => inform.Text = "Ждём запуска майнкрафта...");
                    if (multiProcess.Count > 0)
                    {
                        Console.WriteLine("Обнаружено что запущено несколько майнкрафтов (кристаликса), они будут заморожены на момент инжекта во избежания краша");
                        Dispatcher.Invoke(() => inform.Text += "\n\nОбнаружено что запущено несколько майнкрафтов (кристаликса), они будут заморожены на момент инжекта во избежания краша");
                    }
                    while (true)
                    {
                        List<Process> list = FileUtil.WhoIsLocking(filePathMinecraft);
                        if (list.Count > 0)
                        {
                            bool found = false;
                            foreach (Process p in list)
                            {
                                if (p.Id == currProcess.Id)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                            {
                                break;
                            }
                        }
                        Thread.Sleep(50);
                    }

                    Console.WriteLine("Подмена 2...");
                    Dispatcher.Invoke(() => inform.Text = "Подмена 2...");
                    JavaProcess(currProcess, false);
                    if (multiProcess.Count > 0)
                    {
                        Console.WriteLine("Обнаружено что запущено несколько майнкрафтов (кристаликса), они будут заморожены на момент инжекта во избежания краша");
                        Dispatcher.Invoke(() => inform.Text += "\n\nОбнаружено что запущено несколько майнкрафтов (кристаликса), они будут заморожены на момент инжекта во избежания краша");
                    }
                    File.Delete(filePath);
                    File.Copy(tempFile, filePath, overwrite: true);
                    //File.Copy(tempFile, filePath, overwrite: true);
                    JavaProcess(currProcess, true);
                    foreach (Process p in multiProcess)
                    {
                        p.Refresh();
                        if (!p.HasExited)
                        {
                            JavaProcess(p, true);
                        }
                    }
                    Console.WriteLine("Вроде всё прошло успешно, проверяйте");
                    Dispatcher.Invoke(() => inform.Text = "Вроде всё прошло успешно, проверяйте");
                    Thread.Sleep(2000);

                    Dispatcher.Invoke(() => this.Close());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Dispatcher.Invoke(() => inform.Text = $"Ошибка: {ex.GetType()} {ex.Message}\nПерезапустите BypassReplacer для повторной попытки");
                    try
                    {
                        foreach (Process p in multiProcess)
                        {
                            p.Refresh();
                            if (!p.HasExited)
                            {
                                JavaProcess(p, true);
                            }
                        }
                    }
                    catch { }
                }
                finally
                {
                    Directory.Delete(tempPath, recursive: true);
                }
            });
        }

        private void TextBoxPreview(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            string text = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            Regex regex = new Regex("^[0-9-]+$");
            if (!regex.IsMatch(text))
            {
                e.Handled = true;
            }
        }

        static bool IsSymbolicLink(string path)
        {
            FileInfo file = new FileInfo(path);
            return file.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }

        static bool IsFileEqual(string filepath1, string filepath2)
        {
            using (var reader1 = new System.IO.FileStream(filepath1, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                using (var reader2 = new System.IO.FileStream(filepath2, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    byte[] hash1;
                    byte[] hash2;

                    using (var md51 = new System.Security.Cryptography.MD5CryptoServiceProvider())
                    {
                        md51.ComputeHash(reader1);
                        hash1 = md51.Hash;
                    }

                    using (var md52 = new System.Security.Cryptography.MD5CryptoServiceProvider())
                    {
                        md52.ComputeHash(reader2);
                        hash2 = md52.Hash;
                    }

                    int j = 0;
                    for (j = 0; j < hash1.Length; j++)
                    {
                        if (hash1[j] != hash2[j])
                        {
                            break;
                        }
                    }

                    return j == hash1.Length;
                }
            }
        }

        private void JavaProcess(Process process, bool active)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                IntPtr intPtr = OpenThread(ThreadAccess.SUSPEND_RESUME, bInheritHandle: false, (uint)thread.Id);
                if (intPtr != IntPtr.Zero)
                {
                    if (active) ResumeThread(intPtr);
                    else SuspendThread(intPtr);
                    CloseHandle(intPtr);
                }
            }
        }

    }
}
