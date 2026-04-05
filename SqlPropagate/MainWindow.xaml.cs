using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.Configuration;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Runtime;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.Sql.Propagate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Default 500ms
        int AutoDelay = 500;

        public MainWindow()
        {
            ControlsHelper.InitInvokeContext();
            _Arguments = new Arguments(Environment.GetCommandLineArgs());
            var autoDelayString = _Arguments.GetValue(nameof(AutoDelay), true);
            AutoDelay = RuntimeHelper.TryParse<int>(autoDelayString, AutoDelay);
            // Use configuration from exe folder.
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var exeDir = Path.GetDirectoryName(exePath);
            var baseName = Path.GetFileNameWithoutExtension(exePath);
            var xmlPath = Path.Combine(exeDir, baseName + ".xml");
            Global.AppData.XmlFile = new FileInfo(xmlPath);
            if (!File.Exists(xmlPath))
            {
                // Extract default config and scripts from embedded resources before first load.
                ExtractEmbeddedResource("Data.JocysCom.Sql.Propagate.xml", baseName + ".xml");
            }
            Global.AppData.Load();
            if (Global.AppData.Items.Count == 0)
            {
                Global.AppData.Items.Add(new AppData());
                Global.AppData.Save();
            }
            // Always extract demo scripts (won't overwrite existing).
            ExtractEmbeddedResource("Data.Script1.sql", Path.Combine(baseName, "Script1.sql"));
            ExtractEmbeddedResource("Data.Script2.sql", Path.Combine(baseName, "Script2.sql"));
            // Load parameters.
            foreach (var item in Global.AppSettings.Parameters)
            {
                var key = item.Name.Replace("$(", "").Replace(")", "");
                if (_Arguments.ContainsKey(key))
                    item.Value = _Arguments.GetValue(key);
            }
            InitializeComponent();
            var assembly = Assembly.GetExecutingAssembly();
            var ai = new ClassLibrary.Configuration.AssemblyInfo();
            Title = ai.GetTitle(true, false, true, false, false);
            LoadHelpAndInfo(true);
            // Initialize other things.
            AddFileDialog = new OpenFileDialog();
            ConnectionsPanel.SetDataItems(Global.AppSettings.Connections);
            ConnectionsPanel.MainDataGrid.IsReadOnly = false;
            ConnectionsPanel.AddButton.Click += ConnectionsPanel_AddButton_Click;
            ConnectionsPanel.EditButton.Click += ConnectionsPanel_EditButton_Click;
            ParametersPanel.SetDataItems(Global.AppSettings.Parameters);
            ParametersPanel.MainDataGrid.IsReadOnly = false;
            ParametersPanel.AddButton.Click += ParametersPanel_AddButton_Click;
            ScriptsPanel.SetDataItems(Global.AppSettings.Scripts);
            ScriptsPanel.AddButton.Click += ScriptsPanel_AddButton_Click;
            ScriptsPanel.EditButton.Click += ScriptsPanel_EditButton_Click;
            ScriptsPanel.ExecuteButton.Click += ScriptPanel_ExecuteButton_Click;
            ScriptsPanel.MainDataGrid.MouseDoubleClick += ScriptsPanel_MainDataGrid_MouseDoubleClick;
            ScriptsPanel.MainDataGrid.IsReadOnly = false;
            ControlsHelper.EnableAutoScroll(LogPanel.LogTextBox);
        }

        Arguments _Arguments;

        /// <summary>
        /// Extract an embedded resource to a path relative to the exe directory.
        /// Does not overwrite existing files.
        /// </summary>
        void ExtractEmbeddedResource(string resourceName, string relativePath)
        {
            var exeDir = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            var targetPath = Path.Combine(exeDir, relativePath);
            if (File.Exists(targetPath))
                return;
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);
            var content = ClassLibrary.Helper.FindResource<byte[]>(resourceName, Assembly.GetExecutingAssembly());
            if (content != null)
                File.WriteAllBytes(targetPath, content);
        }

        private void ScriptsPanel_MainDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ScriptTabItem.Visibility = Visibility.Visible;
            var scriptItem = (DataItem)ScriptsPanel.MainDataGrid.SelectedItem;
            if (scriptItem == null)
                return;
            var column = (e.Source as DataGrid)?.CurrentColumn;
            if (column == ScriptsPanel.ValueColumn)
            {
                var parameters = ParametersPanel.GetCheckedOrSelectedReferences(out bool containsChecked);
                ScriptTextBox.Text = ScriptExecutor.ApplyParameters(scriptItem, parameters);
                //ScriptTabItem.Visibility = Visibility.Visible;
                Dispatcher.BeginInvoke((Action)(() => MainTabControl.SelectedIndex = 1));
            }
        }

        #region Connections Panel

        private void ConnectionsPanel_AddButton_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new DataItem();
            newItem.IsEnabled = true;
            newItem.IsChecked = true;
            var isOK = UpdateConnectionItem(newItem);
            if (isOK)
            {
                Global.AppSettings.Connections.Add(newItem);
                newItem.Order = Global.AppSettings.Connections.IndexOf(newItem);
            }
        }

        private void ConnectionsPanel_EditButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (DataItem)ConnectionsPanel.MainDataGrid.SelectedItem;
            if (item == null)
                return;
            UpdateConnectionItem(item);
        }

        /// <summary>
        /// Reformat connection string property names with spaces to their non-spaced equivalents
        /// so that the System.Data-based ConnectionUI dialog can parse them.
        /// </summary>
        static string ReformatConnectionStringForDialog(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;
            var s = connectionString;
            s = s.Replace("Application Intent", "ApplicationIntent");
            s = s.Replace("Connect Retry Count", "ConnectRetryCount");
            s = s.Replace("Connect Retry Interval", "ConnectRetryInterval");
            s = s.Replace("Pool Blocking Period", "PoolBlockingPeriod");
            s = s.Replace("Multiple Active Result Sets", "MultipleActiveResultSets");
            s = s.Replace("Multiple Subnet Failover", "MultiSubnetFailover");
            s = s.Replace("Transparent Network IP Resolution", "TransparentNetworkIPResolution");
            s = s.Replace("Trust Server Certificate", "TrustServerCertificate");
            return s;
        }

        bool UpdateConnectionItem(DataItem item)
        {
            // Register Microsoft.Data.SqlClient provider so the dialog can use it.
            if (!System.Data.Common.DbProviderFactories.GetProviderInvariantNames().Contains("Microsoft.Data.SqlClient"))
                System.Data.Common.DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", Microsoft.Data.SqlClient.SqlClientFactory.Instance);
            var dcd = new Microsoft.SqlServer.Management.ConnectionUI.DataConnectionDialog();
            dcd.DataSources.Add(Microsoft.SqlServer.Management.ConnectionUI.DataSource.SqlDataSource);
            dcd.SelectedDataSource = Microsoft.SqlServer.Management.ConnectionUI.DataSource.SqlDataSource;
            dcd.SelectedDataProvider = Microsoft.SqlServer.Management.ConnectionUI.DataProvider.SqlDataProvider;
            try { dcd.ConnectionString = ReformatConnectionStringForDialog(item.Value); }
            catch { /* ignore parse errors from new properties */ }
            Microsoft.SqlServer.Management.ConnectionUI.DataConnectionDialog.Show(dcd);
            var isOK = dcd.DialogResult == System.Windows.Forms.DialogResult.OK;
            if (isOK)
            {
                item.Value = dcd.ConnectionString;
                bool isEntity;
                var cs = item.Name = ClassLibrary.Data.SqlHelper.GetProviderConnectionString(item.Value, out isEntity);
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(cs);
                item.Name = $"{builder.DataSource}, {builder.InitialCatalog}".Trim(' ', ',');
                Global.AppData.Save();
            }
            return isOK;
        }

        #endregion

        #region Parameters Panel

        private void ParametersPanel_AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddParameter(Global.AppSettings.Parameters);
        }

        void AddParameter(IList<DataItem> list, string name = "", string value = "", bool isChecked = true)
        {
            var newItem = new DataItem();
            newItem.Name = name;
            newItem.Value = value;
            newItem.IsChecked = isChecked;
            newItem.IsEnabled = true;
            list.Add(newItem);
            newItem.Order = list.IndexOf(newItem);
        }

        #endregion

        #region Scripts Panel

        OpenFileDialog AddFileDialog;

        private void ScriptsPanel_AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddScriptItems();
        }

        private void ScriptsPanel_EditButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (DataItem)ScriptsPanel.MainDataGrid.SelectedItem;
            if (item == null)
                return;
            AddScriptItems(item);
        }

        void AddScriptItems(DataItem item = null)
        {
            var dialog = AddFileDialog;
            dialog.Multiselect = true;
            dialog.Filter = "SQL Script (*.sql)|*.sql|All files (*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            var currentPath = new DirectoryInfo(".").FullName;
            var initialDirectory = currentPath;
            if (!string.IsNullOrEmpty(item?.Value))
            {
                var fi = new FileInfo(item?.Value);
                if (string.IsNullOrEmpty(dialog.FileName))
                    dialog.FileName = System.IO.Path.GetFileNameWithoutExtension(fi.Name);
                initialDirectory = fi.Directory.FullName;
            }
            if (string.IsNullOrEmpty(dialog.InitialDirectory))
                dialog.InitialDirectory = initialDirectory;
            dialog.Title = "Import Settings File";
            var result = dialog.ShowDialog();
            if (result != true)
                return;
            foreach (var path in dialog.FileNames)
            {
                var newItem = new DataItem()
                {
                    Name = System.IO.Path.GetFileName(path),
                    Value = JocysCom.ClassLibrary.IO.PathHelper.GetRelativePath(currentPath + "\\", path),
                    IsEnabled = true,
                };
                Global.AppSettings.Scripts.Add(newItem);
                newItem.Order = Global.AppSettings.Scripts.IndexOf(newItem);
            }
        }

        #endregion

        private void ScriptPanel_ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_IsExecuting)
                CancelExecution();
            else
                Execute();
        }

        Controls.DataListControl _TaskControl;
        bool _IsExecuting;
        System.Threading.CancellationTokenSource _Cts;

        void SetExecutingState(bool executing)
        {
            _IsExecuting = executing;
            ControlsHelper.Invoke(() =>
            {
                var label = ScriptsPanel.ExecuteButton.Content as System.Windows.Controls.StackPanel;
                if (label != null)
                {
                    var lbl = label.Children[1] as System.Windows.Controls.Label;
                    if (lbl != null)
                        lbl.Content = executing ? "Stop" : "Execute...";
                    var icon = label.Children[0] as System.Windows.Controls.ContentControl;
                    if (icon != null)
                        icon.Content = executing
                            ? FindResource("Icon_Cancel")
                            : FindResource("Icon_Play");
                }
            });
        }

        void CancelExecution()
        {
            _Cts?.Cancel();
        }

        void Execute(bool skipConfirmation = false)
        {
            List<DataItem> scripts = null;
            List<DataItem> connections = null;
            List<DataItem> parameters = null;
            bool containsChecked;
            connections = ConnectionsPanel.GetCheckedOrSelectedReferences(out containsChecked);
            scripts = ScriptsPanel.GetCheckedOrSelectedReferences(out containsChecked);
            parameters = ParametersPanel.GetCheckedOrSelectedReferences(out containsChecked);
            var form = new MessageBoxWindow();
            if (connections.Count == 0)
            {
                form.ShowDialog($"Please check at least one connection", "Execute", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (scripts.Count == 0)
            {
                form.ShowDialog($"Please check at least one script", "Execute", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (string.IsNullOrEmpty(parameter.Name))
                {
                    form.ShowDialog($"Parameter Name can't be empty", "Execute", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            for (int i = 0; i < scripts.Count; i++)
            {
                var script = scripts[i];
                if (!System.IO.File.Exists(script.Value))
                {
                    form.ShowDialog($"Script '{script.Value}' not found", "Execute", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            if (!skipConfirmation)
            {
                var connectionsText = $"{connections.Count} connection" + (connections.Count > 1 ? "s" : "");
                var parametersText = $"{parameters.Count} parameter" + (parameters.Count > 1 ? "s" : "");
                var scriptsText = $"{scripts.Count} script" + (scripts.Count > 1 ? "s" : "");
                var message = $"Execute {scriptsText} with {parametersText} on {connectionsText}?";
                var result = form.ShowDialog(message, "Execute", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result != MessageBoxResult.OK)
                    return;
            }
            LogPanel.Clear();
            var param = new ScriptExecutorParam()
            {
                Connections = connections,
                Parameters = parameters,
                Scripts = scripts,
            };
            InfoPanel.AddTask(TaskId);
            _TaskControl = ScriptsPanel;
            _Cts = new System.Threading.CancellationTokenSource();
            SetExecutingState(true);
            var success = System.Threading.ThreadPool.QueueUserWorkItem(ExecuteTask, param);
            if (!success)
            {
                SetExecutingState(false);
                _TaskControl.ScanProgressPanel.UpdateProgress("Task failed!", "", true);
                InfoPanel.RemoveTask(TaskId);
            }
        }

        ScriptExecutor _ScriptExecutor;
        int TaskId = 1;

        void ExecuteTask(object state)
        {
            ControlsHelper.Invoke(() =>
            {
                _TaskControl.ScanProgressPanel.UpdateProgress("Starting...", "", true);
            });
            _ScriptExecutor = new ScriptExecutor();
            _ScriptExecutor.CancellationToken = _Cts.Token;
            _ScriptExecutor.Progress += _ScriptExecutor_Progress;
            _ScriptExecutor.InfoMessage += _ScriptExecutor_InfoMessage;
            _ScriptExecutor.BatchMessage += _ScriptExecutor_BatchMessage;
            var param = (ScriptExecutorParam)state;
            _ScriptExecutor.ProcessData(param);
        }

        private void _ScriptExecutor_BatchMessage(object sender, string message)
        {
            LogPanel.Add(message + "\r\n");
        }

        private void _ScriptExecutor_InfoMessage(object sender, Microsoft.Data.SqlClient.SqlInfoMessageEventArgs e)
        {
            LogPanel.Add("Level {0} Message: {1}\r\n", e.Errors[0].Class, e.Message);
        }

        private void _ScriptExecutor_Progress(object sender, ProgressEventArgs e)
        {
            if (ControlsHelper.InvokeRequired)
            {
                ControlsHelper.Invoke(() =>
                    _ScriptExecutor_Progress(sender, e)
                );
                return;
            }
            var scanner = (ScriptExecutor)sender;
            switch (e.State)
            {
                case ProgressStatus.Started:
                    var sm = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Started...";
                    _TaskControl.ScanProgressPanel.UpdateProgress(sm, "");
                    LogPanel.Add("{0}\r\n", sm);
                    break;
                case ProgressStatus.Updated:
                    _TaskControl.ScanProgressPanel.UpdateProgress(e);
                    if (!string.IsNullOrEmpty(e.SubMessage))
                        LogPanel.Add("{0} \\ {1}\r\n", e.TopMessage, e.SubMessage);
                    break;
                case ProgressStatus.Exception:
                    LogPanel.Add("{0}\r\n", e.Exception.ToString());
                    SetExecutingState(false);
                    _TaskControl.ScanProgressPanel.UpdateProgress();
                    InfoPanel.RemoveTask(TaskId);
                    break;
                case ProgressStatus.Completed:
                    var dm = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Done.";
                    LogPanel.Add("{0}\r\n", dm);
                    SetExecutingState(false);
                    _TaskControl.ScanProgressPanel.UpdateProgress();
                    InfoPanel.RemoveTask(TaskId);
                    if (_Arguments.ContainsKey("AutoClose"))
                        ControlsHelper.BeginInvoke(() =>
                        {
                            System.Windows.Application.Current.Shutdown();
                        }, AutoDelay);
                    break;
                default:
                    break;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Global.AppData.Save();
        }

        void LoadHelpAndInfo(bool setLog = false)
        {
            // Set log.
            if (setLog && !string.IsNullOrEmpty(Global.AppSettings.LogsBodyText))
            {
                LogPanel.Clear();
                LogPanel.Add(Global.AppSettings.LogsBodyText);
            }
            var assembly = Assembly.GetExecutingAssembly();
            // Set Help Head text
            var product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute))).Product;
            var helpHead = string.IsNullOrEmpty(Global.AppSettings.HelpHeadText)
                ? product
                : Global.AppSettings.HelpHeadText;
            InfoPanel.SetHead(helpHead);
            // Set Help Body text.
            var description = ((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute))).Description;
            var helpBody = string.IsNullOrEmpty(Global.AppSettings.HelpBodyText)
                ? description
                : Global.AppSettings.HelpBodyText;
            InfoPanel.SetBodyInfo(helpBody);
        }

        private void HelpHeadEditTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => LoadHelpAndInfo();

        private void HelpBodyEditTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => LoadHelpAndInfo();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var bytes = JocysCom.ClassLibrary.Helper.FindResource<byte[]>("Documents.Help.rtf");
            ControlsHelper.SetTextFromResource(HelpRichTextBox, bytes);

            if (_Arguments.ContainsKey("AutoExecute"))
                ControlsHelper.BeginInvoke(() =>
                {
                    Execute(true);
                }, AutoDelay);

        }
    }
}
