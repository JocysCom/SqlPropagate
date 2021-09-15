using System.Collections.Generic;

namespace JocysCom.Sql.Propagate
{
	public class ScriptExecutorParam
	{
		public List<DataItem> Connections { get; set; }
		public List<DataItem> Parameters { get; set; }
		public List<DataItem> Scripts { get; set; }
	}
}
