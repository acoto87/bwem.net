using System;
using System.Buffers;

namespace BWEM.NET
{
    public struct Markable2D : IDisposable
    {
        private readonly int _rows;
        private readonly int _cols;
        private readonly bool[] _marks;
        private int _count;

        public int Rows
        {
            get => _rows;
        }

        public int Cols
        {
            get => _cols;
        }

        public int Count
        {
            get => _count;
        }

        public int MarksCapacity
        {
            get => _marks.Length;
        }

        public Markable2D(int rows, int cols)
        {
            _rows = rows;
            _cols = cols;
            _marks = ArrayPool<bool>.Shared.Rent(_rows * _cols);
            _count = 0;
        }

        public bool IsMarked(int x, int y)
        {
            return _marks[y * _cols + x];
        }

        public void Mark(int x, int y)
        {
            _marks[y * _cols + x] = true;
            _count++;
        }

        public void UnMark(int x, int y)
        {
            _marks[y * _cols + x] = false;
            _count--;
        }

        public void Clear()
        {
            Array.Clear(_marks, 0, _marks.Length);
            _count = 0;
        }

        public void Dispose()
        {
            ArrayPool<bool>.Shared.Return(_marks, true);
        }
    }
}