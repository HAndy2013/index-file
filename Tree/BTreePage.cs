using System;
using System.Collections.Generic;
using projekt2PlikIndeksowy.Exceptions;
using BC = System.BitConverter;


namespace projekt2PlikIndeksowy.Tree {
    public class BTreePage {

        public long ParentIndex;    //if 0 pointer == null
        public long SelfIndex;
        private long _presentRecords;
        public bool IsLeaf;

        public List<NodeRecord> Records;
        public List<long> Childrens;

        private int _byteIterator;

        public BTreePage(byte[] pageData) {
            _byteIterator = 0;



            Records = new List<NodeRecord> { Capacity = (int)Static.MemMax };
            Childrens = new List<long> { Capacity = (int)Static.ChilMax };

            ParentIndex = BC.ToInt64(pageData, _byteIterator);
            _byteIterator += sizeof(long);
            SelfIndex = BC.ToInt64(pageData, _byteIterator);
            _byteIterator += sizeof(long);
            _presentRecords = BC.ToInt64(pageData, _byteIterator);
            _byteIterator += sizeof(long);
            IsLeaf = BC.ToBoolean(pageData, _byteIterator);
            _byteIterator += sizeof(bool);

            for (var i = 0; i < _presentRecords; i++) {

                var key = BC.ToInt64(pageData, _byteIterator);
                _byteIterator += sizeof(long);
                var pageIndex = BC.ToInt64(pageData, _byteIterator);
                _byteIterator += sizeof(long);

                Records.Add(new NodeRecord(key, pageIndex));
            }
            if (!IsLeaf)
                for (var i = 0; i < _presentRecords + 1; i++) {
                    Childrens.Add(BC.ToInt64(pageData, _byteIterator));
                    _byteIterator += sizeof(long);
                }

        }
        public BTreePage(long parentIndex, long selfIndex) {

            ParentIndex = parentIndex;
            SelfIndex = selfIndex;
            IsLeaf = true;
            _presentRecords = 0;

            Records = new List<NodeRecord> { Capacity = (int)Static.MemMax };
            Childrens = new List<long> { Capacity = (int)Static.ChilMax };

        }
        public byte[] GetBTreePage() {
            _byteIterator = 0;
            var page = new byte[Static.PageSize];
            _presentRecords = Records.Count;

            Array.Copy(BC.GetBytes(ParentIndex), 0, page, _byteIterator, sizeof(long));
            _byteIterator += sizeof(long);
            Array.Copy(BC.GetBytes(SelfIndex), 0, page, _byteIterator, sizeof(long));
            _byteIterator += sizeof(long);
            Array.Copy(BC.GetBytes(_presentRecords), 0, page, _byteIterator, sizeof(long));
            _byteIterator += sizeof(long);
            Array.Copy(BC.GetBytes(IsLeaf), 0, page, _byteIterator, sizeof(bool));
            _byteIterator += sizeof(bool);

            for (var i = 0; i < _presentRecords; i++) {

                var key = BC.GetBytes(Records[i].Key);
                var pageIndex = BC.GetBytes(Records[i].RecordIndex);

                Array.Copy(key, 0, page, _byteIterator, sizeof(long));
                _byteIterator += sizeof(long);
                Array.Copy(pageIndex, 0, page, _byteIterator, sizeof(long));
                _byteIterator += sizeof(long);
            }
            if (!IsLeaf)
                for (var i = 0; i < _presentRecords + 1; i++) {

                    Array.Copy(BC.GetBytes(Childrens[i]), 0, page, _byteIterator, sizeof(long));
                    _byteIterator += sizeof(long);
                }

            return page;
        }

        public void InsertRecord(NodeRecord nodeRecord) {

            if (Records.Count == 0 || nodeRecord.Key < Records[0].Key)
                Records.Insert(0, nodeRecord);
            else if (nodeRecord.Key > Records[Records.Count - 1].Key)
                Records.Insert(Records.Count, nodeRecord);
            else {
                for (var i = 1; i < Records.Count; i++) {
                    if (nodeRecord.Key < Records[i].Key) {
                        Records.Insert(i, nodeRecord);
                        break;
                    }
                }
            }

            if (Records.Count > Static.MemMax)
                throw new PageOverflowException();
        }

        public NodeRecord RemoveRecord(long key) {
            if (SelfIndex == BTreeHeaderPage.RootIndex && Records.Count == 0)
                throw new RootPageDeficitException();
            NodeRecord record = null;
            for (var i = 0; i < Records.Count; i++) {
                if (key == Records[i].Key) {
                    record = Records[i];
                    Records.RemoveAt(i);
                }
            }
            
            if (SelfIndex != BTreeHeaderPage.RootIndex && Records.Count < Static.MemMin)
                throw new PageDeficitException(record);

            return record;
        }

        public NodeRecord PopRecordAt(int index, bool mergeMode) {
            var record = Records[index];
            Records.RemoveAt(index);

            if (!mergeMode) {
                if (SelfIndex == BTreeHeaderPage.RootIndex && Records.Count == 0)
                    throw new RootPageDeficitException();
                if (SelfIndex != BTreeHeaderPage.RootIndex && Records.Count < Static.MemMin)
                    throw new PageDeficitException(record);
            }
            return record;
        }

        public NodeRecord SwapRecord(NodeRecord input, NodeRecord toSwap) {

            for (var i = 0; i < GetPresentRecords(); i++) {
                if (Records[i].Key == toSwap.Key) {
                    var temp = Records[i];
                    Records[i] = input;
                    return temp;
                }
            }

            throw new ErrorException();
        }

        public long PopChildrenAt(int index) {
            var children = Childrens[index];
            Childrens.RemoveAt(index);
            return children;
        }

        public NodeRecord FindRecord(long key) {
            int start = 0, end = Records.Count - 1;

            if (Records.Count == 0)
                return null;

            while (start != end) {
                var middle = (start + end) / 2;

                if (key == Records[middle].Key)
                    return Records[middle];
                if (key > Records[middle].Key)
                    start = middle + 1;
                else {
                    end = middle;
                }
            }
            if (key == Records[start].Key)
                return Records[start];

            return null;
        }

        public int GetPresentRecords() {
            return Records.Count;
        }
    }
}