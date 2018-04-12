using System;
using System.IO;
using System.Threading;
using projekt2PlikIndeksowy.Exceptions;
using projekt2PlikIndeksowy.IO;
using projekt2PlikIndeksowy.Tree;



namespace projekt2PlikIndeksowy {
	class Program {

		static void Main(string[] args) {

			var treeStream = File.Open(Static.FilesPath + "tree", FileMode.OpenOrCreate);
			var dataStream = File.Open(Static.FilesPath + "data", FileMode.OpenOrCreate);
			var treeMapStream = File.Open(Static.FilesPath + "treeMap", FileMode.OpenOrCreate);
			var dataMapStream = File.Open(Static.FilesPath + "dataMap", FileMode.OpenOrCreate);
			#region test
			//var tree = new BTree(treeStream, dataStream, treeMapStream, dataMapStream, true);


			//const int am = 10000;
			//var keys = new int[am];
			//var rand = new Random(100);


			//for (var i = 0; i < am; i++)
			//	keys[i] = rand.Next(int.MaxValue);

			//for (var i = 0; i < am; i++)
			//	tree.InsertRecord(new DataRecord(keys[i], i * 0.1, (short)(i % 360)));

			//for (var i = 0; i < am / 2; i++)
			//	tree.RemoveRecordByKey(keys[i]);


			//for (var i = 0; i < am; i++) {
			//	var record = tree.FindRecordByKey(keys[i]);
			//	Console.WriteLine(record != null ? $"{record.Key} {record.Record.Radius} {record.Record.Angle}" : "null");
			//}


			//goto endjmp;
			#endregion

			StreamReader reader = null;
			var fileReadMode = false;

			#region program

			BTree tree;

			Console.WriteLine("Choose what to do:\n" +
							  "\t-type \"create\" to create new BTree\n" +
							  "\t-type \"open\" to open existing BTree\n" +
							  "\t-type \"generate [count]\" to generate random inserts"
							  );
			string command;
			while (true) {

				Console.Write("$: ");
				command = Console.ReadLine();

				var parts = command.Split(' ');
				if (parts[0] == "create") {
					tree = new BTree(treeStream, dataStream, treeMapStream, dataMapStream, true);
					break;
				}
				if (parts[0] == "open") {
					tree = new BTree(treeStream, dataStream, treeMapStream, dataMapStream, false);
					break;
				}
				if (parts[0] == "generate") {
					GenerateInserts(int.Parse(parts[1]));
					continue;
				}

				Console.WriteLine("Invalid command");
			}
			var end = false;
			while (!end) {
				if (!fileReadMode) {
					Console.Write("$: ");
					command = Console.ReadLine();
				} else {
					command = reader.ReadLine();
					if (command == null) {
						reader.Dispose();
						fileReadMode = false;
						continue;
					}
				}
				var parts = command.Split(' ');
				try {
					switch (parts[0]) {
						case "insert": {
								if (parts.Length < 4)
									throw new FormatException();
								var key = long.Parse(parts[1]);
								var radius = double.Parse(parts[2]);
								var angle = short.Parse(parts[3]);

								if (angle < 0 || angle > 360) {
									Console.WriteLine("Invalid angle parameter");
									break;
								}
								tree.BtreeIO.ResetOperations();
								tree.DataIO.ResetOperations();
								try {
									tree.InsertRecord(new DataRecord(key, radius, angle));
								} catch (KeyAlreadyExistsException) {
									Console.WriteLine("Key already exists");
								}
								break;
							}
						case "remove": {
								if (parts.Length < 2)
									throw new FormatException();
								var key = long.Parse(parts[1]);

								tree.BtreeIO.ResetOperations();
								tree.DataIO.ResetOperations();
								try {
									tree.RemoveRecordByKey(key);
								} catch (KeyDoesNotExistException) {
									Console.WriteLine("Key does not exist");
								}

								break;
							}
						case "update": {
								if (parts.Length < 4)
									throw new FormatException();
								var key = long.Parse(parts[1]);
								var radius = double.Parse(parts[2]);
								var angle = short.Parse(parts[3]);

								if (angle < 0 || angle > 360) {
									Console.WriteLine("Invalid angle parameter");
									break;
								}
								tree.BtreeIO.ResetOperations();
								tree.DataIO.ResetOperations();
								try {
									tree.UpdateRecord(new DataRecord(key, radius, angle));
								} catch (KeyDoesNotExistException) {
									Console.WriteLine("Key does not exist");
								}
								break;
							}
						case "find": {
								if (parts.Length < 2)
									throw new FormatException();
								var key = long.Parse(parts[1]);

								tree.BtreeIO.ResetOperations();
								tree.DataIO.ResetOperations();

								var record = tree.FindRecordByKey(key);
								if (record != null)
									Console.WriteLine($"{record.Key} {record.Record.Radius} {record.Record.Angle}");
								else {
									Console.WriteLine("Key does not exist");
								}


								break;
							}
						case "show": {
								tree.BtreeIO.ResetOperations();
								tree.DataIO.ResetOperations();
								Console.WriteLine("-----------------BTree-----------------");
								tree.DisplayTree(tree.RootPage);
								Console.WriteLine("--------------End of BTree-------------");
								break;
							}
						case "disk": {
								Console.WriteLine("Disk operations:");
								Console.WriteLine("\ttree: " + tree.BtreeIO.DiskOperations);
								Console.WriteLine("\tdata: " + tree.DataIO.DiskOperations);
								break;
							}
						case "help": {

								Console.WriteLine("commands:");
								Console.WriteLine("\t insert    [key] [double] [short 0:360]");
								Console.WriteLine("\t remove    [key]");
								Console.WriteLine("\t update    [key] [double] [short 0:360]");
								Console.WriteLine("\t find      [key]");
								Console.WriteLine("\t show      - displays entire tree");
								Console.WriteLine("\t disk      - displays disk operations from last executed command");
								Console.WriteLine("\t exit      - ends program");

								break;
							}
						case "load": {
								try {
									reader = new StreamReader(File.Open(Static.FilesPath + parts[1], FileMode.Open));
									fileReadMode = true;
								} catch (Exception) {
									Console.WriteLine("File does not exist");
								}
								break;
							}

						case "exit": {
								end = true;
								break;
							}
						default: {
								Console.WriteLine("Invalid command");
								break;
							}
					}
				} catch (FormatException) {
					Console.WriteLine("Invalid command");
				}


			}
			#endregion

			endjmp:
			tree.Dispose();
		}


