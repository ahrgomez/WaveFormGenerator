using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using NAudio.Wave;
using System.Drawing;

namespace TracksporterCore.Audio
{
	public static class WaveformGenerator
	{
		#region Public Members

		public static float[] GenerateMp3Wave(string uri)
		{
			float[] result;
			using (FileStream inputStream = new FileStream(uri, FileMode.Open, FileAccess.Read))
			{
				using (Mp3FileReader mp3 = new Mp3FileReader(inputStream))
				{
					int bytesPerBeat = Math.Abs((176400 / 1024)) * 1024;
					int numsBeat = (int)Math.Abs(mp3.TotalTime.TotalSeconds);
					int divisions = 0;
					while (numsBeat < 5000)
					{
						numsBeat *= 2;
						divisions += 2;
					}

					result = GenerateWave(mp3, Math.Abs(((bytesPerBeat / divisions) / 1024)) * 1024);
				}
			}

			return result;
		}

		public static float[] GenerateWavWave(string uri)
		{
			float[] result;
			using (FileStream inputStream = new FileStream(uri, FileMode.Open))
			{
				using (WaveFileReader wave = new WaveFileReader(inputStream))
				{
					int bytesPerSecond = Math.Abs((wave.WaveFormat.AverageBytesPerSecond / 1024)) * 1024;
					int bytesPerBeat = bytesPerSecond;
					while (wave.TotalTime.TotalSeconds / bytesPerBeat < 5000)
					{
						bytesPerBeat /= 2;
					}

					result = GenerateWave(wave, bytesPerBeat);
				}
			}

			return result;
		}

		public static float[] GenerateAiffWave(string uri)
		{
			float[] result;
			using (FileStream inputStream = new FileStream(uri, FileMode.Open))
			{
				using (AiffFileReader aiff = new AiffFileReader(inputStream))
				{
					int bytesPerSecond = Math.Abs((aiff.WaveFormat.AverageBytesPerSecond / 1024)) * 1024;
					int bytesPerBeat = bytesPerSecond;
					while (aiff.TotalTime.TotalSeconds / bytesPerBeat < 5000)
					{
						bytesPerBeat /= 2;
					}
					result = GenerateWave(aiff, bytesPerBeat);
				}
			}

			return result;
		}


		#endregion

		#region Private Members

		private static float[] GenerateWave(Stream waveStream, int bytesPerBeat)
		{
			List<float> wave = new List<float>();
			long readed = 0;
			while (readed != waveStream.Length)
			{
				byte[] second = new byte[(readed + bytesPerBeat) > waveStream.Length ? waveStream.Length - readed : bytesPerBeat];
				readed += waveStream.Read(second, 0, second.Length);
				List<float> r = new List<float>();

				for (var i = 0; i < second.Length; i += 4)
				{
					r.Add((float)BitConverter.ToInt16(second, i) + (float)BitConverter.ToInt16(second, i + 2));
				}

				float maxValue = float.MinValue;

				foreach (float f in r)
				{
					if (f > maxValue) maxValue = f;
				}

				wave.Add(maxValue);

				if (second.Length < bytesPerBeat) break;
			}
			return wave.ToArray();
		}

		private static float[] GetData(string uri)
		{
			try
			{
				float[] points = new float[0];

				if (Path.GetExtension(uri).Equals(".mp3"))
				{
					points = GenerateMp3Wave(uri);
				}
				else if (Path.GetExtension(uri).Equals(".wav"))
				{
					points = GenerateWavWave(uri);
				}
				else if (Path.GetExtension(uri).Equals(".aiff"))
				{
					points = GenerateAiffWave(uri);
				}

				if (points.Length == 0) return null;

				float maxValue = float.MinValue;
				float minValue = float.MaxValue;

				float[] positive = points.Where(v => v >= 0).ToArray();

				foreach (float v in positive)
				{
					if (v > maxValue) maxValue = v;
					if (v < minValue) minValue = v;
				}

				float[] result = new float[positive.Length];

				for (int i = 0; i < positive.Length; i++)
				{
					result[i] = positive[i] / maxValue;
				}

				while (result.Length > 4000)
				{
					int index = 0;
					float[] newResult = new float[result.Length / 2];
					for (int i = 1; i < result.Length; i += 2)
					{
						string s = result[i - 1] > result[i] ? result[i - 1].ToString("0.0") : result[i].ToString("0.0");
						newResult[index] = float.Parse(s);
						index++;
					}

					result = newResult;
				}

				return result;
			}
			catch {
				return new float[0];
			}
		}

		#endregion
	}
}