using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace PixelDa
{
    /// <summary>
    /// PixelDa 音乐工具：把 AI 返回的 ABC 记谱解析为方波(chiptune)音频。
    /// 原 Python 后端用 music21+symusic+8bit 音色库渲染 WAV，这里纯 C# 用方波合成器复刻
    /// 「8-bit chiptune」效果（实现常见 ABC 子集），并可保存 WAV + .abc 源文件、在编辑器内试听。
    /// </summary>
    public static class PixelDaMusicUtil
    {
        #region 常量

        /// <summary>采样率</summary>
        private const int SAMPLE_RATE = 44100;

        /// <summary>音名到半音偏移（自然音）</summary>
        private static readonly Dictionary<char, int> NOTE_SEMITONE = new Dictionary<char, int>
        {
            { 'C', 0 }, { 'D', 2 }, { 'E', 4 }, { 'F', 5 }, { 'G', 7 }, { 'A', 9 }, { 'B', 11 },
        };

        #endregion

        #region 内部结构

        /// <summary>解析出的单个音符/休止符事件</summary>
        private struct NoteEvent
        {
            /// <summary>MIDI 音高（休止符为 -1）</summary>
            public int midi;

            /// <summary>时长（秒）</summary>
            public float seconds;
        }

        #endregion

        #region 对外：合成

        /// <summary>
        /// 解析 ABC 并合成方波音频，返回单声道浮点采样
        /// </summary>
        public static float[] Synthesize(string abc, out int sampleRate)
        {
            sampleRate = SAMPLE_RATE;
            List<NoteEvent> events = ParseAbc(abc);
            return RenderSquareWave(events, sampleRate);
        }

        /// <summary>
        /// 合成并创建 AudioClip（用于编辑器试听）
        /// </summary>
        public static AudioClip SynthesizeToClip(string abc, string clipName)
        {
            float[] samples = Synthesize(abc, out int sr);
            if (samples.Length == 0) samples = new float[sr]; // 防空
            AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion

        #region 对外：保存

        /// <summary>
        /// 保存 ABC 源文本
        /// </summary>
        public static void SaveAbc(string abc, string path)
        {
            File.WriteAllText(path, abc, new UTF8Encoding(false));
        }

        /// <summary>
        /// 保存为 16bit PCM 单声道 WAV
        /// </summary>
        public static void SaveWav(float[] samples, int sampleRate, string path)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int byteRate = sampleRate * 2;
                int dataSize = samples.Length * 2;

                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataSize);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);             // PCM 块大小
                bw.Write((short)1);       // PCM
                bw.Write((short)1);       // 单声道
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)2);       // 块对齐
                bw.Write((short)16);      // 位深
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);

                foreach (float s in samples)
                {
                    short v = (short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue);
                    bw.Write(v);
                }
            }
        }

        #endregion

        #region ABC 解析

        /// <summary>
        /// 解析 ABC 记谱为音符事件序列（实现常见子集：L/M/Q/K 头 + 单音/休止符/时长/八度/临时升降号）
        /// </summary>
        private static List<NoteEvent> ParseAbc(string abc)
        {
            var events = new List<NoteEvent>();
            if (string.IsNullOrEmpty(abc)) return events;

            // 头部默认值
            double defaultLen = 1.0 / 8.0;   // L:
            double beatUnit = 1.0 / 4.0;     // Q 的拍单位
            double bpm = 120;                 // Q 的每分钟拍数
            var keyMap = BuildKeyAccidentals("C");

            var bodyLines = new List<string>();
            foreach (string raw in abc.Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;

                // 头部字段形如 "X:..."（单字母 + 冒号）
                if (line.Length >= 2 && char.IsLetter(line[0]) && line[1] == ':')
                {
                    char field = line[0];
                    string val = line.Substring(2).Trim();
                    switch (field)
                    {
                        case 'L': defaultLen = ParseFraction(val, defaultLen); break;
                        case 'Q': ParseTempo(val, ref beatUnit, ref bpm); break;
                        case 'K': keyMap = BuildKeyAccidentals(val); break;
                        case 'M': /* 拍号不直接影响时长计算，忽略 */ break;
                        default: break; // X/T/C/... 忽略
                    }
                    continue;
                }
                bodyLines.Add(line);
            }

            double quarterSeconds = 60.0 / bpm;

            foreach (string line in bodyLines)
            {
                ParseBodyLine(line, defaultLen, beatUnit, quarterSeconds, keyMap, events);
            }
            return events;
        }

        /// <summary>
        /// 解析一行曲体
        /// </summary>
        private static void ParseBodyLine(string line, double defaultLen, double beatUnit,
            double quarterSeconds, Dictionary<char, int> keyMap, List<NoteEvent> events)
        {
            int i = 0;
            int n = line.Length;
            while (i < n)
            {
                char c = line[i];

                // 行内注释 / 装饰 / 小节线等直接跳过
                if (c == '%') break;
                if (c == '|' || c == ':' || c == ']' || c == '[' || c == ' ' || c == '\t'
                    || c == '(' || c == ')' || c == '-' || c == '>' || c == '<' || c == '/')
                {
                    i++;
                    continue;
                }

                // 临时升降号
                int accidental = 0;
                while (i < n && (line[i] == '^' || line[i] == '_' || line[i] == '='))
                {
                    if (line[i] == '^') accidental += 1;
                    else if (line[i] == '_') accidental -= 1;
                    else accidental = 100; // 还原标记
                    i++;
                }
                if (i >= n) break;
                c = line[i];

                bool isRest = (c == 'z' || c == 'Z' || c == 'x');
                bool isNote = NOTE_SEMITONE.ContainsKey(char.ToUpper(c)) && char.IsLetter(c);

                if (!isRest && !isNote)
                {
                    i++;
                    continue;
                }

                int midi = -1;
                if (isNote)
                {
                    char upper = char.ToUpper(c);
                    int semitone = NOTE_SEMITONE[upper];
                    // 大写=C4(60) 区；小写=高八度
                    int octaveBase = char.IsUpper(c) ? 60 : 72;
                    midi = octaveBase + semitone;

                    // 应用调号
                    if (accidental == 100)
                    {
                        // 还原：不加调号
                    }
                    else if (accidental != 0)
                    {
                        midi += accidental;
                    }
                    else if (keyMap.TryGetValue(upper, out int keyAcc))
                    {
                        midi += keyAcc;
                    }
                }
                i++;

                // 八度修饰符
                while (i < n && (line[i] == '\'' || line[i] == ','))
                {
                    if (line[i] == '\'') midi += 12;
                    else if (line[i] == ',') midi -= 12;
                    i++;
                }

                // 时长：数字 + 斜杠
                double lengthMul = ParseNoteLength(line, ref i);

                double noteLenWhole = defaultLen * lengthMul; // 占全音符比例
                double seconds = (noteLenWhole / beatUnit) * quarterSeconds;
                if (seconds <= 0) seconds = 0.01;

                events.Add(new NoteEvent { midi = isRest ? -1 : midi, seconds = (float)seconds });
            }
        }

        /// <summary>
        /// 解析紧跟音符的时长修饰（如 2、/2、3/2、/）
        /// </summary>
        private static double ParseNoteLength(string line, ref int i)
        {
            int n = line.Length;
            double numerator = 0;
            bool hasNum = false;
            while (i < n && char.IsDigit(line[i]))
            {
                numerator = numerator * 10 + (line[i] - '0');
                hasNum = true;
                i++;
            }
            double mul = hasNum ? numerator : 1.0;

            // 斜杠表示除法，可叠加 // = 除以4
            while (i < n && line[i] == '/')
            {
                i++;
                double denom = 0;
                bool hasDenom = false;
                while (i < n && char.IsDigit(line[i]))
                {
                    denom = denom * 10 + (line[i] - '0');
                    hasDenom = true;
                    i++;
                }
                mul /= hasDenom ? denom : 2.0;
            }
            return mul;
        }

        /// <summary>
        /// 解析形如 "1/8" 的分数，失败返回默认值
        /// </summary>
        private static double ParseFraction(string s, double fallback)
        {
            try
            {
                s = s.Trim();
                if (s.Contains("/"))
                {
                    string[] parts = s.Split('/');
                    double a = double.Parse(parts[0], CultureInfo.InvariantCulture);
                    double b = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    return b == 0 ? fallback : a / b;
                }
                return double.Parse(s, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// 解析 Q 字段，如 "1/4=120" 或 "120"
        /// </summary>
        private static void ParseTempo(string val, ref double beatUnit, ref double bpm)
        {
            try
            {
                val = val.Trim();
                if (val.Contains("="))
                {
                    string[] parts = val.Split('=');
                    beatUnit = ParseFraction(parts[0], beatUnit);
                    bpm = double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
                }
                else
                {
                    bpm = double.Parse(val, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                // 保留默认
            }
        }

        /// <summary>
        /// 根据大调调号生成各音名的升降半音表
        /// </summary>
        private static Dictionary<char, int> BuildKeyAccidentals(string key)
        {
            var map = new Dictionary<char, int>();
            if (string.IsNullOrEmpty(key)) return map;

            // 取调号主音（首字母 + 可能的 # / b），忽略 mode/装饰
            string norm = key.Trim().ToUpper();
            // 升号调：G D A E B F# C#
            string[] sharpOrder = { "F", "C", "G", "D", "A", "E", "B" };
            string[] flatOrder = { "B", "E", "A", "D", "G", "C", "F" };

            int sharps = 0, flats = 0;
            if (norm.StartsWith("G")) sharps = 1;
            else if (norm.StartsWith("D")) sharps = 2;
            else if (norm.StartsWith("A") && !norm.StartsWith("AB")) sharps = 3;
            else if (norm.StartsWith("E") && !norm.StartsWith("EB")) sharps = 4;
            else if (norm.StartsWith("B") && !norm.StartsWith("BB")) sharps = 5;
            else if (norm.StartsWith("F#")) sharps = 6;
            else if (norm.StartsWith("C#")) sharps = 7;
            else if (norm.StartsWith("F") && !norm.StartsWith("F#")) flats = 1;
            else if (norm.StartsWith("BB")) flats = 2;
            else if (norm.StartsWith("EB")) flats = 3;
            else if (norm.StartsWith("AB")) flats = 4;
            else if (norm.StartsWith("DB")) flats = 5;
            else if (norm.StartsWith("GB")) flats = 6;

            for (int i = 0; i < sharps; i++) map[sharpOrder[i][0]] = 1;
            for (int i = 0; i < flats; i++) map[flatOrder[i][0]] = -1;
            return map;
        }

        #endregion

        #region 方波渲染

        /// <summary>
        /// 把音符事件渲染为方波采样（带简单淡入淡出包络防爆音）
        /// </summary>
        private static float[] RenderSquareWave(List<NoteEvent> events, int sampleRate)
        {
            int total = 0;
            foreach (var e in events)
            {
                total += Mathf.Max(1, Mathf.RoundToInt(e.seconds * sampleRate));
            }
            if (total <= 0) return Array.Empty<float>();

            float[] buffer = new float[total];
            int pos = 0;
            const float amplitude = 0.25f;
            int fade = Mathf.RoundToInt(0.005f * sampleRate); // 5ms 淡入淡出

            foreach (var e in events)
            {
                int len = Mathf.Max(1, Mathf.RoundToInt(e.seconds * sampleRate));
                if (e.midi >= 0)
                {
                    float freq = 440f * Mathf.Pow(2f, (e.midi - 69) / 12f);
                    float period = sampleRate / freq;
                    for (int s = 0; s < len && pos < total; s++, pos++)
                    {
                        float phase = (s % period) / period;
                        float val = phase < 0.5f ? amplitude : -amplitude;

                        // 包络
                        float env = 1f;
                        if (s < fade) env = (float)s / fade;
                        else if (s > len - fade) env = (float)(len - s) / fade;
                        buffer[pos] = val * env;
                    }
                }
                else
                {
                    // 休止符：静音推进
                    pos += len;
                    if (pos > total) pos = total;
                }
            }
            return buffer;
        }

        #endregion
    }
}
