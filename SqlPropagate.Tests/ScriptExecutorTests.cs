using JocysCom.Sql.Propagate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace JocysCom.Sql.Propagate.Tests
{
	[TestClass]
	public class ScriptExecutorTests
	{
		[TestMethod]
		public void ApplyParameters_ReplacesAllParameters()
		{
			// Arrange
			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "SELECT * FROM {Table} WHERE {Column} = '{Value}'");
			var script = new DataItem { Name = "test.sql", Value = tempFile };
			var parameters = new List<DataItem>
			{
				new DataItem { Name = "{Table}", Value = "Users" },
				new DataItem { Name = "{Column}", Value = "Name" },
				new DataItem { Name = "{Value}", Value = "Admin" },
			};

			try
			{
				// Act
				var result = ScriptExecutor.ApplyParameters(script, parameters);

				// Assert
				Assert.AreEqual("SELECT * FROM Users WHERE Name = 'Admin'", result);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[TestMethod]
		public void ApplyParameters_NoParameters_ReturnsOriginalScript()
		{
			// Arrange
			var tempFile = Path.GetTempFileName();
			var originalText = "SELECT 1";
			File.WriteAllText(tempFile, originalText);
			var script = new DataItem { Name = "test.sql", Value = tempFile };
			var parameters = new List<DataItem>();

			try
			{
				// Act
				var result = ScriptExecutor.ApplyParameters(script, parameters);

				// Assert
				Assert.AreEqual(originalText, result);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[TestMethod]
		public void ApplyParameters_MultipleOccurrences_ReplacesAll()
		{
			// Arrange
			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "{Param} and {Param} again");
			var script = new DataItem { Name = "test.sql", Value = tempFile };
			var parameters = new List<DataItem>
			{
				new DataItem { Name = "{Param}", Value = "Hello" },
			};

			try
			{
				// Act
				var result = ScriptExecutor.ApplyParameters(script, parameters);

				// Assert
				Assert.AreEqual("Hello and Hello again", result);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		[TestMethod]
		public void DataItem_PropertyChanged_RaisesEvent()
		{
			// Arrange
			var item = new DataItem();
			string changedProperty = null;
			item.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

			// Act
			item.Name = "TestName";

			// Assert
			Assert.AreEqual("Name", changedProperty);
		}

		[TestMethod]
		public void DataItem_SetProperties_RetainsValues()
		{
			// Arrange & Act
			var item = new DataItem
			{
				Name = "MyConnection",
				Value = "Server=localhost;Database=TestDb",
				Order = 5,
				IsChecked = true,
				StatusText = "OK",
			};

			// Assert
			Assert.AreEqual("MyConnection", item.Name);
			Assert.AreEqual("Server=localhost;Database=TestDb", item.Value);
			Assert.AreEqual(5, item.Order);
			Assert.IsTrue(item.IsChecked);
			Assert.AreEqual("OK", item.StatusText);
		}

		[TestMethod]
		public void ScriptExecutorParam_ListProperties_InitializeCorrectly()
		{
			// Arrange & Act
			var param = new ScriptExecutorParam
			{
				Connections = new List<DataItem> { new DataItem { Name = "Conn1" } },
				Parameters = new List<DataItem> { new DataItem { Name = "{Db}", Value = "TestDb" } },
				Scripts = new List<DataItem> { new DataItem { Name = "Script1.sql", Value = "path" } },
			};

			// Assert
			Assert.AreEqual(1, param.Connections.Count);
			Assert.AreEqual(1, param.Parameters.Count);
			Assert.AreEqual(1, param.Scripts.Count);
			Assert.AreEqual("Conn1", param.Connections[0].Name);
		}

		[TestMethod]
		public void SplitOnGo_SplitsBatches()
		{
			var script = "SELECT 1\r\nGO\r\nSELECT 2\r\nGO\r\nSELECT 3";
			var batches = ScriptExecutor.SplitOnGo(script);
			Assert.AreEqual(3, batches.Length);
			Assert.IsTrue(batches[0].Trim().Contains("SELECT 1"));
			Assert.IsTrue(batches[1].Trim().Contains("SELECT 2"));
			Assert.IsTrue(batches[2].Trim().Contains("SELECT 3"));
		}

		[TestMethod]
		public void SplitOnGo_CaseInsensitive()
		{
			var script = "SELECT 1\ngo\nSELECT 2\nGo\nSELECT 3";
			var batches = ScriptExecutor.SplitOnGo(script);
			Assert.AreEqual(3, batches.Length);
		}

		[TestMethod]
		public void SplitOnGo_NoGo_ReturnsSingleBatch()
		{
			var script = "SELECT 1\nSELECT 2";
			var batches = ScriptExecutor.SplitOnGo(script);
			Assert.AreEqual(1, batches.Length);
			Assert.IsTrue(batches[0].Contains("SELECT 1"));
		}

		[TestMethod]
		public void SplitOnGo_GoInsideString_NotSplit()
		{
			// GO in a string literal on the same line as other text should not split.
			var script = "SELECT 'GO somewhere'\nGO\nSELECT 2";
			var batches = ScriptExecutor.SplitOnGo(script);
			Assert.AreEqual(2, batches.Length);
			Assert.IsTrue(batches[0].Contains("GO somewhere"));
		}

		[TestMethod]
		public void DefaultScripts_AreEmbeddedResources()
		{
			var assembly = typeof(ScriptExecutor).Assembly;
			var resourceNames = assembly.GetManifestResourceNames();
			Assert.IsTrue(resourceNames.Any(n => n.EndsWith("Script1.sql")), "Script1.sql should be an embedded resource.");
			Assert.IsTrue(resourceNames.Any(n => n.EndsWith("Script2.sql")), "Script2.sql should be an embedded resource.");
			Assert.IsTrue(resourceNames.Any(n => n.EndsWith("JocysCom.Sql.Propagate.xml")), "Default config XML should be an embedded resource.");
		}

		[TestMethod]
		public void DefaultScript_ApplyParameters_ReplacesMyParam1()
		{
			// Extract Script1.sql from embedded resources and apply parameter.
			var assembly = typeof(ScriptExecutor).Assembly;
			string scriptContent = null;
			foreach (var name in assembly.GetManifestResourceNames())
			{
				if (name.EndsWith("Script1.sql"))
				{
					using (var stream = assembly.GetManifestResourceStream(name))
					using (var reader = new StreamReader(stream))
						scriptContent = reader.ReadToEnd();
					break;
				}
			}
			Assert.IsNotNull(scriptContent, "Could not read Script1.sql from embedded resources.");

			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, scriptContent);
			var script = new DataItem { Name = "Script1.sql", Value = tempFile };
			var parameters = new List<DataItem>
			{
				new DataItem { Name = "$(MyParam1)", Value = "Hello from test!" },
			};

			try
			{
				var result = ScriptExecutor.ApplyParameters(script, parameters);
				Assert.IsTrue(result.Contains("Hello from test!"), "$(MyParam1) should be replaced.");
				Assert.IsFalse(result.Contains("$(MyParam1)"), "No unresolved $(MyParam1) placeholders should remain.");
				Assert.IsTrue(result.Contains("SERVERPROPERTY"), "Script should contain SERVERPROPERTY queries.");
			}
			finally
			{
				File.Delete(tempFile);
			}
		}
	}
}