		public static void GenerateInserts(int count) {

			var insertsStream = File.Open(Static.FilesPath + "inserts.txt", FileMode.OpenOrCreate);
			var removesStream = File.Open(Static.FilesPath + "removes.txt", FileMode.OpenOrCreate);
			var findsStream = File.Open(Static.FilesPath + "finds.txt", FileMode.OpenOrCreate);
			insertsStream.SetLength(0);
			removesStream.SetLength(0);
			findsStream.SetLength(0);

			var insertsWriter = new StreamWriter(insertsStream);
			var removesWriter = new StreamWriter(removesStream);
			var findsWriter = new StreamWriter(findsStream);

			var random = new Random();

			for (var i = 0; i < count; i++) {
				var randKey = random.Next(0, 10000000);
				insertsWriter.WriteLine($"insert {randKey} {random.NextDouble()} {random.Next(0, 360)}");
				insertsWriter.Flush();
				insertsStream.Flush();
				removesWriter.WriteLine($"remove {randKey}");
				removesWriter.Flush();
				removesStream.Flush();
				findsWriter.WriteLine($"find {randKey}\ndisk");
				findsWriter.Flush();
				findsStream.Flush();
			}

			insertsStream.Dispose();
			removesStream.Dispose();
			findsStream.Dispose();
		}

	}
}

