﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MiNET.Worlds;
using SharpMCRewrite.Blocks;
using System.IO;
using SharpMCRewrite.Worlds.Experimental;

namespace SharpMCRewrite.Worlds.Nether
{
	class NetherGenerator : IWorldProvider
	{
		float stoneBaseHeight = 0;
		float stoneBaseNoise = 0.08f;
		float stoneBaseNoiseHeight = 4;

		float stoneMountainHeight = 48;
		float stoneMountainFrequency = 0.008f;
		float stoneMinHeight = 0;

		float topBaseHeight = 1;
		float topNoise = 1.23f;
		float topNoiseHeight = 16;

		private string _folder = "";
		public Dictionary<Tuple<int, int>, ChunkColumn> ChunkCache = new Dictionary<Tuple<int, int>, ChunkColumn>();
		public override bool IsCaching { get; set; }
		private static int _seedoffset = new Random(Globals.Seed.GetHashCode()).Next(1, Int16.MaxValue);

		public NetherGenerator(string folder)
        {
            _folder = folder;
            IsCaching = true;
        }

		public override ChunkColumn GetChunk(int x, int z)
		{
			foreach (var ch in ChunkCache)
			{
				if (ch.Key.Item1 == x && ch.Key.Item2 == z)
				{
					return ch.Value;
				}
			}
			throw new Exception("We couldn't find the chunk.");
		}

		public override ChunkColumn LoadChunk(int x, int z)
		{
			byte[] u = Globals.Decompress(File.ReadAllBytes(_folder + "/" + x + "." + z + ".cfile"));
			MSGBuffer reader = new MSGBuffer(u);

			int BlockLength = reader.ReadInt();
			ushort[] Block = reader.ReadUShortLocal(BlockLength);

			int SkyLength = reader.ReadInt();
			byte[] Skylight = reader.Read(SkyLength);

			int LightLength = reader.ReadInt();
			byte[] Blocklight = reader.Read(LightLength);

			int BiomeIDLength = reader.ReadInt();
			byte[] BiomeID = reader.Read(BiomeIDLength);

			ChunkColumn CC = new ChunkColumn();
			CC.Blocks = Block;
			CC.Blocklight.Data = Blocklight;
			CC.Skylight.Data = Skylight;
			CC.BiomeId = BiomeID;
			CC.X = x;
			CC.Z = z;
			Debug.WriteLine("We should have loaded " + x + ", " + z);
			return CC;
		}

		public override void SaveChunks(string Folder)
		{
			foreach (var i in ChunkCache)
			{
				File.WriteAllBytes(_folder + "/" + i.Value.X + "." + i.Value.Z + ".cfile", Globals.Compress(i.Value.Export()));
			}
		}

		public override ChunkColumn GenerateChunkColumn(Vector2 chunkCoordinates)
		{
			if (ChunkCache.ContainsKey(new Tuple<int, int>(chunkCoordinates.X, chunkCoordinates.Z)))
			{
				ChunkColumn c;
				if (ChunkCache.TryGetValue(new Tuple<int, int>(chunkCoordinates.X, chunkCoordinates.Z), out c))
				{
					Debug.WriteLine("Chunk " + chunkCoordinates.X + ":" + chunkCoordinates.Z + " was already generated!");
					return c;
				}
			}

			if (File.Exists((_folder + "/" + chunkCoordinates.X + "." + chunkCoordinates.Z + ".cfile")))
			{
				ChunkColumn cd = LoadChunk(chunkCoordinates.X, chunkCoordinates.Z);
				if (!ChunkCache.ContainsKey(new Tuple<int, int>(cd.X, cd.Z)))
					ChunkCache.Add(new Tuple<int, int>(cd.X, cd.Z), cd);
				return cd;
			}

			Debug.WriteLine("ChunkFile not found, generating...");

			var chunk = new ChunkColumn { X = chunkCoordinates.X, Z = chunkCoordinates.Z };
			PopulateChunk(chunk);

			ChunkCache.Add(new Tuple<int, int>(chunkCoordinates.X, chunkCoordinates.Z), chunk);

			return chunk;
		}

		public override IEnumerable<ChunkColumn> GenerateChunks(int _viewDistance, double playerX, double playerZ, Dictionary<Tuple<int, int>, ChunkColumn> chunksUsed, ClientWrapper wrapper)
		{
			lock (chunksUsed)
			{
				Dictionary<Tuple<int, int>, double> newOrders = new Dictionary<Tuple<int, int>, double>();
				double radiusSquared = _viewDistance / Math.PI;
				double radius = Math.Ceiling(Math.Sqrt(radiusSquared));
				double centerX = Math.Floor((playerX) / 16);
				double centerZ = Math.Floor((playerZ) / 16);

				for (double x = -radius; x <= radius; ++x)
				{
					for (double z = -radius; z <= radius; ++z)
					{
						var distance = (x * x) + (z * z);
						if (distance > radiusSquared)
						{
							continue;
						}
						int chunkX = (int)Math.Floor(x + centerX);
						int chunkZ = (int)Math.Floor(z + centerZ);

						Tuple<int, int> index = new Tuple<int, int>((int)chunkX, (int)chunkZ);
						newOrders[index] = distance;
					}
				}

				if (newOrders.Count > _viewDistance)
				{
					foreach (var pair in newOrders.OrderByDescending(pair => pair.Value))
					{
						if (newOrders.Count <= _viewDistance) break;
						newOrders.Remove(pair.Key);
					}
				}


				foreach (var chunkKey in chunksUsed.Keys.ToArray())
				{
					if (!newOrders.ContainsKey(chunkKey))
					{
						chunksUsed.Remove(chunkKey);
					}
				}

				long avarageLoadTime = -1;
				foreach (var pair in newOrders.OrderBy(pair => pair.Value))
				{
					if (chunksUsed.ContainsKey(pair.Key)) continue;

					int x = pair.Key.Item1;
					int z = pair.Key.Item2;

					ChunkColumn chunk = GenerateChunkColumn(new Vector2(x, z));
					chunksUsed.Add(pair.Key, chunk);

					yield return chunk;
				}
			}
		}

