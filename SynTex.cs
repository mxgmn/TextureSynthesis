/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Linq;
using System.Drawing;
using System.Xml.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;

static class Program
{
	static void Main()
	{
		Stopwatch sw = Stopwatch.StartNew();
		XDocument xdoc = XDocument.Load("samples.xml");
		int pass = 1;

		foreach (XElement xelem in xdoc.Root.Elements("sample"))
		{
			string name = xelem.Get<string>("name"), method = xelem.Get<string>("method");
			int K = xelem.Get("K", 1), N = xelem.Get("N", 1), M = xelem.Get("M", 20), polish = xelem.Get("polish", 3), OW = xelem.Get("width", 32), OH = xelem.Get("height", 32);
			bool indexed = xelem.Get("indexed", true);
			double t = xelem.Get("temperature", 1.0);

			Bitmap sample = new Bitmap($"Samples/{name}.png");
			List<int>[] similaritySets = null;

			int[] sampleArray = new int[sample.Width * sample.Height];
			for (int j = 0; j < sample.Width * sample.Height; j++) sampleArray[j] = sample.GetPixel(j % sample.Width, j / sample.Width).ToArgb();

			if (method == "Coherent")
			{
				Console.WriteLine($"< {name}");
				similaritySets = Analysis(sampleArray, sample.Width, sample.Height, K, N, indexed);
			}

			for (int i = 0; i < xelem.Get("screenshots", 1); i++)
			{
				Console.WriteLine($"> {name} {i}");
				string filename = $"{pass} {method} {name} {indexed} N={N} ";
				int[] outputArray;

				if (method == "Full")
				{
					outputArray = FullSynthesis(sampleArray, sample.Width, sample.Height, N, OW, OH, t, indexed);
					filename += $"t={t}";
				}
				else if (method == "Coherent")
				{
					outputArray = CoherentSynthesis(sampleArray, sample.Width, sample.Height, similaritySets, N, OW, OH, t, indexed);
					filename += $"K={K} t={t}";
				}
				else if (method == "Harrison")
				{
					outputArray = ReSynthesis(sampleArray, sample.Width, sample.Height, N, M, polish, indexed, OW, OH);
					filename += $"M={M} polish={polish}";
				}
				else continue;

				Bitmap output = new Bitmap(OW, OH);
				for (int j = 0; j < OW * OH; j++) output.SetPixel(j % OW, j / OW, Color.FromArgb(outputArray[j]));
				output.Save($"{filename} {i}.png");
			}

			pass++;
		}

		Console.WriteLine($"time = {sw.ElapsedMilliseconds}");
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
				double max = -1E-4;
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

		for (int i = 0; i < result.Length; i++)
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

							if (!mask[ox, oy]) candidates.Add(ox + oy * SW, Math.Pow(1E+2, s / t));
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

		for (int i = 0; i < result.Length; i++)
		{
			double[] candidates = new double[SW * SH];
			double max = -1E+4;
			int argmax = -1;

			for (int j = 0; j < SW * SH; j++)
			{
				double s = Similarity(j, sample, SW, SH, i, result, OW, OH, N, origins, indexed);
				if (s > max)
				{
					max = s;
					argmax = j;
				}

				if (indexed) candidates[j] = Math.Pow(1E+2, s / t);
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

	static int[] ReSynthesis(int[] sample, int SW, int SH, int N, int M, int polish, bool indexed, int OW, int OH)
	{
		List<int> colors = new List<int>();
		int[] indexedSample = new int[sample.Length];

		for (int j = 0; j < SW * SH; j++)
		{
			int color = sample[j];

			int i = 0;
			foreach (var c in colors)
			{
				if (c == color) break;
				i++;
			}

			if (i == colors.Count) colors.Add(color);
			indexedSample[j] = i;
		}

		int colorsNumber = colors.Count;

		double metric(int c1, int c2)
		{
			Color color1 = Color.FromArgb(c1), color2 = Color.FromArgb(c2);
			const double lambda = 1.0 / (20.0 * 65536.0);
			double r = 1.0 + lambda * (double)((color1.R - color2.R) * (color1.R - color2.R));
			double g = 1.0 + lambda * (double)((color1.G - color2.G) * (color1.G - color2.G));
			double b = 1.0 + lambda * (double)((color1.B - color2.B) * (color1.B - color2.B));
			return -Math.Log(r * g * b);
		};

		double[][] colorMetric = null;
		if (!indexed && colorsNumber <= 1024)
		{
			colorMetric = new double[colorsNumber][];
			for (int x = 0; x < colorsNumber; x++)
			{
				colorMetric[x] = new double[colorsNumber];
				for (int y = 0; y < colorsNumber; y++)
				{
					int cx = colors[x], cy = colors[y];
					colorMetric[x][y] = metric(cx, cy);
				}
			}
		}

		int[] origins = new int[OW * OH];
		for (int i = 0; i < origins.Length; i++) origins[i] = -1;
		Random random = new Random();

		int[] shuffle = new int[OW * OH];
		for (int i = 0; i < shuffle.Length; i++)
		{
			int j = random.Next(i + 1);
			if (j != i) shuffle[i] = shuffle[j];
			shuffle[j] = i;
		}

		for (int round = 0; round <= polish; round++) for (int counter = 0; counter < shuffle.Length; counter++)
			{
				int f = shuffle[counter];
				int fx = f % OW, fy = f / OW;
				int neighborsNumber = round > 0 ? 8 : Math.Min(8, counter);
				int neighborsFound = 0;

				int[] candidates = new int[neighborsNumber + M];

				if (neighborsNumber > 0)
				{
					int[] neighbors = new int[neighborsNumber];
					int[] x = new int[4], y = new int[4];

					for (int radius = 1; neighborsFound < neighborsNumber; radius++)
					{
						x[0] = fx - radius;
						y[0] = fy - radius;
						x[1] = fx - radius;
						y[1] = fy + radius;
						x[2] = fx + radius;
						y[2] = fy + radius;
						x[3] = fx + radius;
						y[3] = fy - radius;

						for (int k = 0; k < 2 * radius; k++)
						{
							for (int d = 0; d < 4; d++)
							{
								x[d] = (x[d] + 10 * OW) % OW;
								y[d] = (y[d] + 10 * OH) % OH;

								if (neighborsFound >= neighborsNumber) continue;
								int point = x[d] + y[d] * OW;
								if (origins[point] != -1)
								{
									neighbors[neighborsFound] = point;
									neighborsFound++;
								}
							}

							y[0]++;
							x[1]++;
							y[2]--;
							x[3]--;
						}
					}


					for (int n = 0; n < neighborsNumber; n++)
					{
						int cx = (origins[neighbors[n]] + (f - neighbors[n]) % OW + 100 * SW) % SW;
						int cy = (origins[neighbors[n]] / SW + f / OW - neighbors[n] / OW + 100 * SH) % SH;
						candidates[n] = cx + cy * SW;
					}
				}

				for (int m = 0; m < M; m++) candidates[neighborsNumber + m] = random.Next(SW * SH);

				double max = -1E+10;
				int argmax = -1;

				for (int c = 0; c < candidates.Length; c++)
				{
					double sum = 1E-6 * random.NextDouble();
					int ix = candidates[c] % SW, iy = candidates[c] / SW, jx = f % OW, jy = f / OW;
					int SX, SY, FX, FY, S, F;
					int origin;

					for (int dy = -N; dy <= N; dy++) for (int dx = -N; dx <= N; dx++) if (dx != 0 || dy != 0)
							{
								SX = ix + dx;
								if (SX < 0) SX += SW;
								else if (SX >= SW) SX -= SW;

								SY = iy + dy;
								if (SY < 0) SY += SH;
								else if (SY >= SH) SY -= SH;

								FX = jx + dx;
								if (FX < 0) FX += OW;
								else if (FX >= OW) FX -= OW;

								FY = jy + dy;
								if (FY < 0) FY += OH;
								else if (FY >= OH) FY -= OH;

								S = SX + SY * SW;
								F = FX + FY * OW;

								origin = origins[F];
								if (origin != -1)
								{
									if (indexed) sum += sample[origin] == sample[S] ? 1 : -1;
									else if (colorMetric != null) sum += colorMetric[indexedSample[origin]][indexedSample[S]];
									else sum += metric(sample[origin], sample[S]);
								}
							}

					if (sum >= max)
					{
						max = sum;
						argmax = candidates[c];
					}
				}

				origins[f] = argmax;
			}

		int[] result = new int[OW * OH];
		for (int i = 0; i < result.Length; i++) result[i] = sample[origins[i]];
		return result;
	}
}

static class Stuff
{
	public static T Get<T>(this XElement xelem, string attribute, T defaultT = default(T))
	{
		XAttribute a = xelem.Attribute(attribute);
		return a == null ? defaultT : (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(a.Value);
	}

	public static int Random(this double[] array, double r)
	{
		double sum = array.Sum();

		if (sum <= 0) 
		{
			for (int j = 0; j < array.Length; j++) array[j] = 1;
			sum = array.Sum();
		}

		for (int j = 0; j < array.Length; j++) array[j] /= sum;

		int i = 0;
		double x = 0;

		while (i < array.Length)
		{
			x += array[i];
			if (r <= x) return i;
			i++;
		}

		return 0;
	}

	public static int Random(this Dictionary<int, double> dic, double r) => dic.Keys.ToArray()[dic.Values.ToArray().Random(r)];
}
