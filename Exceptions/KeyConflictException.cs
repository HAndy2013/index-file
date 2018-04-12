using System;
using projekt2PlikIndeksowy.Tree;

namespace projekt2PlikIndeksowy.Exceptions {
    public class KeyConflictException : Exception {

        public Record Record;
        public KeyConflictException(Record record) {
            Record = record;
        }
    }
}