		private static readonly Random getrandom = new Random();
		private static readonly object syncLock = new object();
		public static int GetRandomNumber(int min, int max)
		{
			lock (syncLock)
			{ // synchronize
				return getrandom.Next(min, max);
			}
		}

		private int waterLevel = 35;

		private void PopulateChunk(ChunkColumn chunk)
		{
			int trees = new Random().Next(0, 10);
			int[,] treeBasePositions = new int[trees, 2];

			for (int t = 0; t < trees; t++)
			{
				int x = new Random().Next(1, 16);
				int z = new Random().Next(1, 16);
				treeBasePositions[t, 0] = x;
				treeBasePositions[t, 1] = z;
			}

			for (int x = 0; x < 16; x++)
			{
				for (int z = 0; z < 16; z++)
				{
					int stoneHeight = (int) Math.Floor(stoneBaseHeight);
					stoneHeight += GetNoise(chunk.X * 16 + x, chunk.Z * 16 + z, stoneMountainFrequency, (int)Math.Floor(stoneMountainHeight));

					if (stoneHeight < stoneMinHeight)
						stoneHeight = (int) Math.Floor(stoneMinHeight);

					stoneHeight += GetNoise(chunk.X * 16 + x, chunk.Z * 16 + z, stoneBaseNoise, (int)Math.Floor(stoneBaseNoiseHeight));


					int topHeight = (int)Math.Floor(topBaseHeight);
					topHeight += GetNoise(chunk.X * 16 + x, chunk.Z * 16 + z, topNoise, (int)Math.Floor(topNoiseHeight));

					if (topHeight < topBaseHeight)
						topHeight = (int)Math.Floor(topBaseHeight);

					topHeight += GetNoise(chunk.X * 16 + x, chunk.Z * 16 + z, topNoise, (int)Math.Floor(topBaseHeight));

					for (int y = 0; y < 256; y++)
					{
							if (y == 0 || y == 80)
							{
								chunk.SetBlock(x,y,z, new BlockBedrock());
							}
							else if (y <= topHeight)
							{
								chunk.SetBlock(x, 80 - y, z, BlockFactory.GetBlockById(87));
								//Glowstone
								if (GetRandomNumber(0, 1500) < 50)
								{
									chunk.SetBlock(x, 80 -y, z, BlockFactory.GetBlockById(89));
								}
							}
							else if (y <= stoneHeight)
							{
								chunk.SetBlock(x , y, z, BlockFactory.GetBlockById(87));
								//Quartz Ore
								if (GetRandomNumber(0, 1500) < 50)
								{
									chunk.SetBlock(x, y, z, BlockFactory.GetBlockById(153));
								}

								if (GetRandomNumber(0, 1200) < 50)
								{
									chunk.SetBlock(x, y + 1, z, BlockFactory.GetBlockById(51));
								}
							}
							else if (y < waterLevel)
							{
								if (chunk.GetBlock(x,y,z) == 0 || chunk.GetBlock(x,y,z) == 51)
									chunk.SetBlock(x, y, z, BlockFactory.GetBlockById(10));
							}
					}
				}
			}
		}

		public override void SetBlock(Block block, Level level, bool broadcast)
		{
			ChunkColumn c;
			if (!ChunkCache.TryGetValue(new Tuple<int, int>(block.Coordinates.X / 16, block.Coordinates.Z / 16), out c)) throw new Exception("No chunk found!");

			c.SetBlock((block.Coordinates.X & 0x0f), (block.Coordinates.Y & 0x7f), (block.Coordinates.Z & 0x0f), block);
			if (!broadcast) return;

			foreach (var player in level.OnlinePlayers)
			{
				new Networking.Packages.BlockChange(player.Wrapper, new MSGBuffer(player.Wrapper))
				{
					Block = block,
					Location = block.Coordinates
				}.Write();
			}
		}

		public override Vector3 GetSpawnPoint()
		{
			return new Vector3(1,1,1);
		}

		private static readonly OpenSimplexNoise OpenNoise = new OpenSimplexNoise(Globals.Seed.GetHashCode());
		private static readonly PerlinNoise PerlinNoise = new PerlinNoise(Globals.Seed.GetHashCode());

		public static int GetNoise(int x, int z, float scale, int max)
		{

			switch (Globals.NoiseGenerator)
			{
				case NoiseGenerator.Perlin:
					return (int)Math.Floor((PerlinNoise.Noise(x * scale, 0, z * scale) + 1f) * (max / 2f));
				case NoiseGenerator.Simplex:
					return (int)Math.Floor((SimplexNoise.Noise.Generate(_seedoffset + x * scale, 0, _seedoffset + z * scale) + 1f) * (max / 2f));
				case NoiseGenerator.OpenSimplex:
					return (int)Math.Floor((OpenNoise.Evaluate(x * scale, z * scale) + 1f) * (max / 2f));
				default:
					return (int)Math.Floor((SimplexNoise.Noise.Generate(_seedoffset + x * scale, 0, _seedoffset + z * scale) + 1f) * (max / 2f));
			}
		}
	}
}
