using JocysCom.ClassLibrary.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
		public MainWindow()
		{
			ControlsHelper.InitInvokeContext();
			// Use configuration from local folder.
			var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
			var baseName = System.IO.Path.GetFileNameWithoutExtension(exeName);
			Global.AppData.XmlFile = new FileInfo($"{baseName}.xml");
			Global.AppData.Load();
			if (Global.AppData.Items.Count == 0)
			{
				Global.AppData.Items.Add(new AppData());
				Global.AppData.Save();
			}
			if (Global.AppSettings.Parameters.Count == 0)
			{
				var newItem = new DataItem()
				{
					Name = "$(MyParam1)",
					Value = "MyValue1",
					IsChecked = true,
					IsEnabled = true,
				};
				Global.AppSettings.Parameters.Add(newItem);
				newItem.Order = Global.AppSettings.Parameters.IndexOf(newItem);
			}
			InitializeComponent();
			var assembly = Assembly.GetExecutingAssembly();
			HMan = new BaseWithHeaderManager<int>(HelpHeadLabel, HelpBodyLabel, LeftIcon, RightIcon, this);
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
			//ScriptsPanel.MainDataGrid.IsReadOnly = false;
		}

		private void ScriptsPanel_MainDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var scriptItem = (DataItem)ScriptsPanel.MainDataGrid.SelectedItem;
			if (scriptItem == null)
				return;
			var parameters = ParametersPanel.GetCheckedOrSelectedReferences(out bool containsChecked);
			ScriptTextBox.Text = ScriptExecutor.ApplyParameters(scriptItem, parameters);
			//ScriptTabItem.Visibility = Visibility.Visible;
			Dispatcher.BeginInvoke((Action)(() => MainTabControl.SelectedIndex = 1));
		}

		#region Connections Panel

		private void ConnectionsPanel_AddButton_Click(object sender, RoutedEventArgs e)
		{
			var newItem = new DataItem();
			newItem.IsEnabled = true;
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

		bool UpdateConnectionItem(DataItem item)
		{
			var dcd = new Microsoft.Data.ConnectionUI.DataConnectionDialog();
			//Adds all the standard supported databases
			//DataSource.AddStandardDataSources(dcd);
			//allows you to add data sources, if you want to specify which will be supported 
			dcd.DataSources.Add(Microsoft.Data.ConnectionUI.DataSource.SqlDataSource);
			dcd.SetSelectedDataProvider(Microsoft.Data.ConnectionUI.DataSource.SqlDataSource, Microsoft.Data.ConnectionUI.DataProvider.SqlDataProvider);
			dcd.ConnectionString = item.Value;
			Microsoft.Data.ConnectionUI.DataConnectionDialog.Show(dcd);
			var isOK = dcd.DialogResult == System.Windows.Forms.DialogResult.OK;
			if (isOK)
			{
				item.Value = dcd.ConnectionString;
				bool isEntity;
				var cs = item.Name = ClassLibrary.Data.SqlHelper.GetProviderConnectionString(item.Value, out isEntity);
				var builder = new System.Data.SqlClient.SqlConnectionStringBuilder(cs);
				item.Name = $"{builder.DataSource}, {builder.InitialCatalog}".Trim(' ', ',');
				Global.AppData.Save();
			}
			return isOK;
		}

		#endregion

		#region Parameters Panel

		private void ParametersPanel_AddButton_Click(object sender, RoutedEventArgs e)
		{
			var item = new DataItem();
			item.IsEnabled = true;
			Global.AppSettings.Parameters.Add(item);
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
					Value = JocysCom.ClassLibrary.IO.PathHelper.GetRelativePath(currentPath, path),
					IsEnabled = true,
				};
				Global.AppSettings.Scripts.Add(newItem);
				newItem.Order = Global.AppSettings.Scripts.IndexOf(newItem);
			}
		}

		#endregion

		public BaseWithHeaderManager<int> HMan;

		private void ScriptPanel_ExecuteButton_Click(object sender, RoutedEventArgs e)
		{
			Execute();
		}

		Controls.DataListControl _TaskControl;

		void Execute()
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
				form.ShowDialog($"Please select at least on connection", "Execute", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}
			if (scripts.Count == 0)
			{
				form.ShowDialog($"Please select at least on script", "Execute", MessageBoxButton.OK, MessageBoxImage.Information);
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
			var connectionsText = $"{connections.Count} connection" + (connections.Count > 1 ? "s" : "");
			var scriptsText = $"{scripts.Count} script" + (scripts.Count > 1 ? "s" : "");
			var message = $"Execute {scriptsText} on {connectionsText}?";
			var result = form.ShowDialog(message, "Execute", MessageBoxButton.OKCancel, MessageBoxImage.Question);
			if (result != MessageBoxResult.OK)
				return;
			LogTextBox.Text = "";
			var param = new ScriptExecutorParam()
			{
				Connections = connections,
				Parameters = parameters,
				Scripts = scripts,
			};
			HMan.AddTask(TaskId);
			_TaskControl = ScriptsPanel;
			var success = System.Threading.ThreadPool.QueueUserWorkItem(ExecuteTask, param);
			if (!success)
			{
				_TaskControl.UpdateProgress("Task failed!", "", true);
				HMan.RemoveTask(TaskId);
			}
		}

		ScriptExecutor _ScriptExecutor;
		int TaskId = 1;

		void ExecuteTask(object state)
		{
			ControlsHelper.Invoke(() =>
			{
				_TaskControl.UpdateProgress("Starting...", "", true);
			});
			_ScriptExecutor = new ScriptExecutor();
			_ScriptExecutor.Progress += _ScriptExecutor_Progress;
			_ScriptExecutor.InfoMessage += _ScriptExecutor_InfoMessage;
			var param = (ScriptExecutorParam)state;
			_ScriptExecutor.ProcessData(param);
		}

		private void _ScriptExecutor_InfoMessage(object sender, System.Data.SqlClient.SqlInfoMessageEventArgs e)
		{
			ControlsHelper.Invoke(() =>
			{
				var s = $"Level {e.Errors[0].Class} Message: {e.Message}\r\n";
				LogTextBox.Text += s;
			});
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
					_TaskControl.UpdateProgress(sm, "");
					LogTextBox.Text += $"{sm}\r\n";
					break;
				case ProgressStatus.Updated:
					_TaskControl.UpdateProgress(e);
					if (!string.IsNullOrEmpty(e.SubMessage))
						LogTextBox.Text += $"{e.TopMessage} \\ {e.SubMessage}\r\n";
					break;
				case ProgressStatus.Exception:
					LogTextBox.Text += $"{e.Exception.ToString()}\r\n";
					break;
				case ProgressStatus.Completed:
					var dm = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Done.";
					LogTextBox.Text += $"{dm}\r\n";
					_TaskControl.UpdateProgress();
					HMan.RemoveTask(TaskId);
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
			if (setLog)
				LogTextBox.Text = Global.AppSettings.LogsBodyText;
			if (HMan == null)
				return;
			var assembly = Assembly.GetExecutingAssembly();
			// Set Help Head text
			var product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute))).Product;
			var helpHead = string.IsNullOrEmpty(Global.AppSettings.HelpHeadText)
				? product
				: Global.AppSettings.HelpHeadText;
			HMan.SetHead(helpHead);
			// Set Help Body text.
			var description = ((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute))).Description;
			var helpBody = string.IsNullOrEmpty(Global.AppSettings.HelpBodyText)
				? description
				: Global.AppSettings.HelpBodyText;
			HMan.SetBodyInfo(helpBody);
		}

		public VerticalAlignment GetScrollVerticalAlignment(System.Windows.Controls.Primitives.TextBoxBase control)
		{
			// Vertical scroll position.
			var offset = control.VerticalOffset;
			// Vertical size of the scrollable content area.
			var height = control.ViewportHeight;
			// Vertical size of the visible content area.
			var visibleView = control.ExtentHeight;
			// Allow flexibility of 2 pixels.
			var flex = 2;
			if (offset + height - visibleView < flex)
				return VerticalAlignment.Bottom;
			if (offset < flex)
				return VerticalAlignment.Top;
			return VerticalAlignment.Center;
		}

		void AutoScroll(System.Windows.Controls.Primitives.TextBoxBase control)
		{
			var scrollPosition = GetScrollVerticalAlignment(control);
			if (scrollPosition == VerticalAlignment.Bottom && control.IsVisible)
				control.ScrollToEnd();
		}

		private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
			=> AutoScroll((TextBox)sender);

		private void LogTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
			=> AutoScroll((TextBox)sender);

		private void HelpHeadEditTextBox_TextChanged(object sender, TextChangedEventArgs e)
			=> LoadHelpAndInfo();

		private void HelpBodyEditTextBox_TextChanged(object sender, TextChangedEventArgs e)
			=> LoadHelpAndInfo();


	}
}
