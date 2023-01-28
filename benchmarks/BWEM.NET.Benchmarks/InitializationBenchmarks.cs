using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using BenchmarkDotNet.Attributes;
using BWAPI.NET;

namespace BWEM.NET.Benchmarks
{
    [MemoryDiagnoser(false)]
    public class InitializationBenchmarks
    {
        private const string ResourcesFolder = "Resources";

        private const string TestMapName = "(2)Astral Balance.scm_frame0_buffer.bin";
        // private const string TestMapName = "(2)Breaking Point.scx_frame0_buffer.bin";
        // private const string TestMapName = "(2)Isolation.scx_frame0_buffer.bin";
        // private const string TestMapName = "(2)Crystallis.scm_frame0_buffer.bin";
        // private const string TestMapName = "(3)Stepping Stones.scm_frame0_buffer.bin";
        // private const string TestMapName = "(4)Arctic Station.scx_frame0_buffer.bin";
        // private const string TestMapName = "(4)Space Debris.scm_frame0_buffer.bin";
        // private const string TestMapName = "(5)Twilight Star.scx_frame0_buffer.bin";
        // private const string TestMapName = "(6)Sapphire Isles.scx_frame0_buffer.bin";
        // private const string TestMapName = "(7)Black Lotus.scx_frame0_buffer.bin";
        // private const string TestMapName = "(8)Frozen Sea.scx_frame0_buffer.bin";

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _gameViewAccessor;
        private Game _game;

        [GlobalSetup]
        public void Setup()
        {
            _mmf = GetMemoryMappedFileForMap(Path.Combine(ResourcesFolder, TestMapName));
            _gameViewAccessor = _mmf.CreateViewAccessor(0, ClientData.GameData_.Size, MemoryMappedFileAccess.ReadWrite);
            _game = new Game(new ClientData(_gameViewAccessor));
            _game.Init();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _gameViewAccessor.Dispose();
            _mmf.Dispose();
        }

        [Benchmark]
        public void BWEMInit()
        {
            var map = new Map(_game);
            map.Initialize();
        }

        private static MemoryMappedFile GetMemoryMappedFileForMap(string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
            using var deflateStream = new DeflateStream(fileStream, CompressionMode.Decompress);
            var mmf = MemoryMappedFile.CreateNew(null, ClientData.GameData_.Size, MemoryMappedFileAccess.ReadWrite);
            using var gameViewStream = mmf.CreateViewStream(0, ClientData.GameData_.Size);
            deflateStream.CopyTo(gameViewStream);
            return mmf;
        }
    }
}
