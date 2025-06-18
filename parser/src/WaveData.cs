namespace MMLGB
{
    public class WaveData
    {
        private int[] _data;

        public WaveData()
        {
            _data = new int[32];
        }

        public WaveData(int[] data)
        {
            SetData(data);
        }

        public int[] GetData()
        {
            return _data;
        }

        public void SetData(int[] data)
        {
            if (data.Length != 32)
            {
                throw new ArgumentException($"Array contains {data.Length} samples. Expected 32.");
            }

            _data = data;
        }
    }
}
