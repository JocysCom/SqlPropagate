using JocysCom.ClassLibrary;
using JocysCom.ClassLibrary.Controls;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace JocysCom.Sql.Propagate
{

	public partial class ScriptExecutor : IProgress<ProgressEventArgs>
	{

		#region ■ IProgress

		public event EventHandler<ProgressEventArgs> Progress;

		public void Report(ProgressEventArgs e)
			=> Progress?.Invoke(this, e);

		#endregion

		public CancellationToken CancellationToken { get; set; }

		public void ProcessData(ScriptExecutorParam param)
		{
			try
			{
				var e = new ProgressEventArgs();
				e.TopMessage = "Started...";
				e.State = ProgressStatus.Started;
				Report(e);
				for (var c = 0; c < param.Connections.Count; c++)
				{
					if (CancellationToken.IsCancellationRequested)
						break;
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
						if (CancellationToken.IsCancellationRequested)
							break;
						var script = param.Scripts[s];
						e.SubIndex = s;
						e.SubCount = param.Scripts.Count;
						e.SubData = script;
						e.SubMessage = $"Script: {script.Name}";
						Report(e);

						var scriptText = ApplyParameters(script, param.Parameters);
						var batches = SplitOnGo(scriptText);
						using (var conn = new SqlConnection(connection.Value))
						{
							conn.InfoMessage += Conn_InfoMessage;
							conn.Open();
							foreach (var batch in batches)
							{
								if (CancellationToken.IsCancellationRequested)
									break;
								if (string.IsNullOrWhiteSpace(batch))
									continue;
								using (var cmd = new SqlCommand(batch, conn))
								{
									cmd.CommandTimeout = 300;
									using (var reader = cmd.ExecuteReader())
									{
										do
										{
											if (reader.FieldCount > 0)
											{
												var rowCount = 0;
												while (reader.Read())
													rowCount++;
												BatchMessage?.Invoke(this, $"({rowCount} row{(rowCount == 1 ? "" : "s")} affected)");
											}
											else if (reader.RecordsAffected >= 0)
											{
												BatchMessage?.Invoke(this, $"({reader.RecordsAffected} row{(reader.RecordsAffected == 1 ? "" : "s")} affected)");
											}
										} while (reader.NextResult());
									}
								}
							}
						}
					}
				}
				e = new ProgressEventArgs();
				e.State = CancellationToken.IsCancellationRequested
					? ProgressStatus.Exception
					: ProgressStatus.Completed;
				if (CancellationToken.IsCancellationRequested)
					e.Exception = new OperationCanceledException("Execution cancelled by user.");
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

		/// <summary>
		/// Split a SQL script on GO batch separators.
		/// GO must appear on its own line (optionally preceded/followed by whitespace).
		/// </summary>
		public static string[] SplitOnGo(string script)
		{
			// Match "GO" on its own line, case-insensitive.
			return Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
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

		public event EventHandler<SqlInfoMessageEventArgs> InfoMessage;
		public event EventHandler<string> BatchMessage;

	}
}
