using System;
using System.Collections.Generic;
using System.IO;
using projekt2PlikIndeksowy.Exceptions;
using projekt2PlikIndeksowy.IO;


namespace projekt2PlikIndeksowy.Tree {
	public class BTree : IDisposable {


		public BufferedIO BtreeIO;
		public BufferedIO DataIO;

		public BTreeHeaderPage BTreeHeader;

		public FreeSpaceMap BTreeFreeSpaceMap;
		public FreeSpaceMap DataFreeSpaceMap;




		public BTreePage RootPage;

		private Stack<PathPart> _path;
		private int _childrenIndex;
		private BTreePage _currentPage;
		private BTreePage _parentPage;


		public BTree(Stream bTreeStream, Stream dataStream, Stream bTreeMap, Stream dataMap, bool create) {


			BtreeIO = new BufferedIO(bTreeStream, (int)Static.PageSize);
			DataIO = new BufferedIO(dataStream, Record.ByteSize);

			if (create) {
				bTreeStream.SetLength(0);
				dataStream.SetLength(0);
				bTreeMap.SetLength(0);
				dataMap.SetLength(0);

				BTreeHeader = new BTreeHeaderPage(Static.FirstRootIndex);
				RootPage = new BTreePage(0, Static.FirstRootIndex);
				BTreeFreeSpaceMap = new FreeSpaceMap(bTreeMap, 2);
				DataFreeSpaceMap = new FreeSpaceMap(dataMap, 0);
			} else {
				BTreeHeader = new BTreeHeaderPage(BtreeIO.ReadPage(0));
				RootPage = new BTreePage(BtreeIO.ReadPage(BTreeHeaderPage.RootIndex));
				BTreeFreeSpaceMap = new FreeSpaceMap(bTreeMap, bTreeStream.Length / Static.PageSize);
				DataFreeSpaceMap = new FreeSpaceMap(dataMap, dataStream.Length / Record.ByteSize);
			}


		}

		private NodeRecord FindByKey(long key) {
			_currentPage = RootPage;
			_path = new Stack<PathPart>();
			while (true) {

				var record = _currentPage.FindRecord(key);
				if (record != null)
					return record;

				if (_currentPage.IsLeaf)
					return null;

				_parentPage = _currentPage;
				if (key < _currentPage.Records[0].Key) {
					_childrenIndex = 0;
					_currentPage = new BTreePage(BtreeIO.ReadPage(_currentPage.Childrens[_childrenIndex]));
				} else if (key > _currentPage.Records[_currentPage.Records.Count - 1].Key) {
					_childrenIndex = _currentPage.Records.Count;
					_currentPage = new BTreePage(BtreeIO.ReadPage(_currentPage.Childrens[_childrenIndex]));
				} else {
					for (var i = 0; i < _currentPage.Records.Count - 1; i++) {
						if (key > _currentPage.Records[i].Key && key < _currentPage.Records[i + 1].Key) {
							_childrenIndex = i + 1;
							_currentPage = new BTreePage(BtreeIO.ReadPage(_currentPage.Childrens[_childrenIndex]));
							break;
						}
					}
				}

				_path.Push(new PathPart {
					ParentPage = _parentPage,
					ChildrenIndex = _childrenIndex
				});


			}
		}

		public DataRecord FindRecordByKey(long key) {
			var nodeRecord = FindByKey(key);
			if (nodeRecord == null)
				return null;
			var record = DataIO.ReadRecord(nodeRecord.RecordIndex);

			return new DataRecord(key, record.Radius, record.Angle);
		}

		public void InsertRecord(DataRecord dataRecord) {

			var record = FindByKey(dataRecord.Key);

			if (record != null)
				throw new KeyAlreadyExistsException();



			var dataPointer = DataFreeSpaceMap.AllocatePage();
			DataIO.WriteRecord(dataRecord.Record, dataPointer);
			var nodeRecord = new NodeRecord(dataRecord.Key, dataPointer);


			try {

				_currentPage.InsertRecord(nodeRecord);
			} catch (PageOverflowException) {   //kompensacja lub rozszczepienie

				if (!Compensate())
					Split();

				return;
			}
			BtreeIO.WritePage(_currentPage.GetBTreePage(), _currentPage.SelfIndex);
		}

