


namespace projekt2PlikIndeksowy.Tree {
    public class Record {

        public static int ByteSize = sizeof(double) + sizeof(short);



        public double Radius;
        public short Angle;

        public Record(double radius, short angle) {
            Radius = radius;
            Angle = angle;

        }

    }
}