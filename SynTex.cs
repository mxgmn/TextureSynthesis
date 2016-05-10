/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Xml;
using System.Linq;
using System.Drawing;
using System.ComponentModel;
using System.Collections.Generic;

class Program
{
	static void Main(string[] args)
	{
		var xdoc = new XmlDocument();
		xdoc.Load("samples.xml");
		int pass = 1;

		for (var xnode = xdoc.FirstChild.FirstChild; xnode != null; xnode = xnode.NextSibling)
		{
			string name = xnode.Get("name", "");
			int K = xnode.Get("K", 1), N = xnode.Get("N", 1), OW = xnode.Get("OW", 32), OH = xnode.Get("OH", 32);
			bool indexed = xnode.Get("indexed", true);
			double t = xnode.Get("temperature", 1.0);

			Bitmap sample = new Bitmap("Samples/" + name + ".bmp");
			List<int>[] similaritySets = null;

			int[] sampleArray = new int[sample.Width * sample.Height];
			for (int j = 0; j < sample.Width * sample.Height; j++) sampleArray[j] = sample.GetPixel(j % sample.Width, j / sample.Width).ToArgb();
			
			if (K > 0)
			{
				Console.WriteLine("< " + name);
				similaritySets = Analysis(sampleArray, sample.Width, sample.Height, K, N, indexed);
			}

			for (int i = 0; i < xnode.Get("screenshots", 1); i++)
			{
				Console.WriteLine("> " + name + " " + i);

				int[] outputArray = K > 0 ? CoherentSynthesis(sampleArray, sample.Width, sample.Height, similaritySets, N, OW, OH, t, indexed) :
					FullSynthesis(sampleArray, sample.Width, sample.Height, N, OW, OH, t, indexed); 

				Bitmap output = new Bitmap(OW, OH);
				for (int j = 0; j < OW * OH; j++) output.SetPixel(j % OW, j / OW, Color.FromArgb(outputArray[j]));
				output.Save(pass.ToString() + " " + name + " " + indexed.ToString() + " K=" + K + " N=" + N + " t=" + t + " " + i + ".bmp");
			}

			pass++;
		}
	}

	static List<int>[] Analysis(int[] bitmap, int width, int height, int K, int N, bool indexed)
	{
		int area = width * height;
		var result = new List<int>[area];
		var points = new List<int>();
		for (int i = 0; i < area; i++) points.Add(i);

		double[] similarities = new double[area * area];
		for (int i = 0; i < area; i++) for (int j = 0; j < area; j++)
				similarities[i * area + j] = similarities[j * area + i] != 0 ? similarities[j * area + i] : 
					Similarity(i, bitmap, width, height, j, bitmap, width, height, N, null, indexed);

		for (int i = 0; i < area; i++)
		{
			result[i] = new List<int>();
			var copy = new List<int>(points);

			result[i].Add(i);
			copy.Remove(i);

			for (int k = 1; k < K; k++)
			{
				double max = -10000;
				int argmax = -1;

				foreach (int p in copy)
				{
					double s = similarities[i * area + p];
					if (s > max)
					{
						max = s;
						argmax = p;
					}
				}

				result[i].Add(argmax);
				copy.Remove(argmax);
			}
		}

		return result;
	}

	static int[] CoherentSynthesis(int[] sample, int SW, int SH, List<int>[] sets, int N, int OW, int OH, double t, bool indexed)
	{
		int[] result = new int[OW * OH];
		int?[] origins = new int?[OW * OH];
		Random random = new Random();

		for (int i = 0; i < OW * OH; i++)
		{
			int x = i % OW, y = i / OW;
			var candidates = new Dictionary<int, double>();
			bool[,] mask = new bool[SW, SH];

			for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
				{
					int sx = (x + dx + OW) % OW, sy = (y + dy + OH) % OH;
					int? origin = origins[sy * OW + sx];
					if ((dx != 0 || dy != 0) && origin != null)
					{
						foreach (int p in sets[(int)origin])
						{
							int ox = (p % SW - dx + SW) % SW, oy = (p / SW - dy + SH) % SH;
							double s = Similarity(oy * SW + ox, sample, SW, SH, i, result, OW, OH, N, origins, indexed);

							if (!mask[ox, oy]) candidates.Add(ox + oy * SW, Math.Pow(100, s / t));
							mask[ox, oy] = true;
						}
					}
				}

			int shifted = candidates.Any() ? candidates.Random(random.NextDouble()) : random.Next(SW) + random.Next(SH) * SW;
			origins[i] = shifted;
			result[i] = sample[shifted];
		}

		return result;
	}