		public void RemoveRecordByKey(long key) {

			var record = FindByKey(key);

			if (record == null)
				throw new KeyDoesNotExistException();


			var recordIndex = 0;
			BTreePage leafPage;
			try {
				if (_currentPage.IsLeaf) {
					_currentPage.RemoveRecord(key);
					BtreeIO.WritePage(_currentPage.GetBTreePage(), _currentPage.SelfIndex);
					return;
				}

			} catch (PageDeficitException) {
				
				if (!Compensate())
					Merge();
				return;
			} finally {
				DataFreeSpaceMap.FreePage(record.RecordIndex);
			}

			for (var i = 0; i < _currentPage.GetPresentRecords(); i++) {
				if (_currentPage.Records[i].Key == record.Key)
					recordIndex = i;
			}
			if (recordIndex == 0) {
				leafPage = GetPageFromSubTreeWithLowestKey(
					new BTreePage(BtreeIO.ReadPage(_currentPage.Childrens[recordIndex + 1]))
				);

				var input = leafPage.Records[0];

				_currentPage.SwapRecord(input, record);
				leafPage.SwapRecord(record, input);
			} else {
				leafPage = GetPageFromSubTreeWithHighestKey(
					new BTreePage(BtreeIO.ReadPage(_currentPage.Childrens[recordIndex]))
				);
				var input = leafPage.Records[leafPage.GetPresentRecords() - 1];

				_currentPage.SwapRecord(input, record);
				leafPage.SwapRecord(record, input);
			}

			
			BtreeIO.WritePage(_currentPage.GetBTreePage(), _currentPage.SelfIndex);
			try {
				FindByKey(leafPage.Records[1].Key); //dla odswiezenia sciezki _path
				leafPage.RemoveRecord(key);
			} catch (PageDeficitException) {
				_currentPage = leafPage;

				if (!Compensate())
					Merge();
				return;
			}

			BtreeIO.WritePage(leafPage.GetBTreePage(), leafPage.SelfIndex);
		}

		public void UpdateRecord(DataRecord dataRecord) {

			var record = FindByKey(dataRecord.Key);

			if (record == null)
				throw new KeyDoesNotExistException();

			DataIO.WriteRecord(dataRecord.Record, record.RecordIndex);

		}

		public void DisplayTree(BTreePage page, int tabs = 0) {   //root

			for (var i = 0; i < page.Records.Count; i++) {
				if (!page.IsLeaf)
					DisplayTree(new BTreePage(BtreeIO.ReadPage(page.Childrens[i])), tabs + 1);
				
				var record = DataIO.ReadRecord(page.Records[i].RecordIndex);
				var outString = new string(' ', 112);
				outString = outString.Insert(tabs * 4, page.Records[i].Key.ToString());
				outString = outString.Insert(80, record.Radius.ToString());
				outString = outString.Insert(100, record.Angle.ToString());
				outString = outString.Insert(110, tabs.ToString());
				Console.WriteLine(outString);

			}

			if (!page.IsLeaf)
				DisplayTree(new BTreePage(BtreeIO.ReadPage(page.Childrens[page.Childrens.Count - 1])), tabs + 1);
			
		}

		private BTreePage GetPageFromSubTreeWithLowestKey(BTreePage subTreeRootPage) {

			var currentPage = subTreeRootPage;

			while (currentPage.IsLeaf == false) {

				currentPage = new BTreePage(BtreeIO.ReadPage(currentPage.Childrens[0]));
			}

			return currentPage;
		}

		private BTreePage GetPageFromSubTreeWithHighestKey(BTreePage subTreeRootPage) {
			var currentPage = subTreeRootPage;

			while (currentPage.IsLeaf == false) {

				currentPage = new BTreePage(BtreeIO.ReadPage(currentPage.Childrens[currentPage.Childrens.Count - 1]));
			}

			return currentPage;
		}

		private bool Compensate() {

			if (_currentPage.SelfIndex == BTreeHeaderPage.RootIndex)
				return false;

			var partPath = _path.Peek();
			var parentPage = partPath.ParentPage;
			var childrenIndex = partPath.ChildrenIndex;

			BTreePage leftBrother, rightBrother;

			if (childrenIndex == 0) {
				leftBrother = null;
				rightBrother = new BTreePage(BtreeIO.ReadPage(parentPage.Childrens[childrenIndex + 1]));
			} else if (childrenIndex == parentPage.GetPresentRecords()) {
				leftBrother = new BTreePage(BtreeIO.ReadPage(parentPage.Childrens[childrenIndex - 1]));
				rightBrother = null;
			} else {
				leftBrother = new BTreePage(BtreeIO.ReadPage(parentPage.Childrens[childrenIndex - 1]));
				rightBrother = new BTreePage(BtreeIO.ReadPage(parentPage.Childrens[childrenIndex + 1]));
			}

			var sumWithLeft = leftBrother?.GetPresentRecords() + _currentPage.GetPresentRecords();
			var sumWithRight = rightBrother?.GetPresentRecords() + _currentPage.GetPresentRecords();

			if (sumWithLeft >= 2 * Static.MemMin && sumWithLeft <= 2 * Static.MemMax) {
				CompensatePages(leftBrother, _currentPage, parentPage, childrenIndex);
				BtreeIO.WritePage(leftBrother.GetBTreePage(), leftBrother.SelfIndex);
			} else if (sumWithRight >= 2 * Static.MemMin && sumWithRight <= 2 * Static.MemMax) {
				CompensatePages(_currentPage, rightBrother, parentPage, childrenIndex);
				BtreeIO.WritePage(rightBrother.GetBTreePage(), rightBrother.SelfIndex);
			} else {
				return false;
			}

			BtreeIO.WritePage(_currentPage.GetBTreePage(), _currentPage.SelfIndex);
			BtreeIO.WritePage(parentPage.GetBTreePage(), parentPage.SelfIndex);


			return true;
		}

