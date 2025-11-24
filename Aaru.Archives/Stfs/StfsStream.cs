using System;
using System.IO;

namespace Aaru.Archives;

public sealed partial class Stfs
{
#region Nested type: StfsStream

    private class StfsStream : Stream
    {
        readonly Stream _baseStream;
        readonly int    _blockSeparation;
        readonly int    _headerSize;
        readonly bool   _isConsole;
        readonly long   _length;
        readonly int    _startingBlock;
        long            _position;

        internal StfsStream(Stream baseStream, int length, int startingBlock, int headerSize, byte blockSeparation,
                            bool   isConsole)
        {
            _baseStream      = baseStream;
            _length          = length;
            _position        = 0;
            _startingBlock   = startingBlock;
            _headerSize      = headerSize;
            _blockSeparation = blockSeparation;
            _isConsole       = isConsole;
        }

        /// <inheritdoc />
        public override bool CanRead => true;
        /// <inheritdoc />
        public override bool CanSeek => true;
        /// <inheritdoc />
        public override bool CanWrite => false;
        /// <inheritdoc />
        public override long Length => _length;

        /// <inheritdoc />
        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        /// <inheritdoc />
        public override void Flush()
        {
            // No-op
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var totalRead = 0;

            // Validate parameters
            ArgumentNullException.ThrowIfNull(buffer);

            if(offset < 0 || count < 0) throw new ArgumentOutOfRangeException("Offset and count must be non-negative");

            if(buffer.Length - offset < count)
                throw new ArgumentException("Buffer too small for the requested offset and count");

            if(_position >= _length) return 0; // EOF

            // Calculate block for current position
            int currentBlock = ComputeBlockNumber((int)(_position / 0x1000) + _startingBlock,
                                                  _headerSize,
                                                  _blockSeparation,
                                                  _isConsole);

            // Calculate position within block
            var blockOffset = (int)(_position % 0x1000);

            // Calculate absolute position in the stream
            long absolutePosition = BlockToPosition(currentBlock, _headerSize) + blockOffset;

            // Seek to the absolute position
            _baseStream.Position = absolutePosition;

            // Calculate bytes left to read to fill a block
            int leftInBlock = 0x1000 - blockOffset;

            // Read bytes left in the block
            _baseStream.ReadExactly(buffer, offset, leftInBlock);

            // Update position and counters
            _position += leftInBlock;
            offset    += leftInBlock;
            count     -= leftInBlock;
            totalRead += leftInBlock;

            // Read full blocks
            while(count >= 0x1000 && _position < _length)
            {
                // Calculate again block number for current position
                currentBlock = ComputeBlockNumber((int)(_position / 0x1000) + _startingBlock,
                                                  _headerSize,
                                                  _blockSeparation,
                                                  _isConsole);

                // Calculate absolute position in the stream
                absolutePosition = BlockToPosition(currentBlock, _headerSize);

                // Seek to the absolute position
                _baseStream.Position = absolutePosition;

                // Read the full block
                _baseStream.ReadExactly(buffer, offset, 0x1000);

                _position += 0x1000;
                offset    += 0x1000;
                count     -= 0x1000;
                totalRead += 0x1000;

                if(_position >= _length) break; // EOF
            }

            // Read remaining bytes
            if(count <= 0 || _position >= _length) return totalRead;

            // Calculate again block number for current position
            currentBlock = ComputeBlockNumber((int)(_position / 0x1000) + _startingBlock,
                                              _headerSize,
                                              _blockSeparation,
                                              _isConsole);

            // Calculate absolute position in the stream
            absolutePosition = BlockToPosition(currentBlock, _headerSize);

            // Calculate bytes left to read to fill a block
            leftInBlock = (int)(_position % 0x1000);

            // Seek to the absolute position
            _baseStream.Position = absolutePosition + leftInBlock;

            // Read remaining bytes
            var toRead = (int)Math.Min(count, _length - _position);
            _baseStream.ReadExactly(buffer, offset, toRead);
            _position += toRead;
            totalRead += toRead;

            return totalRead;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPos = origin switch
                          {
                              SeekOrigin.Begin   => offset,
                              SeekOrigin.Current => _position + offset,
                              SeekOrigin.End     => _length   + offset,
                              _                  => throw new ArgumentException("Invalid SeekOrigin", nameof(origin))
                          };

            if(newPos < 0 || newPos > _length) throw new IOException("Attempt to seek outside the strean");

            _position = newPos;

            return _position;
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException("Stream is read-only");
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Stream is read-only");
        }
    }

#endregion
}