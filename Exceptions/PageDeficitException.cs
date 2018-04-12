using System;
using projekt2PlikIndeksowy.Tree;

namespace projekt2PlikIndeksowy.Exceptions {
    public class PageDeficitException : Exception {

        public NodeRecord RemovedRecord;

        public PageDeficitException(NodeRecord removedRecord) {
            RemovedRecord = removedRecord;
        }
    }
}