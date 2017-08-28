using System.Text;

namespace Twingly.Gearman
{
    public delegate byte[] DataSerializer<T>(T data) where T : class;
    public delegate T DataDeserializer<T>(byte[] data) where T : class;

    public static class Serializers
    {
        public static byte[] UTF8StringSerialize(string data)
        {
            if (data == null)
                return null;

            return Encoding.UTF8.GetBytes(data);
        }

        public static string UTF8StringDeserialize(byte[] data)
        {
            if (data == null)
                return null;

            return Encoding.UTF8.GetString(data);
        }
    }
}
