using System.IO;
using projekt2PlikIndeksowy.Tree;
using BC = System.BitConverter;

namespace projekt2PlikIndeksowy.IO {
	public class BufferedIO {

		public Stream Stream { get; protected set; }
		protected int _pageSize;
		public long DiskOperations;

		public BufferedIO(Stream stream, int pageSize) {
			Stream = stream;
			_pageSize = pageSize;
		}

		public byte[] ReadPage(long index) {

			var page = new byte[_pageSize];

			Stream.Position = index * _pageSize;

			Stream.Read(page, 0, _pageSize);

			DiskOperations++;
			return page;
		}

		public Record ReadRecord(long index) {

			var record = new byte[_pageSize];

			Stream.Position = index * _pageSize;

			Stream.Read(record, 0, _pageSize);

			DiskOperations++;
			return new Record(BC.ToDouble(record, 0), BC.ToInt16(record, 8));
		}

		public void WritePage(byte[] data, long index) {
			Stream.Position = index * _pageSize;

			Stream.Write(data, 0, _pageSize);
			Stream.Flush();
			DiskOperations++;
		}

		public void WriteRecord(Record record, long index) {
			Stream.Position = index * _pageSize;

			Stream.Write(BC.GetBytes(record.Radius), 0, sizeof(double));
			Stream.Write(BC.GetBytes(record.Angle), 0, sizeof(short));
			Stream.Flush();
			DiskOperations++;
		}

		public byte[] ReadAll() {
			Stream.Position = 0;
			var all = new byte[Stream.Length];

			Stream.Read(all, 0, (int)Stream.Length);
			return all;
		}

		public void SaveAll(byte[] all) {
			Stream.Position = 0;

			Stream.Write(all, 0, all.Length);
			Stream.Flush();
		}

		public void ResetOperations() {
			DiskOperations = 0;
		}
	}
}
