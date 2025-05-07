using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOOP
{
    public class PieceMovedEventArgs : EventArgs
    {
        public ChessPiece MovedPiece { get; }
        public ChessPiece EatenPiece { get; }
        public Position From { get; }
        public Position To { get; }
        public Dictionary<PieceType, int> CountOfEachType = new Dictionary<PieceType, int>
        {
            {PieceType.Pawn, 0 },
            {PieceType.Bishop, 0 },
            {PieceType.Knight, 0 },
            {PieceType.Rook, 0 },
            {PieceType.Queen, 0 },
            {PieceType.King, 0 }
        };

        public PieceMovedEventArgs(ChessPiece piece, ChessPiece eatenPiece, Position from, Position to)
        {
            MovedPiece = piece;
            EatenPiece = eatenPiece;
            From = from;
            To = to;
            if (eatenPiece is not null)
            {
                CountOfEachType[eatenPiece.Type] += 1;
            }
        }
    }
}
