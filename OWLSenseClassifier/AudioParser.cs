using FftSharp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OWLSenseClassifier
{
    internal class AudioParser
    {

        private String fileNamePCM;
        private String fileNameMETA;
        public const int SAMPLE_RATE = 8000;
        public const int PCM_LENGTH = SAMPLE_RATE * 2;

        private List<InferenceData> inferenceList = new List<InferenceData>();
        private int currentPosition = 0;

        public AudioParser(string fileNamePCM, string fileNameMETA)
        {
            this.fileNamePCM = fileNamePCM;
            this.fileNameMETA = fileNameMETA;   
            LoadFile();
        }

        private void LoadFile()
        {
            inferenceList = new List<InferenceData>();
            currentPosition = 0;
            using (RawSourceWaveStream rawReader = new RawSourceWaveStream(File.OpenRead(fileNamePCM), new WaveFormat(SAMPLE_RATE, 1)))
            {
                using (FileStream metadataStream = new FileStream(fileNameMETA, FileMode.Open))
                {
                    int read = 1;
                    while (read > 0)
                    {
                        byte[] buffer = new byte[PCM_LENGTH];
                        read = rawReader.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            InferenceData data = new InferenceData(buffer, GetCurrentMetaData(metadataStream));
                            inferenceList.Add(data);
                        }
                    }
                }
            }
        }

        private void GetFileWriter(string key)
        {
            WaveFileWriter writer;

            //writer = new WaveFileWriter(path + key, new WaveFormat(SAMPLE_RATE, 1));
        }



        private byte[]  GetCurrentMetaData(FileStream metadataStream)
        {
            byte[] buffer = new byte[InferenceData.METADATALENGTH];
            metadataStream.Read(buffer, 0, InferenceData.METADATALENGTH);
            return buffer;
        }


        public string GetStatus()
        {
            return (currentPosition + 1) + "/" + inferenceList.Count;
        }

        public InferenceData GetCurrent()
        {
            return inferenceList[currentPosition];
        }
        public InferenceData GetNext()
        {
            currentPosition++;
            if (currentPosition >= inferenceList.Count)
                currentPosition = inferenceList.Count -1;
            return inferenceList[currentPosition];
        }

        public InferenceData GetPrevious()
        {
            currentPosition--;
            if (currentPosition < 0)
                currentPosition = 0;
            return inferenceList[currentPosition];
        }


        public void WriteOutputFiles()
        {

        }


        public void WriteSample(string key)
        {
            RawSourceWaveStream source = currentRecord.GetAudio();
            long position = source.Position;
            source.Position = 0;
            byte[] buffer = new byte[PCM_LENGTH];
            source.Read(buffer, 0, buffer.Length);
            WaveFileWriter writer;
        }


    }
}
