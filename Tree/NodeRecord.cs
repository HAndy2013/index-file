


namespace projekt2PlikIndeksowy.Tree {
    public class NodeRecord {

        public static uint NodeRecordSize = 2 * sizeof(long);   //sizeof(Key) + sizeof(RecordIndex)

        public long Key;
        public long RecordIndex;

        public NodeRecord(long key, long recordIndex) {
            Key = key;
            RecordIndex = recordIndex;
        }

    }
}