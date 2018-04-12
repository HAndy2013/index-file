
using System;
using System.Collections.Generic;
using projekt2PlikIndeksowy.Exceptions;
using BC = System.BitConverter;

namespace projekt2PlikIndeksowy.Tree {
    public class DataPage {

        public int UsedSpace;
        public List<Record> Records;

        private int _byteIterator;
        public DataPage(byte[] page) {
            _byteIterator = 0;

            Records = new List<Record> { Capacity = Static.GetRecordsPerPage() };
            UsedSpace = BC.ToInt32(page, _byteIterator);
            _byteIterator += sizeof(int);

            for (var i = 0; i < UsedSpace; i++) {
                var key = BC.ToUInt32(page, _byteIterator);
                _byteIterator += sizeof(uint);
                var radius = BC.ToDouble(page, _byteIterator);
                _byteIterator += sizeof(double);
                var angle = BC.ToInt16(page, _byteIterator);
                _byteIterator += sizeof(short);

                Records.Add(new Record(key, radius, angle));
            }

        }

        public DataPage() {
            UsedSpace = 0;
            Records = new List<Record>();
        }

        public byte[] GetDataPage() {
            _byteIterator = 0;
            UsedSpace = Records.Count;

            var page = new byte[Static.PageSize];

            Array.Copy(BC.GetBytes(UsedSpace), 0, page, _byteIterator, sizeof(int));
            _byteIterator += sizeof(int);


            for (var i = 0; i < UsedSpace; i++) {
                Array.Copy(BC.GetBytes(Records[i].Key), 0, page, _byteIterator, sizeof(uint));
                _byteIterator += sizeof(uint);
                Array.Copy(BC.GetBytes(Records[i].Radius), 0, page, _byteIterator, sizeof(double));
                _byteIterator += sizeof(double);
                Array.Copy(BC.GetBytes(Records[i].Angle), 0, page, _byteIterator, sizeof(short));
                _byteIterator += sizeof(short);
            }

            return page;
        }

        public void AddRecord(Record record) {
            if (Records.Count >= Static.GetRecordsPerPage())
                throw new PageOverflowException();
            if(FindRecordByKey(record.Key) != null)
                throw new KeyConflictException(record);

            Records.Add(record);

            Records.Sort(delegate (Record r1, Record r2) {
                if (r1.Key > r2.Key)
                    return 1;
                if (r1.Key < r2.Key)
                    return -1;
                return 0;
            });
        }

        public Record FindRecordByKey(long key) {
            foreach (var record in Records) {
                if (record.Key == key)
                    return record;
            }
            return null;
        }

        public void RemoveRecord(long key) {
            int i;
            for (i = 0; i < Records.Count; i++) {
                if (Records[i].Key == key)
                    break;
            }
            Records.RemoveAt(i);
        }

    }
}