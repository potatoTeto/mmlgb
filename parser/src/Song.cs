using System.Text;

namespace MMLGB
{
    public class Song
    {
        private string _filename;

        private List<List<int>> _channel;

        private List<List<int>> _waveData;
        private Dictionary<int, int> _waveMap;

        private List<List<int>> _macroData;
        private Dictionary<int, int> _macroMap;

        public enum CMD
        {
            T_LENGTH,
            T_OCTAVE,
            T_OCT_UP,
            T_OCT_DOWN,
            T_VOL,
            T_ENV,
            T_WAVEDUTY,
            T_PAN,
            T_PORTAMENTO,
            T_VIBRATO,
            T_VIBRATO_DELAY,
            T_REP_START,
            T_REP_END,
            T_LOOP,
            T_PITCH_OFFSET,
            T_TEMPO,
            T_NOISE_STEP,
            T_WAVE,
            T_MACRO,
            T_EOF,
            T_C,
            T_Cs,
            T_D,
            T_Ds,
            T_E,
            T_F,
            T_Fs,
            T_G,
            T_Gs,
            T_A,
            T_As,
            T_B,
            T_REST,
            T_WAIT
        }

        public Song()
        {
            _waveData = new List<List<int>>();
            _waveMap = new Dictionary<int, int>();

            _macroData = new List<List<int>>();
            _macroMap = new Dictionary<int, int>();

            _channel = new List<List<int>>();
            for (int i = 0; i < 4; ++i)
            {
                _channel.Add(new List<int>());
            }
        }

        public void AddWaveData(int id, List<int> data)
        {
            _waveMap[id] = _waveData.Count;
            _waveData.Add(data);
        }

        public int? GetWaveIndex(int id)
        {
            return _waveMap.TryGetValue(id, out var index) ? index : (int?)null;
        }

        public void AddMacroData(int id, List<int> data)
        {
            _macroMap[id] = _macroData.Count;
            _macroData.Add(data);
        }

        public List<int> GetMacroData(int id)
        {
            return _macroData[GetMacroIndex(id)];
        }

        public int GetMacroIndex(int id)
        {
            return _macroMap[id];
        }

        public void AddData(int chan, int value)
        {
            _channel[chan].Add(value);
        }

        public void AddData(int chan, IEnumerable<int> values)
        {
            _channel[chan].AddRange(values);
        }

        public void AddData(bool[] active, int value)
        {
            for (int i = 0; i < 4; ++i)
            {
                if (active[i])
                {
                    AddData(i, value);
                }
            }
        }

        public void AddData(bool[] active, IEnumerable<int> values)
        {
            for (int i = 0; i < 4; ++i)
            {
                if (active[i])
                {
                    AddData(i, values);
                }
            }
        }

        public List<int> GetChannel(int i)
        {
            return _channel[i];
        }

        public List<int> GetData()
        {
            List<int> data = new List<int>();

            int waveStart = (4 + 1 + _macroData.Count) * 2;
            int pos = waveStart + _waveData.Count * 16;
            int[] macroStart = new int[_macroData.Count];
            for (int i = 0; i < macroStart.Length; ++i)
            {
                macroStart[i] = pos;
                pos += _macroData[i].Count + 1;
            }

            int c1Start = pos;
            int c2Start = c1Start + _channel[0].Count;
            int c3Start = c2Start + _channel[1].Count;
            int c4Start = c3Start + _channel[2].Count;

            data.Add(c1Start & 0xFF);
            data.Add(c1Start >> 8);
            data.Add(c2Start & 0xFF);
            data.Add(c2Start >> 8);
            data.Add(c3Start & 0xFF);
            data.Add(c3Start >> 8);
            data.Add(c4Start & 0xFF);
            data.Add(c4Start >> 8);

            data.Add(waveStart & 0xFF);
            data.Add(waveStart >> 8);

            foreach (int macro in macroStart)
            {
                data.Add(macro & 0xFF);
                data.Add(macro >> 8);
            }

            foreach (var samples in _waveData)
            {
                for (int j = 0; j < 32; j += 2)
                {
                    int value = (samples[j] << 4) | samples[j + 1];
                    data.Add(value);
                }
            }

            foreach (var macro in _macroData)
            {
                data.AddRange(macro);
                data.Add((int)CMD.T_EOF);
            }

            foreach (var ch in _channel)
            {
                data.AddRange(ch);
            }

            return data;
        }

        public void SetFileName(string name)
        {
            _filename = name;
        }

        public string EmitC()
        {
            string[] parts = _filename.Split('.');
            string id = parts[0];
            string idUpper = id.ToUpper();
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"#ifndef {idUpper}_H");
            sb.AppendLine($"#define {idUpper}_H\n");
            sb.AppendLine($"const UBYTE {id}_data[] = {{");

            foreach (int i in GetData())
            {
                sb.AppendLine($"\t{i}U,");
            }

            sb.AppendLine("};\n");
            sb.AppendLine("#endif");

            return sb.ToString();
        }

        public string EmitASM()
        {
            string[] parts = _filename.Split('.');
            string id = parts[0];
            string idLower = id.ToLower();
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($".globl _{idLower}_data");
            sb.AppendLine($"_{idLower}_data:");

            foreach (int i in GetData())
            {
                sb.AppendLine($"\t.db 0x{i:X2}");
            }

            return sb.ToString();
        }
    }
}
