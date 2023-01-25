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
        private const string TestFileName = "(2)Breaking Point.scx_frame0_buffer.bin";

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _gameViewAccessor;
        private Game _game;

        [GlobalSetup]
        public void Setup()
        {
            _mmf = GetMemoryMappedFileForMap(Path.Combine(ResourcesFolder, TestFileName));
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
            Map.Instance.Initialize(_game);
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