		private void CompensatePages(BTreePage left, BTreePage right, BTreePage parent, int childrenIndex) {



			int parentRecordIndex = 0, recordsToTransfer;
			if (left.GetPresentRecords() < right.GetPresentRecords()) {
				for (var i = 0; i < parent.Childrens.Count; i++) {
					if (parent.Childrens[i] == left.SelfIndex)
						parentRecordIndex = i;
				}
				//parentRecordIndex = childrenIndex - 1;
				recordsToTransfer = (right.GetPresentRecords() - left.GetPresentRecords()) / 2;

				left.InsertRecord(parent.Records[parentRecordIndex]);
				for (var i = 0; i < recordsToTransfer - 1; i++) {
					left.InsertRecord(right.PopRecordAt(0, false));
				}
				if (!right.IsLeaf)
					for (var i = 0; i < recordsToTransfer; i++) {
						left.Childrens.Add(right.PopChildrenAt(0));
					}

				parent.Records[parentRecordIndex] = right.PopRecordAt(0, false);

			} else {
				for (var i = 0; i < parent.Childrens.Count; i++) {
					if (parent.Childrens[i] == left.SelfIndex)
						parentRecordIndex = i;
				}
				//parentRecordIndex = childrenIndex;
				recordsToTransfer = (left.GetPresentRecords() - right.GetPresentRecords()) / 2;

				right.InsertRecord(parent.Records[parentRecordIndex]);

				for (var i = 0; i < recordsToTransfer - 1; i++) {
					right.InsertRecord(left.PopRecordAt(left.Records.Count - 1, false));
				}
				if (!left.IsLeaf)
					for (var i = 0; i < recordsToTransfer; i++) {
						right.Childrens.Insert(0, left.PopChildrenAt(left.Childrens.Count - 1));
					}

				parent.Records[parentRecordIndex] = left.PopRecordAt(left.Records.Count - 1, false);

			}

		}

		private void Split() {

			if (_currentPage.SelfIndex == BTreeHeaderPage.RootIndex) {      //root split

				SplitRoot();
				return;
			}

			var pathPart = _path.Pop();
			var parentPage = pathPart.ParentPage;
			var childrenIndex = pathPart.ChildrenIndex;

			var newPage = new BTreePage(parentPage.SelfIndex, BTreeFreeSpaceMap.AllocatePage());
			if (!_currentPage.IsLeaf)
				newPage.IsLeaf = false;

			parentPage.Childrens.Insert(childrenIndex, newPage.SelfIndex);

			var recordsPerPage = _currentPage.GetPresentRecords() / 2;

			for (var i = 0; i < recordsPerPage; i++) {
				newPage.InsertRecord(_currentPage.PopRecordAt(0, false));
			}

			if (!_currentPage.IsLeaf) {
				for (var i = 0; i < recordsPerPage + 1; i++) {
					newPage.Childrens.Add(_currentPage.PopChildrenAt(0));
				}
			}

			var forParent = _currentPage.PopRecordAt(0, false);

			BtreeIO.WritePage(_currentPage.GetBTreePage(), _currentPage.SelfIndex);
			BtreeIO.WritePage(newPage.GetBTreePage(), newPage.SelfIndex);

			try {
				parentPage.InsertRecord(forParent);

				BtreeIO.WritePage(parentPage.GetBTreePage(), parentPage.SelfIndex);
			} catch (PageOverflowException) {
				_currentPage = parentPage;

				if (Compensate())
					return;

				Split();
			}

		}

