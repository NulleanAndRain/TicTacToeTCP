using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommonClasses {
	[Serializable]
	public class DataMessage {
		public enum DataType {
			noData

		}

		public DataType type;
		public string dataJson;

		public DataMessage(DataType type, string dataJson) {
			this.type = type;
			this.dataJson = dataJson;
		}

		public DataMessage() : this(DataType.noData, string.Empty) { }

		public string getJSON() {
			return JsonSerializer.Serialize(this);
		}

		public static DataMessage getData(string json) {
			DataMessage data = JsonSerializer.Deserialize<DataMessage>(json);
			return data;
		}
	}
}
