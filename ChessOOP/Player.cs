using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOOP
{
    public class Player
    {
        public PlayerColor color { get; set; }
        public List<ChessPiece> EatenPieces = new List<ChessPiece>();
        public Player(PlayerColor color)
        {
            this.color = color;
        }
    }
}