using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using projekt2PlikIndeksowy.IO;


namespace projekt2PlikIndeksowy.Tree {
    public class FreeSpaceMap : IDisposable {

        private BufferedIO bufferedIo;
        private List<long> _map = new List<long>();

        private long _indexEnd;

        public bool FreeSpace;
        public FreeSpaceMap(Stream stream, long indexEnd) {
            bufferedIo = new BufferedIO(stream, sizeof(long));
            _indexEnd = indexEnd;

            var bytes = bufferedIo.ReadAll();
            for (var i = 0; i < bytes.Length; i += 8) {
                _map.Add(BitConverter.ToInt64(bytes, i));
            }

            
        }

        public long AllocatePage() {
            if (_map.Count == 0)
                ExtendSpace();

            var index = _map[0];
            _map.RemoveAt(0);
            return index;
        }

        public void FreePage(long index) {
            _map.Add(index);
        }

        public void ExtendSpace() {
            _map.Add(_indexEnd);
            _indexEnd += 1;
        }

        public static bool BitCheck(byte bt, int offset) {
            return (bt & (1 << offset)) != 0;
        }

        public static void BitSet(ref byte bt, int offset) {
            bt |= (byte)(1 << offset);
        }

        public static void BitZero(ref byte bt, int offset) {
            bt &= (byte)~(1 << offset);
        }

        public void Dispose() {
            var all = new byte[_map.Count * sizeof(long)];

            for (var i = 0; i < _map.Count; i++) {
                Array.Copy(BitConverter.GetBytes(_map[i]), 0, all, i * sizeof(long), sizeof(long));
            }

            bufferedIo.SaveAll(all);

            bufferedIo.Stream.Dispose();
        }
    }
}