	static int[] FullSynthesis(int[] sample, int SW, int SH, int N, int OW, int OH, double t, bool indexed)
	{
		int[] result = new int[OW * OH];
		int?[] origins = new int?[OW * OH];
		Random random = new Random();

		if (!indexed) for (int y = 0; y < OH; y++) for (int x = 0; x < OW; x++)	if (y + N >= OH)
		{
			result[x + y * OW] = sample[random.Next(SW * SH)];
			origins[x + y * OW] = -1;
		}

		for (int i = 0; i < OW * OH; i++)
		{
			double[] candidates = new double[SW * SH];
			double max = -10000;
			int argmax = -1;

			for (int j = 0; j < SW * SH; j++)
			{
				double s = Similarity(j, sample, SW, SH, i, result, OW, OH, N, origins, indexed);
				if (s > max)
				{
					max = s;
					argmax = j;
				}

				if (indexed) candidates[j] = Math.Pow(100.0, s / t);
			}

			if (indexed) argmax = candidates.Random(random.NextDouble());
			result[i] = sample[argmax];
			origins[i] = -1;
		}

		return result;
	}

	static double Similarity(int i1, int[] b1, int w1, int h1, int i2, int[] b2, int w2, int h2, int N, int?[] origins, bool indexed)
	{
		double sum = 0;
		int x1 = i1 % w1, y1 = i1 / w1, x2 = i2 % w2, y2 = i2 / w2;

		for (int dy = -N; dy <= 0; dy++) for (int dx = -N; (dy < 0 && dx <= N) || (dy == 0 && dx < 0); dx++)
			{
				int sx1 = (x1 + dx + w1) % w1, sy1 = (y1 + dy + h1) % h1;
				int sx2 = (x2 + dx + w2) % w2, sy2 = (y2 + dy + h2) % h2;

				int c1 = b1[sx1 + sy1 * w1];
				int c2 = b2[sx2 + sy2 * w2];

				if (origins == null || origins[sy2 * w2 + sx2] != null)
				{
					if (indexed) sum += c1 == c2 ? 1 : -1;
					else
					{
						Color C1 = Color.FromArgb(c1), C2 = Color.FromArgb(c2);
						sum -= (double)((C1.R - C2.R) * (C1.R - C2.R) + (C1.G - C2.G) * (C1.G - C2.G) + (C1.B - C2.B) * (C1.B - C2.B)) / 65536.0;
					}
				}
			}

		return sum;
	}
}

static class Stuff
{
	public static T Get<T>(this XmlNode node, string attribute, T defaultT = default(T))
	{
		string s = ((XmlElement)node).GetAttribute(attribute);
		var converter = TypeDescriptor.GetConverter(typeof(T));
		return s == "" ? defaultT : (T)converter.ConvertFromString(s);
	}

	public static int Random(this double[] array, double r)
	{
		double sum = array.Sum();

		if (sum <= 0) 
		{
			for (int j = 0; j < array.Count(); j++) array[j] = 1;
			sum = array.Sum();
		}

		for (int j = 0; j < array.Count(); j++) array[j] /= sum;

		int i = 0;
		double x = 0;

		while (i < array.Count())
		{
			x += array[i];
			if (r <= x) return i;
			i++;
		}

		return 0;
	}

	public static int Random(this Dictionary<int, double> dic, double r) { return dic.Keys.ToArray()[dic.Values.ToArray().Random(r)]; }
}