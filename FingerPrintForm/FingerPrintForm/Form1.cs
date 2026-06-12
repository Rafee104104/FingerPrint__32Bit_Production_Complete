using System;
using System.Collections.Generic;
#nullable disable
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FingerPrintForm;

public partial class Form1 : Form
{
    private const int AutoTimerIntervalMilliseconds = 60000;
    private static readonly Color ConnectReadyColor = SystemColors.ActiveCaption;
    private static readonly Color DisconnectReadyColor = Color.FromArgb(128, 255, 128);
    private static readonly Color ConnectionClickedColor = Color.Gray;
    private static readonly Color InsertReadyColor = Color.Lime;
    private static readonly Color AttendanceReadyColor = Color.Cyan;
    private static readonly Color ActionClickedColor = Color.Crimson;

    private dynamic objZkeeper = null;
    private bool bIsConnected = false;
    private int iMachineNumber = 1;
    private DataTable dtAttendance = new DataTable();
    private readonly object deviceLock = new object();
    private volatile bool insertRunning = false;
    private volatile bool attendanceLoadRunning = false;
    private bool autoInsertEnabled = false;
    private bool autoLoadEnabled = false;
    private int autoInsertRunCount = 0;
    private int autoLoadRunCount = 0;

    public Form1()
    {
        InitializeComponent();
        ApplyInitialConnectionButtonState();
        ApplyAutoButtonState();

        Timer1.Interval = AutoTimerIntervalMilliseconds;
        Timer1.Tick += Timer1_Tick;

        // If these events are already connected in Designer.cs, remove these lines to avoid duplicate execution.
        Load += Form1_Load;
        FormClosing += Form1_FormClosing;
        btnConnect.Click += btnConnect_Click;
        btnDisconnect.Click += btnDisconnect_Click;
        btnGetAttendance.Click += btnGetAttendance_Click;
        btnGetUsers.Click += btnGetUsers_Click;
        btnExport.Click += btnExport_Click;
        Button1.Click += Button1_Click;
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        InitializeDataTable();

        objZkeeper = CreateZkemObject();

        if (objZkeeper == null)
        {
            MessageBox.Show(
                "ZKTeco SDK লোড হয়নি!" + Environment.NewLine +
                @"regsvr32 C:\Windows\SysWOW64\zkemkeeper.dll",
                "SDK Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        else
        {
            lblStatus.Text = "Status: SDK Loaded - Ready to Connect";
            lblStatus.ForeColor = Color.Blue;
        }

        // btnConnect.PerformClick();
    }

    private dynamic CreateZkemObject()
    {
        string[] progIds =
        {
            "zkemkeeper.ZKEM.1",
            "zkemkeeper.ZKEM",
            "zkemkeeper.CZKEMClass",
            "zkemkeeper.CZKEM"
        };

        foreach (string progId in progIds)
        {
            try
            {
                Type comType = Type.GetTypeFromProgID(progId);
                if (comType != null)
                {
                    return Activator.CreateInstance(comType);
                }
            }
            catch
            {
                // Try next ProgID.
            }
        }

        return null;
    }

    private void InitializeDataTable()
    {
        EnsureAttendanceColumns(false);
        dgvAttendance.DataSource = dtAttendance;
    }

    private void EnsureAttendanceColumns(bool clearRows)
    {
        if (dtAttendance.Columns.Count != 7 ||
            !dtAttendance.Columns.Contains("User ID") ||
            !dtAttendance.Columns.Contains("Date") ||
            !dtAttendance.Columns.Contains("Time"))
        {
            dtAttendance.Clear();
            dtAttendance.Columns.Clear();
            dtAttendance.Columns.Add("User ID", typeof(string));
            dtAttendance.Columns.Add("Name", typeof(string));
            dtAttendance.Columns.Add("Date", typeof(string));
            dtAttendance.Columns.Add("Time", typeof(string));
            dtAttendance.Columns.Add("Verify Mode", typeof(string));
            dtAttendance.Columns.Add("In/Out", typeof(string));
            dtAttendance.Columns.Add("Work Code", typeof(string));
        }
        else if (clearRows)
        {
            dtAttendance.Clear();
        }
    }

    private bool TryGetDateRange(out DateTime fromDate, out DateTime toDate)
    {
        fromDate = dtpFromDate.Value.Date;
        toDate = dtpToDate.Value.Date;

        if (fromDate > toDate)
        {
            MessageBox.Show("From Date cannot be greater than To Date!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    private bool IsDateInRange(int year, int month, int day, DateTime fromDate, DateTime toDate)
    {
        if (year < fromDate.Year || year > toDate.Year) return false;
        if (year == fromDate.Year && month < fromDate.Month) return false;
        if (year == toDate.Year && month > toDate.Month) return false;
        if (year == fromDate.Year && month == fromDate.Month && day < fromDate.Day) return false;
        if (year == toDate.Year && month == toDate.Month && day > toDate.Day) return false;

        return true;
    }

    private string AutoIntervalText()
    {
        return (AutoTimerIntervalMilliseconds / 1000).ToString() + "s";
    }

    private bool IsAnyOperationRunning()
    {
        return insertRunning || attendanceLoadRunning;
    }

    private string ReadyStatusText()
    {
        if (autoInsertEnabled && autoLoadEnabled)
        {
            return "Status: Connected - Auto insert + load every " + AutoIntervalText();
        }

        if (autoInsertEnabled)
        {
            return "Status: Connected - Auto insert every " + AutoIntervalText();
        }

        if (autoLoadEnabled)
        {
            return "Status: Connected - Auto load every " + AutoIntervalText();
        }

        return "Status: Connected - Ready";
    }

    private string CurrentStatusText()
    {
        if (insertRunning && attendanceLoadRunning)
        {
            return "Status: Insert + load running...";
        }

        if (insertRunning)
        {
            return "Status: Inserting to DB...";
        }

        if (attendanceLoadRunning)
        {
            return "Status: Loading attendance...";
        }

        return ReadyStatusText();
    }

    private void RefreshStatus()
    {
        UpdateUI(() =>
        {
            bool running = IsAnyOperationRunning();
            lblStatus.Text = bIsConnected ? CurrentStatusText() : "Status: Disconnected";
            lblStatus.ForeColor = running ? Color.Orange : (bIsConnected ? Color.Green : Color.Gray);
            Cursor = running ? Cursors.WaitCursor : Cursors.Default;
        });
    }

    private void ApplyAutoButtonState()
    {
        Button1.Text = autoInsertEnabled ? "Stop Insert" : "Insert to DB";
        Button1.BackColor = autoInsertEnabled ? ActionClickedColor : InsertReadyColor;
        Button1.ForeColor = Color.Black;

        btnGetAttendance.Text = autoLoadEnabled ? "Stop Load" : "Load Attendance";
        btnGetAttendance.BackColor = autoLoadEnabled ? ActionClickedColor : AttendanceReadyColor;
        btnGetAttendance.ForeColor = Color.Black;
    }

    private void ApplyInitialConnectionButtonState()
    {
        btnConnect.BackColor = ConnectReadyColor;
        btnConnect.ForeColor = Color.Black;

        btnDisconnect.BackColor = ConnectionClickedColor;
        btnDisconnect.ForeColor = Color.Black;
    }

    private void ApplyConnectionButtonState(bool connected)
    {
        btnConnect.BackColor = connected ? ConnectionClickedColor : ConnectReadyColor;
        btnConnect.ForeColor = Color.Black;

        btnDisconnect.BackColor = connected ? DisconnectReadyColor : ConnectionClickedColor;
        btnDisconnect.ForeColor = Color.Black;
    }

    private void UpdateAutoButtonState()
    {
        UpdateUI(ApplyAutoButtonState);
    }

    private void UpdateRecordLabel(string text)
    {
        UpdateUI(() => lblRecordCount.Text = text);
    }

    private void UpdateAutoTimerState()
    {
        if (autoInsertEnabled || autoLoadEnabled)
        {
            Timer1.Interval = AutoTimerIntervalMilliseconds;
            if (!Timer1.Enabled)
            {
                Timer1.Start();
            }
        }
        else
        {
            Timer1.Stop();
        }

        UpdateAutoButtonState();
    }

    private void StartAutoInsertTimer()
    {
        autoInsertEnabled = true;
        autoInsertRunCount = 0;
        UpdateAutoTimerState();
    }

    private void StartAutoLoadTimer()
    {
        autoLoadEnabled = true;
        autoLoadRunCount = 0;
        UpdateAutoTimerState();
    }

    private void StopAutoInsertTimer(string message = null, Color? messageColor = null)
    {
        autoInsertEnabled = false;
        autoInsertRunCount = 0;
        UpdateAutoTimerState();

        if (!string.IsNullOrWhiteSpace(message))
        {
            AddLog(message, messageColor ?? Color.Orange);
        }
    }

    private void StopAutoLoadTimer(string message = null, Color? messageColor = null)
    {
        autoLoadEnabled = false;
        autoLoadRunCount = 0;
        UpdateAutoTimerState();

        if (!string.IsNullOrWhiteSpace(message))
        {
            AddLog(message, messageColor ?? Color.Orange);
        }
    }

    private void StopAllAutoTimers()
    {
        autoInsertEnabled = false;
        autoLoadEnabled = false;
        autoInsertRunCount = 0;
        autoLoadRunCount = 0;
        UpdateAutoTimerState();
    }

    // Thread-safe UI update helper
    private void UpdateUI(Action action)
    {
        if (InvokeRequired)
        {
            Invoke(action);
        }
        else
        {
            action();
        }
    }

    // Thread-safe log with color
    private void AddLog(string message, Color? logColor = null)
    {
        Color color = logColor ?? Color.White;
        string logLine = DateTime.Now.ToString("HH:mm:ss") + " - " + message + Environment.NewLine;

        UpdateUI(() =>
        {
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText(logLine);
            txtLog.SelectionColor = txtLog.ForeColor;
            txtLog.ScrollToCaret();
        });
    }

    // Set buttons enabled/disabled state
    private void SetButtonsEnabled(bool connected)
    {
        UpdateUI(() =>
        {
            bool anyProcessing = IsAnyOperationRunning();

            btnConnect.Enabled = !connected && !anyProcessing;
            btnDisconnect.Enabled = connected && !anyProcessing;
            btnGetAttendance.Enabled = connected && (!attendanceLoadRunning || autoLoadEnabled);
            btnGetUsers.Enabled = connected && !anyProcessing;
            Button1.Enabled = connected && (!insertRunning || autoInsertEnabled);
            btnExport.Enabled = connected && !anyProcessing;
            txtIP.Enabled = !connected && !anyProcessing;
            txtPort.Enabled = !connected && !anyProcessing;
            ApplyConnectionButtonState(connected);
            ApplyAutoButtonState();
        });
    }

    private void btnConnect_Click(object sender, EventArgs e)
    {
        try
        {
            if (objZkeeper == null)
            {
                MessageBox.Show("ZKTeco SDK not loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Cursor = Cursors.WaitCursor;
            string ip = txtIP.Text.Trim();
            int port = int.Parse(txtPort.Text.Trim());

            bIsConnected = objZkeeper.Connect_Net(ip, port);

            if (bIsConnected)
            {
                lblStatus.Text = "Status: Connected to " + ip;
                lblStatus.ForeColor = Color.Green;
                StopAllAutoTimers();
                SetButtonsEnabled(true);
                lock (deviceLock)
                {
                    objZkeeper.EnableDevice(iMachineNumber, false);
                }
                MessageBox.Show("Connected!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                int errorCode = 0;
                try
                {
                    objZkeeper.GetLastError(ref errorCode);
                }
                catch
                {
                    // Some COM bindings may not return error code properly through dynamic.
                }

                MessageBox.Show("Connection failed! Error: " + errorCode, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void btnDisconnect_Click(object sender, EventArgs e)
    {
        try
        {
            if (bIsConnected)
            {
                StopAllAutoTimers();
                lock (deviceLock)
                {
                    objZkeeper.EnableDevice(iMachineNumber, true);
                    objZkeeper.Disconnect();
                }
                bIsConnected = false;
                lblStatus.Text = "Status: Disconnected";
                lblStatus.ForeColor = Color.Gray;
                SetButtonsEnabled(false);
                MessageBox.Show("Disconnected!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Read attendance with date filter from the device only when the user clicks Insert to DB.
    private List<Tuple<string, DateTime>> ReadAttendanceFromDevice(DateTime fromDate, DateTime toDate)
    {
        List<Tuple<string, DateTime>> filteredList = new List<Tuple<string, DateTime>>();
        int totalRecords = 0;

        lock (deviceLock)
        {
            try
            {
                objZkeeper.EnableDevice(iMachineNumber, false);

                bool readSuccess = Convert.ToBoolean(objZkeeper.ReadGeneralLogData(iMachineNumber));
                if (readSuccess)
                {
                    AddLog("Using ReadGeneralLogData (full date range read)", Color.Yellow);
                }

                if (readSuccess)
                {
                    Type zkType = objZkeeper.GetType();

                    ParameterModifier[] modifiers = { new ParameterModifier(11) };
                    for (int i = 1; i <= 10; i++)
                    {
                        modifiers[0][i] = true;
                    }

                    while (true)
                    {
                        object[] args = { iMachineNumber, "", 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                        object result = zkType.InvokeMember(
                            "SSR_GetGeneralLogData",
                            BindingFlags.InvokeMethod,
                            null,
                            objZkeeper,
                            args,
                            modifiers,
                            null,
                            null);

                        if (!Convert.ToBoolean(result))
                        {
                            break;
                        }

                        totalRecords++;

                        int yr = Convert.ToInt32(args[4]);
                        int mo = Convert.ToInt32(args[5]);
                        int dy = Convert.ToInt32(args[6]);

                        if (totalRecords % 1000 == 0)
                        {
                            AddLog("Scanning... " + totalRecords + " done, " + filteredList.Count + " matched", Color.Gray);
                        }

                        if (!IsDateInRange(yr, mo, dy, fromDate, toDate)) continue;

                        string empId = args[1] != null ? args[1].ToString() : "";
                        DateTime checkTime = new DateTime(
                            yr,
                            mo,
                            dy,
                            Convert.ToInt32(args[7]),
                            Convert.ToInt32(args[8]),
                            Convert.ToInt32(args[9]));

                        filteredList.Add(Tuple.Create(empId, checkTime));
                        UpdateRecordLabel("Records: " + filteredList.Count + " | Scanned: " + totalRecords);
                    }
                }
            }
            finally
            {
                try
                {
                    objZkeeper.EnableDevice(iMachineNumber, true);
                }
                catch
                {
                    // Ignore device re-enable failures; the caller logs the main operation error.
                }
            }
        }

        UpdateRecordLabel("Records: " + filteredList.Count);
        AddLog("Device read done! Scanned: " + totalRecords + " | Matched: " + filteredList.Count, Color.Yellow);
        return filteredList;
    }

    private List<object[]> ReadAllAttendance(DateTime fromDate, DateTime toDate)
    {
        List<object[]> allRecords = new List<object[]>();
        int scannedCount = 0;

        lock (deviceLock)
        {
            try
            {
                objZkeeper.EnableDevice(iMachineNumber, false);

                if (Convert.ToBoolean(objZkeeper.ReadGeneralLogData(iMachineNumber)))
                {
                    Type zkType = objZkeeper.GetType();

                    ParameterModifier[] modifiers = { new ParameterModifier(11) };
                    for (int i = 1; i <= 10; i++)
                    {
                        modifiers[0][i] = true;
                    }

                    while (true)
                    {
                        object[] args = { iMachineNumber, "", 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                        object result = zkType.InvokeMember(
                            "SSR_GetGeneralLogData",
                            BindingFlags.InvokeMethod,
                            null,
                            objZkeeper,
                            args,
                            modifiers,
                            null,
                            null);

                        if (!Convert.ToBoolean(result))
                        {
                            break;
                        }

                        scannedCount++;

                        int yr = Convert.ToInt32(args[4]);
                        int mo = Convert.ToInt32(args[5]);
                        int dy = Convert.ToInt32(args[6]);

                        if (scannedCount % 1000 == 0)
                        {
                            AddLog("Reading... " + scannedCount + " scanned, " + allRecords.Count + " matched", Color.Gray);
                        }

                        if (!IsDateInRange(yr, mo, dy, fromDate, toDate)) continue;

                        allRecords.Add(new object[]
                        {
                            args[1], args[2], args[3], args[4], args[5],
                            args[6], args[7], args[8], args[9], args[10]
                        });

                        UpdateRecordLabel("Records: " + allRecords.Count + " | Scanned: " + scannedCount);
                    }
                }
            }
            finally
            {
                try
                {
                    objZkeeper.EnableDevice(iMachineNumber, true);
                }
                catch
                {
                    // Ignore device re-enable failures; the caller logs the main operation error.
                }
            }
        }

        UpdateRecordLabel("Records: " + allRecords.Count);
        AddLog("Attendance load done! Scanned: " + scannedCount + " | Matched: " + allRecords.Count, Color.Yellow);
        return allRecords;
    }

    private void PopulateAttendanceGrid(List<object[]> records, Dictionary<string, string> userNames)
    {
        EnsureAttendanceColumns(true);

        foreach (object[] rec in records.OrderByDescending(GetAttendanceTimestamp))
        {
            DataRow row = dtAttendance.NewRow();
            string eid = rec[0] != null ? rec[0].ToString() : "";

            row["User ID"] = eid;
            row["Name"] = userNames.ContainsKey(eid) ? userNames[eid] : "Unknown";
            row["Date"] = string.Format("{0:D4}-{1:D2}-{2:D2}", Convert.ToInt32(rec[3]), Convert.ToInt32(rec[4]), Convert.ToInt32(rec[5]));
            row["Time"] = string.Format("{0:D2}:{1:D2}:{2:D2}", Convert.ToInt32(rec[6]), Convert.ToInt32(rec[7]), Convert.ToInt32(rec[8]));
            row["Verify Mode"] = GetVerifyModeName(Convert.ToInt32(rec[1]));
            row["In/Out"] = GetInOutModeName(Convert.ToInt32(rec[2]));
            row["Work Code"] = rec[9] != null ? rec[9].ToString() : "";

            dtAttendance.Rows.Add(row);
        }

        lblRecordCount.Text = "Records: " + dtAttendance.Rows.Count;
    }

    private static DateTime GetAttendanceTimestamp(object[] record)
    {
        return new DateTime(
            Convert.ToInt32(record[3]),
            Convert.ToInt32(record[4]),
            Convert.ToInt32(record[5]),
            Convert.ToInt32(record[6]),
            Convert.ToInt32(record[7]),
            Convert.ToInt32(record[8]));
    }

    private bool LoadAttendanceForDateRange(Action<int> afterLoad = null, bool clearLog = true, bool scheduledRun = false)
    {
        if (!bIsConnected || attendanceLoadRunning) return false;
        if (!TryGetDateRange(out DateTime fromDate, out DateTime toDateValue))
        {
            if (scheduledRun)
            {
                StopAutoLoadTimer("Auto attendance load stopped because the date range is invalid.", Color.Red);
            }

            return false;
        }

        attendanceLoadRunning = true;
        SetButtonsEnabled(bIsConnected);

        UpdateUI(() =>
        {
            lblStatus.Text = CurrentStatusText();
            lblStatus.ForeColor = Color.Orange;
            if (clearLog && !insertRunning)
            {
                txtLog.Clear();
            }
            lblRecordCount.Text = "Records: 0";
            Cursor = Cursors.WaitCursor;
        });

        AddLog(scheduledRun ? "Auto load run #" + autoLoadRunCount + " started..." : "Reading attendance from device...", Color.Yellow);
        AddLog("Date: " + fromDate.ToString("yyyy-MM-dd") + " to " + toDateValue.ToString("yyyy-MM-dd"), Color.Yellow);

        Thread bgThread = new Thread(() =>
        {
            try
            {
                Dictionary<string, string> userNames = new Dictionary<string, string>();
                GetUserNames(userNames);
                List<object[]> allRecords = ReadAllAttendance(fromDate, toDateValue);

                UpdateUI(() =>
                {
                    PopulateAttendanceGrid(allRecords, userNames);
                    afterLoad?.Invoke(allRecords.Count);
                });

                AddLog("Total " + allRecords.Count + " records loaded!", Color.LimeGreen);
            }
            catch (Exception ex)
            {
                AddLog("Error: " + ex.Message, Color.Red);
            }
            finally
            {
                attendanceLoadRunning = false;
                RefreshStatus();
                SetButtonsEnabled(bIsConnected);
            }
        });

        bgThread.SetApartmentState(ApartmentState.STA);
        bgThread.IsBackground = true;
        bgThread.Start();
        return true;
    }

    private void btnGetAttendance_Click(object sender, EventArgs e)
    {
        if (autoLoadEnabled)
        {
            StopAutoLoadTimer("Auto attendance load stopped.");
            SetButtonsEnabled(bIsConnected);
            RefreshStatus();
            return;
        }

        if (!bIsConnected || attendanceLoadRunning) return;
        if (!TryGetDateRange(out _, out _)) return;

        StartAutoLoadTimer();
        if (LoadAttendanceForDateRange(null, true))
        {
            AddLog("Auto attendance load started. Interval: " + AutoIntervalText(), Color.Cyan);
        }
    }

    private void Timer1_Tick(object sender, EventArgs e)
    {
        if (!bIsConnected)
        {
            StopAllAutoTimers();
            return;
        }

        if (autoInsertEnabled && !insertRunning)
        {
            autoInsertRunCount++;
            StartInsertToDb(false, true);
        }

        if (autoLoadEnabled && !attendanceLoadRunning)
        {
            autoLoadRunCount++;
            LoadAttendanceForDateRange(null, false, true);
        }

        if (!autoInsertEnabled && !autoLoadEnabled)
        {
            Timer1.Stop();
        }
    }

    private void GetUserNames(Dictionary<string, string> userNames)
    {
        try
        {
            lock (deviceLock)
            {
                if (Convert.ToBoolean(objZkeeper.ReadAllUserID(iMachineNumber)))
                {
                    Type zkType = objZkeeper.GetType();

                    ParameterModifier[] modifiers = { new ParameterModifier(6) };
                    for (int i = 1; i <= 5; i++)
                    {
                        modifiers[0][i] = true;
                    }

                    while (true)
                    {
                        object[] args = { iMachineNumber, "", "", "", 0, false };

                        object result = zkType.InvokeMember(
                            "SSR_GetAllUserInfo",
                            BindingFlags.InvokeMethod,
                            null,
                            objZkeeper,
                            args,
                            modifiers,
                            null,
                            null);

                        if (!Convert.ToBoolean(result))
                        {
                            break;
                        }

                        string enrollNum = args[1] != null ? args[1].ToString() : "";
                        string name = args[2] != null ? args[2].ToString() : "";

                        if (!string.IsNullOrEmpty(enrollNum) && !userNames.ContainsKey(enrollNum))
                        {
                            userNames.Add(enrollNum, string.IsNullOrEmpty(name) ? "User " + enrollNum : name);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore user-name reading errors, same as original VB code.
        }
    }

    private void btnGetUsers_Click(object sender, EventArgs e)
    {
        try
        {
            if (!bIsConnected || IsAnyOperationRunning()) return;

            Cursor = Cursors.WaitCursor;
            dtAttendance.Clear();
            dtAttendance.Columns.Clear();
            dtAttendance.Columns.Add("User ID", typeof(string));
            dtAttendance.Columns.Add("Name", typeof(string));
            dtAttendance.Columns.Add("Privilege", typeof(string));
            dtAttendance.Columns.Add("Status", typeof(string));

            lock (deviceLock)
            {
                try
                {
                    objZkeeper.EnableDevice(iMachineNumber, false);

                    if (Convert.ToBoolean(objZkeeper.ReadAllUserID(iMachineNumber)))
                    {
                        Type zkType = objZkeeper.GetType();

                        ParameterModifier[] modifiers = { new ParameterModifier(6) };
                        for (int i = 1; i <= 5; i++)
                        {
                            modifiers[0][i] = true;
                        }

                        while (true)
                        {
                            object[] args = { iMachineNumber, "", "", "", 0, false };

                            object result = zkType.InvokeMember(
                                "SSR_GetAllUserInfo",
                                BindingFlags.InvokeMethod,
                                null,
                                objZkeeper,
                                args,
                                modifiers,
                                null,
                                null);

                            if (!Convert.ToBoolean(result))
                            {
                                break;
                            }

                            DataRow row = dtAttendance.NewRow();
                            row["User ID"] = args[1] != null ? args[1].ToString() : "";
                            row["Name"] = args[2] != null ? args[2].ToString() : "";
                            row["Privilege"] = GetPrivilegeName(Convert.ToInt32(args[4]));
                            row["Status"] = Convert.ToBoolean(args[5]) ? "Enabled" : "Disabled";
                            dtAttendance.Rows.Add(row);
                        }
                    }
                }
                finally
                {
                    objZkeeper.EnableDevice(iMachineNumber, true);
                }
            }

            lblRecordCount.Text = "Users: " + dtAttendance.Rows.Count;
            btnExport.Enabled = bIsConnected;
            MessageBox.Show("Total " + dtAttendance.Rows.Count + " users!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void btnExport_Click(object sender, EventArgs e)
    {
        LoadAttendanceForDateRange(loadedCount =>
        {
            if (loadedCount == 0)
            {
                MessageBox.Show("No attendance records found in selected date range.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ExportAttendanceToCsv();
        });
    }

    private void ExportAttendanceToCsv()
    {
        try
        {
            if (dtAttendance.Rows.Count == 0)
            {
                MessageBox.Show("No records to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = "Attendance_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter writer = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                {
                    List<string> headers = new List<string>();
                    foreach (DataColumn col in dtAttendance.Columns)
                    {
                        headers.Add(col.ColumnName);
                    }
                    writer.WriteLine(string.Join(",", headers));

                    foreach (DataRow row in dtAttendance.Rows)
                    {
                        List<string> values = new List<string>();
                        foreach (DataColumn col in dtAttendance.Columns)
                        {
                            string value = row[col]?.ToString() ?? "";
                            value = value.Replace("\"", "\"\"");
                            values.Add("\"" + value + "\"");
                        }
                        writer.WriteLine(string.Join(",", values));
                    }
                }

                MessageBox.Show("Exported!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string GetVerifyModeName(int mode)
    {
        switch (mode)
        {
            case 0: return "Password";
            case 1: return "Fingerprint";
            case 2: return "Card";
            case 15: return "Face";
            default: return mode.ToString();
        }
    }

    private string GetInOutModeName(int mode)
    {
        switch (mode)
        {
            case 0: return "Check-In";
            case 1: return "Check-Out";
            case 2: return "Break-Out";
            case 3: return "Break-In";
            case 4: return "OT-In";
            case 5: return "OT-Out";
            default: return mode.ToString();
        }
    }

    private string GetPrivilegeName(int privilege)
    {
        switch (privilege)
        {
            case 0: return "User";
            case 1: return "Enroller";
            case 2: return "Manager";
            case 3: return "Super Admin";
            default: return "Unknown";
        }
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        try
        {
            if (bIsConnected && objZkeeper != null)
            {
                StopAllAutoTimers();
                bool lockTaken = false;
                try
                {
                    Monitor.TryEnter(deviceLock, 5000, ref lockTaken);
                    if (lockTaken)
                    {
                        objZkeeper.EnableDevice(iMachineNumber, true);
                        objZkeeper.Disconnect();
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(deviceLock);
                    }
                }
            }
        }
        catch
        {
            // Ignore closing errors, same as original VB code.
        }
    }

    private void Button1_Click(object sender, EventArgs e)
    {
        if (autoInsertEnabled)
        {
            StopAutoInsertTimer("Auto insert stopped.");
            SetButtonsEnabled(bIsConnected);
            RefreshStatus();
            return;
        }

        if (!bIsConnected || insertRunning) return;
        if (!TryGetDateRange(out _, out _)) return;

        StartAutoInsertTimer();
        if (StartInsertToDb(true))
        {
            AddLog("Auto insert started. Interval: " + AutoIntervalText(), Color.Cyan);
        }
    }

    // Direct read from machine and insert to Oracle (non-blocking)
    private bool StartInsertToDb(bool clearLog, bool scheduledRun = false)
    {
        if (!bIsConnected || insertRunning) return false;

        if (!TryGetDateRange(out DateTime fromDate, out DateTime toDateValue))
        {
            if (scheduledRun)
            {
                StopAutoInsertTimer("Auto insert stopped because the date range is invalid.", Color.Red);
            }

            return false;
        }

        insertRunning = true;
        SetButtonsEnabled(bIsConnected);

        UpdateUI(() =>
        {
            if (clearLog && !attendanceLoadRunning)
            {
                txtLog.Clear();
            }

            lblStatus.Text = CurrentStatusText();
            lblStatus.ForeColor = Color.Orange;
            lblRecordCount.Text = "Records: 0";
            Cursor = Cursors.WaitCursor;
        });

        string logFilePath = Path.Combine(Application.StartupPath, "info.txt");

        AddLog(scheduledRun ? "========== AUTO INSERT RUN #" + autoInsertRunCount + " =========" : "========== INSERT FROM MACHINE =========", Color.Cyan);
        AddLog("Date: " + fromDate.ToString("yyyy-MM-dd") + " to " + toDateValue.ToString("yyyy-MM-dd"), Color.Yellow);

        Thread bgThread = new Thread(() =>
        {
            Oracle_database_helper oracleDb = new Oracle_database_helper();
            int insertedCount = 0;
            int duplicateCount = 0;
            int errorCount = 0;

            try
            {
                AddLog("Step 1: Reading selected date range from device...", Color.Yellow);
                List<Tuple<string, DateTime>> filteredList = ReadAttendanceFromDevice(fromDate, toDateValue);
                UpdateUI(() => lblRecordCount.Text = "Records: " + filteredList.Count);

                if (filteredList.Count == 0)
                {
                    AddLog("No records found in date range!", Color.Orange);
                    return;
                }

                // Step 2: Batch duplicate check - get all existing records in date range at once
                AddLog("Step 2: Checking duplicates & inserting...", Color.Yellow);

                // Load existing records from DB for this date range in ONE query
                HashSet<string> existingSet = new HashSet<string>();
                try
                {
                    oracleDb.AddParam(":fromDate", fromDate);
                    oracleDb.AddParam(":toDate", toDateValue.AddDays(1));
                    oracleDb.ExecQuery("SELECT USERID, CHECKTIME FROM ATT WHERE CHECKTIME >= :fromDate AND CHECKTIME < :toDate");

                    if (!oracleDb.HasException(false) && oracleDb.DBDT != null)
                    {
                        foreach (DataRow dbRow in oracleDb.DBDT.Rows)
                        {
                            string key = dbRow["USERID"].ToString() + "|" + Convert.ToDateTime(dbRow["CHECKTIME"]).ToString("yyyyMMddHHmmss");
                            existingSet.Add(key);
                        }
                    }

                    AddLog("Existing records in DB for date range: " + existingSet.Count, Color.Gray);
                }
                catch
                {
                    AddLog("Could not pre-load existing records, will check one by one", Color.Orange);
                }

                using (StreamWriter logWriter = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    logWriter.WriteLine("========================================");
                    logWriter.WriteLine("Insert: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    logWriter.WriteLine("Filter: " + fromDate.ToString("yyyy-MM-dd") + " to " + toDateValue.ToString("yyyy-MM-dd"));
                    logWriter.WriteLine("Filtered: " + filteredList.Count + " | Existing in DB: " + existingSet.Count);
                    logWriter.WriteLine("========================================");

                    for (int idx = 0; idx < filteredList.Count; idx++)
                    {
                        string empId = filteredList[idx].Item1;
                        DateTime checkTime = filteredList[idx].Item2;
                        int rowNum = idx + 1;

                        try
                        {
                            // Fast in-memory duplicate check
                            string key = empId + "|" + checkTime.ToString("yyyyMMddHHmmss");

                            if (existingSet.Contains(key))
                            {
                                duplicateCount++;
                                string dupMsg = "[" + rowNum + "] DUP - " + empId + " | " + checkTime.ToString("yyyy-MM-dd HH:mm:ss");
                                AddLog(dupMsg, Color.Orange);
                                logWriter.WriteLine(dupMsg);
                            }
                            else
                            {
                                // Insert
                                oracleDb.AddParam(":emp_id", empId);
                                oracleDb.AddParam(":checktime", checkTime);
                                oracleDb.AddParam(":checktime1", checkTime.ToString("M/d/yyyy h:mm:ss tt"));

                                oracleDb.ExecQuery("INSERT INTO ATT (USERID, CHECKTIME, CHECKTIME1) VALUES (:emp_id, :checktime, :checktime1)");

                                if (oracleDb.HasException(false))
                                {
                                    errorCount++;
                                    string errMsg = "[" + rowNum + "] ERR - " + empId + " | " + checkTime.ToString("yyyy-MM-dd HH:mm:ss") + " | " + oracleDb.Exception;
                                    AddLog(errMsg, Color.Red);
                                    logWriter.WriteLine(errMsg);
                                }
                                else
                                {
                                    insertedCount++;
                                    existingSet.Add(key); // Add to set so next same record is caught as dup
                                    string okMsg = "[" + rowNum + "] OK - " + empId + " | " + checkTime.ToString("yyyy-MM-dd HH:mm:ss");
                                    AddLog(okMsg, Color.LimeGreen);
                                    logWriter.WriteLine(okMsg);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            AddLog("[" + rowNum + "] EX - " + ex.Message, Color.Red);
                            logWriter.WriteLine("[" + rowNum + "] EX - " + ex.Message);
                        }

                        UpdateRecordLabel("Records: " + rowNum + "/" + filteredList.Count + " | Ins: " + insertedCount);
                    }

                    logWriter.WriteLine("Ins=" + insertedCount + " Dup=" + duplicateCount + " Err=" + errorCount);
                    logWriter.WriteLine("========================================");
                }

                AddLog("", Color.White);
                AddLog("========== DONE =========", Color.Cyan);
                AddLog("Matched: " + filteredList.Count + " | Ins: " + insertedCount + " | Dup: " + duplicateCount + " | Err: " + errorCount, Color.White);
                UpdateRecordLabel("Records: " + filteredList.Count + " | Ins: " + insertedCount);
            }
            catch (Exception ex)
            {
                AddLog("FATAL: " + ex.Message, Color.Red);
            }
            finally
            {
                insertRunning = false;
                RefreshStatus();
                SetButtonsEnabled(bIsConnected);
            }
        });

        bgThread.SetApartmentState(ApartmentState.STA);
        bgThread.IsBackground = true;
        bgThread.Start();
        return true;
    }
}
