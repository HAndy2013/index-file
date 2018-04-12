using projekt2PlikIndeksowy.Tree;

namespace projekt2PlikIndeksowy {
    public class Static {

        public static readonly uint DParam = 3;

        public static readonly uint ChilMin = DParam + 1;
        public static readonly uint ChilMax = DParam * 2 + 1;
        public static readonly uint MemMin = DParam;
        public static readonly uint MemMax = DParam * 2;

        public static readonly uint PageSize = NodeRecord.NodeRecordSize * MemMax +
                                               sizeof(long) * ChilMax +
                                               sizeof(long) * 3 + sizeof(bool);

        public static readonly uint AllocationUnit = 8;
        public const uint FirstRootIndex = 1;

        public static string FilesPath = 
            "D:\\5SEMESTR\\SBD\\projekt\\projekt2\\projekt2PlikIndeksowy\\projekt2PlikIndeksowy\\files\\";







    }
}