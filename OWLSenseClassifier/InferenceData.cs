using FftSharp;
using NAudio.Mixer;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OWLSenseClassifier
{
    internal class InferenceData
    {
        public ushort SensorId;
        public uint Epoch;
        public string Label;
        public string ClassifiedLabel;
        public const int LABEL_LENGTH = 128;
        public short Probability;
        public float TemperatureC;
        public float Latitude;
        public float Longitude;
        private byte[] audioWavBuffer;

        public int testPosition;

        public const int METADATALENGTH = sizeof(ushort) + sizeof(uint) + LABEL_LENGTH + sizeof(byte) + sizeof(float) + sizeof(float) + sizeof(float);

        public InferenceData(byte[] audio, byte[] metadata)
        {
            audioWavBuffer = audio;
            ReadMetadata(metadata);
        }

        public InferenceData()
        {

            audioWavBuffer = new byte[AudioParser.PCM_LENGTH];
        }

        public RawSourceWaveStream GetAudio()
        {
            return new RawSourceWaveStream(audioWavBuffer, 0, AudioParser.PCM_LENGTH, new WaveFormat(AudioParser.SAMPLE_RATE, 1));
        }

        private void ReadMetadata(byte[] buffer)
        {
            int index = 0;
            SensorId = BitConverter.ToUInt16(buffer, index);
            index += sizeof(ushort);
            Epoch = BitConverter.ToUInt32(buffer, index);
            index += sizeof(uint);
            StringBuilder sb = new StringBuilder();
            for(int i = index; i < LABEL_LENGTH + index; i++)
            {
                if (buffer[i] == '\0')
                    break;
                sb.Append(Convert.ToChar(buffer[i]));
            }
            Label = sb.ToString();
            index += LABEL_LENGTH;
            Probability = (byte)buffer[index];
            index += sizeof(byte);
            TemperatureC = BitConverter.ToSingle(buffer, index);
            index += sizeof(float);
            Latitude = BitConverter.ToSingle(buffer, index);
            index += sizeof(float);
            Longitude = BitConverter.ToSingle(buffer, index);
            index += sizeof(float);
        }

        public double[] ReadWavMono(double multiplier = 16_000)
        {
            using (var audioWav =new RawSourceWaveStream(audioWavBuffer, 0, AudioParser.PCM_LENGTH, new WaveFormat(AudioParser.SAMPLE_RATE, 1)))
            {
                Pcm16BitToSampleProvider afr = new Pcm16BitToSampleProvider(audioWav);
                int sampleRate = afr.WaveFormat.SampleRate;
                int bytesPerSample = afr.WaveFormat.BitsPerSample / 8;
                int sampleCount = (int)audioWav.Length;//SampleRate;// (int)(afr.Length / bytesPerSample);
                int channelCount = afr.WaveFormat.Channels;
                var audio = new List<double>(sampleCount);
                var buffer = new float[sampleRate * channelCount];
                int samplesRead = 0;
                while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
                    audio.AddRange(buffer.Take(samplesRead).Select(x => x * multiplier));
                return audio.ToArray();
            }
        }
    }
}
