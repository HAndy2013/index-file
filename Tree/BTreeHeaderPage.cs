using System;
using System.Collections.Generic;
using BC = System.BitConverter;

namespace projekt2PlikIndeksowy.Tree {

    public class BTreeHeaderPage {

        public static long RootIndex;


        private int _byteIterator;

        public BTreeHeaderPage(byte[] headerPage) {

            
            RootIndex = BC.ToInt64(headerPage, _byteIterator);
            _byteIterator += sizeof(long);



        }

        public BTreeHeaderPage(long rootIndex) {

            RootIndex = rootIndex;

        }

        public byte[] GetHeaderPage() {
            _byteIterator = 0;
            var headerPage = new byte[Static.PageSize];

            Array.Copy(BC.GetBytes(RootIndex), 0, headerPage, _byteIterator, sizeof(long));
            _byteIterator += sizeof(long);

            return headerPage;
        }



    }
}