namespace projekt2PlikIndeksowy.Tree {
    public class DataRecord {

        public Record Record;
        public long Key;

        public DataRecord(long key, double radius, short angle) {
            Record = new Record(radius, angle);
            Key = key;
        }

    }
}