		private void SplitRoot() {

			var rootPage = new BTreePage(0, BTreeFreeSpaceMap.AllocatePage());
			rootPage.IsLeaf = false;

			_currentPage.ParentIndex = rootPage.SelfIndex;
			var newPage = new BTreePage(rootPage.SelfIndex, BTreeFreeSpaceMap.AllocatePage());
			if (!_currentPage.IsLeaf)
				newPage.IsLeaf = false;


			rootPage.Childrens.Add(newPage.SelfIndex);
			rootPage.Childrens.Add(_currentPage.SelfIndex);

			var recordsPerPage = _currentPage.GetPresentRecords() / 2;

			for (var i = 0; i < recordsPerPage; i++) {
				newPage.InsertRecord(_currentPage.PopRecordAt(0, false));
			}
			if (!_currentPage.IsLeaf)
				for (var i = 0; i < recordsPerPage + 1; i++) {
					newPage.Childrens.Add(_currentPage.PopChildrenAt(0));
				}

			rootPage.InsertRecord(_currentPage.PopRecordAt(0, false));

			BtreeIO.WritePage(_currentPage.GetBTreePage(), _currentPage.SelfIndex);
			BtreeIO.WritePage(newPage.GetBTreePage(), newPage.SelfIndex);
			BtreeIO.WritePage(rootPage.GetBTreePage(), rootPage.SelfIndex);

			RootPage = rootPage;
			BTreeHeaderPage.RootIndex = RootPage.SelfIndex;
		}

		private void Merge() {


			var partPath = _path.Pop();
			var parentPage = partPath.ParentPage;
			var childrenIndex = partPath.ChildrenIndex;
			BTreePage leftBrother = null, rightBrother = null;
			if (childrenIndex == 0) {   //zrobic zapisy
				rightBrother = new BTreePage(BtreeIO.ReadPage(parentPage.Childrens[childrenIndex + 1]));

				var recordsToTransfer = rightBrother.GetPresentRecords();
				for (var i = 0; i < recordsToTransfer; i++) {
					_currentPage.InsertRecord(rightBrother.PopRecordAt(0, true));
				}
				if (!_currentPage.IsLeaf)
					for (var i = 0; i < recordsToTransfer + 1; i++) {
						_currentPage.Childrens.Add(rightBrother.PopChildrenAt(0));
					}

				parentPage.PopChildrenAt(childrenIndex + 1);

				_currentPage.InsertRecord(parentPage.Records[childrenIndex]);

				BTreeFreeSpaceMap.FreePage(rightBrother.SelfIndex);
				BtreeIO.WritePage(_currentPage.GetBTreePage(), _currentPage.SelfIndex);
			} else {
				leftBrother = new BTreePage(BtreeIO.ReadPage(parentPage.Childrens[childrenIndex - 1]));

				var recordsToTransfer = _currentPage.GetPresentRecords();
				for (var i = 0; i < recordsToTransfer; i++) {
					leftBrother.InsertRecord(_currentPage.PopRecordAt(0, true));
				}
				if (!_currentPage.IsLeaf)
					for (var i = 0; i < recordsToTransfer + 1; i++) {
						leftBrother.Childrens.Add(_currentPage.PopChildrenAt(0));
					}

				parentPage.PopChildrenAt(childrenIndex);

				leftBrother.InsertRecord(parentPage.Records[childrenIndex - 1]);

				BTreeFreeSpaceMap.FreePage(_currentPage.SelfIndex);
				BtreeIO.WritePage(leftBrother.GetBTreePage(), leftBrother.SelfIndex);
			}

			try {
				if (childrenIndex == 0) {
					parentPage.PopRecordAt(childrenIndex, false);
				} else {
					parentPage.PopRecordAt(childrenIndex - 1, false);
				}
				BtreeIO.WritePage(parentPage.GetBTreePage(), parentPage.SelfIndex);
			} catch (PageDeficitException ex) {

				_currentPage = parentPage;
				if (!Compensate())
					Merge();

			} catch (RootPageDeficitException ex) {
				BTreeFreeSpaceMap.FreePage(RootPage.SelfIndex);
				if (childrenIndex == 0) {
					RootPage = _currentPage;
					BTreeHeaderPage.RootIndex = _currentPage.SelfIndex;
				} else {
					RootPage = leftBrother;
					BTreeHeaderPage.RootIndex = leftBrother.SelfIndex;
				}

			}

		}

		public void Dispose() {

			BtreeIO.WritePage(RootPage.GetBTreePage(), RootPage.SelfIndex);
			BtreeIO.WritePage(BTreeHeader.GetHeaderPage(), 0);

			BTreeFreeSpaceMap.Dispose();
			DataFreeSpaceMap.Dispose();

			BtreeIO.Stream.Dispose();
			DataIO.Stream.Dispose();
		}

	}

	public struct PathPart {
		public BTreePage ParentPage;
		public int ChildrenIndex;
	}
}
