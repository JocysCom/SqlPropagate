using JocysCom.ClassLibrary.Controls;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace JocysCom.Sql.Propagate
{

	public partial class ScriptExecutor : IProgress<ProgressEventArgs>
	{

		#region ■ IProgress

		public event EventHandler<ProgressEventArgs> Progress;

		public void Report(ProgressEventArgs e)
			=> Progress?.Invoke(this, e);

		#endregion

		public void ProcessData(ScriptExecutorParam param)
		{
			try
			{
				var e = new ProgressEventArgs();
				// Create "References" solution folder.
				e.TopMessage = "Started...";
				e.State = ProgressStatus.Started;
				Report(e);
				for (var c = 0; c < param.Connections.Count; c++)
				{
					var connection = param.Connections[c];
					e.State = ProgressStatus.Updated;
					e.TopIndex = c;
					e.TopCount = param.Connections.Count;
					e.TopData = connection;
					e.TopMessage = $"Connection: {connection.Name}";
					e.ClearSub();
					Report(e);
					for (int s = 0; s < param.Scripts.Count; s++)
					{
						var script = param.Scripts[s];
						e.SubIndex = s;
						e.SubCount = param.Scripts.Count;
						e.SubData = script;
						e.SubMessage = $"Script: {script.Name}";
						Report(e);

						var scriptText = ApplyParameters(script, param.Parameters);
						var conn = new System.Data.SqlClient.SqlConnection(connection.Value);
						conn.InfoMessage += Conn_InfoMessage; ;
						var sconn = new Microsoft.SqlServer.Management.Common.ServerConnection(conn);
						var server = new Microsoft.SqlServer.Management.Smo.Server(sconn);
						int ra = -1;

						// Requires:
						// Microsoft.SqlServer.ConnectionInfo, Microsoft.SqlServer.Management.Sdk and Microsoft.SqlServer.Smo
						//
						// IMPORTANT EXCEPTION: System.BadImageFormatException: 'Could not load file or assembly 'Microsoft.SqlServer.BatchParser
						// 1. Set project: Build -> Platform Target: x64
						// 2. Add Reference: Class Library\_Resources\Microsoft.SqlServer\runtimes\win-x64\native\Microsoft.SqlServer.BatchParser.dll
						// Important;ExecuteNonQuery allows to execute scripts with GO statements.
						ra = server.ConnectionContext.ExecuteNonQuery(scriptText);
						//var ra = server.ConnectionContext.ExecuteWithResults(script);
						//ResultsDataGrid.ItemsSource = ra.Tables.Cast<System.Data.DataTable>().FirstOrDefault()?.DefaultView;
						conn.Close();
					}
				}
				e = new ProgressEventArgs();
				e.State = ProgressStatus.Completed;
				Report(e);
			}
			catch (Exception ex)
			{
				var e2 = new ProgressEventArgs();
				e2.State = ProgressStatus.Exception;
				e2.Exception = ex;
				Report(e2);
			}
		}

		private void Conn_InfoMessage(object sender, SqlInfoMessageEventArgs e)
		{
			InfoMessage?.Invoke(sender, e);
		}

		public static string ApplyParameters(DataItem script, IList<DataItem> parameters)
		{
			var scriptText = System.IO.File.ReadAllText(script.Value);
			for (int p = 0; p < parameters.Count; p++)
			{
				var parameter = parameters[p];
				scriptText = scriptText.Replace(parameter.Name, parameter.Value);
			}
			return scriptText;
		}

		public event SqlInfoMessageEventHandler InfoMessage;

	}
}
