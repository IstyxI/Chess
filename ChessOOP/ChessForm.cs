using Microsoft.VisualBasic.Devices;
using System.Numerics;
using System.Timers;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace ChessOOP
{
    public record Position(int Row, int Col)
    {
        // Проверка на выход за пределы доски
        public bool IsValid() => Row is >= 0 and < 8 && Col is >= 0 and < 8;

        // Методы для вычисления новых позиций
        public Position AddRow(int delta) => new(Row + delta, Col);
        public Position AddCol(int delta) => new(Row, Col + delta);
        public Position Add(int rowDelta, int colDelta) => new(Row + rowDelta, Col + colDelta);

        // Преобразование в шахматную нотацию (например, "a1")
        public override string ToString()
            => $"{(char)('a' + Col)}{8 - Row}";
    }
    public record Move(Position from, Position to, PieceType piece)
    {
        public override string ToString()
            => $"{(char)('a' + from.Col)}{8 - from.Row} -> {(char)('a' + to.Col)}{8 - to.Row}";
    }
    public enum GameState
    {
        InProgress,  // Игра продолжается
        Check,       // Шах текущему игроку
        Checkmate,   // Мат (игра завершена)
        Stalemate,   // Пат (ничья)
        Draw,        // Другая ничья (например, по соглашению)
        Resigned     // Один из игроков сдался
    }
    public enum PlayerColor
    {
        White,  // Белые фигуры
        Black   // Чёрные фигуры
    }
    public static class PlayerColorExtensions
    {
        public static PlayerColor Opposite(this PlayerColor color)
        {
            return color == PlayerColor.White
                ? PlayerColor.Black
                : PlayerColor.White;
        }
    }
    public enum PieceType
    {
        King = 1,    // Король
        Queen,   // Ферзь
        Bishop,  // Слон
        Knight,  // Конь
        Rook,    // Ладья
        Pawn,    // Пешка
    }
    public class CheckChecker
    {
        private readonly Board _board;

        public CheckChecker(Board board)
        {
            _board = board;
        }

        public bool IsInCheck(PlayerColor player)
        {
            Position kingPosition = FindKingPosition(player);
            bool is_check = IsPositionUnderAttack(kingPosition, player.Opposite());
            return is_check;
        }
        public bool IsCheckmate(PlayerColor player)
        {
            if (!IsInCheck(player)) return false;

            foreach (var from in GetAllPiecesPositions(player))
            {
                var piece = _board[from];
                var moves = piece.GetPossibleMoves(from, _board, _board.gameState)
                    .Concat(piece.GetAttackMoves(from, _board));

                foreach (var to in moves)
                {
                    var tempBoard = _board.Clone();
                    tempBoard.MovePiece(from, to, true);
                    if (!new CheckChecker(tempBoard).IsInCheck(player))
                        return false;
                }
            }
            return true;
        }

        public bool IsStalemate(PlayerColor player)
        {
            if (IsInCheck(player)) return false;

            foreach (var from in GetAllPiecesPositions(player))
            {
                var piece = _board[from];
                if (piece.GetPossibleMoves(from, _board, _board.gameState).Any())
                    return false;
            }
            return true;
        }

        private IEnumerable<Position> GetAllPiecesPositions(PlayerColor color)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var pos = new Position(row, col);
                    if (_board[pos]?.Color == color)
                        yield return pos;
                }
            }
        }
        private Position FindKingPosition(PlayerColor player)
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var position = new Position(row, col);
                    ChessPiece piece = _board[position];
                    if (piece != null && piece.Type == PieceType.King && piece.Color == player)
                    {
                        return new Position(row, col);
                    }
                }
            }
            throw new InvalidOperationException("Король не найден на доске");
        }

        internal bool IsPositionUnderAttack(Position targetPosition, PlayerColor attackerColor)
        {
            // Проверяем атаки только от фигур противника
            CheckChecker checker = new CheckChecker(_board);
            foreach (var pos in checker.GetAllPiecesPositions(attackerColor))
            {
                var piece = _board[pos];
                if (piece.GetAttackMoves(pos, _board).Contains(targetPosition))
                    return true;
            }
            return false;
        }
    }
    public class MoveValidator
    {
        private readonly Board _board;
        private readonly ChessGame _game;

        public MoveValidator(ChessGame game)
        {
            _game = game;
            _board = game._board;
        }

        // Основной метод проверки хода
        public bool IsMoveValid(Position from, Position to)
        {
            // 1. Проверка базовых условий
            if (!from.IsValid() || !to.IsValid()) return false;  // Находится в границах
            if (_board[from] == null) return false;  // Не нажата ли до этого пустая клетка
            if (_board[from].Color != _game.CurrentPlayer) return false; // Игрок ходит своей фигурой

            // Рокировка
            var piece = _board[from];
            if (piece.Type == PieceType.King && IsCastlingMove(from, to))
            {
                var tempBoard = _board.Clone();
                return ValidateCastling(from, to);
            }

            // Проверка правил для конкретной фигуры
            if (!piece.GetPossibleMoves(from, _board, _board.gameState)
                .Concat(_board[from]
                .GetAttackMoves(from, _board))
                .Contains(to))
                return false;

            // Проверка шаха после хода
            if (WouldCauseSelfCheck(from, to))
                return false;

            return true;
        }
        private bool IsCastlingMove(Position from, Position to)
        {
            bool isWhiteKingside = from == new Position(7, 4) && to == new Position(7, 6);
            bool isWhiteQueenside = from == new Position(7, 4) && to == new Position(7, 2);
            bool isBlackKingside = from == new Position(0, 4) && to == new Position(0, 6);
            bool isBlackQueenside = from == new Position(0, 4) && to == new Position(0, 2);

            return isWhiteKingside || isWhiteQueenside || isBlackKingside || isBlackQueenside;
        }

        public bool WouldCauseSelfCheck(Position from, Position to)
        {
            // Симуляция хода и проверка, не остался ли король под шахом
            var tempBoard = _board.Clone();
            tempBoard.MovePiece(from, to, true);
            return new CheckChecker(tempBoard).IsInCheck(_game.CurrentPlayer);
        }

        private bool ValidateCastling(Position kingFrom, Position kingTo)
        {
            // Создаем временную копию доски
            var tempBoard = _board.Clone();
            Timers timers = new Timers();
            var tempGame = new ChessGame(timers) { _board = tempBoard};

            // Получаем короля и ладью на временной доске
            var king = tempBoard[kingFrom] as King;
            if (king == null || king.HasMoved) return false;

            bool isKingside = kingTo.Col > kingFrom.Col;
            int rookCol = isKingside ? 7 : 0;
            var rookPosition = new Position(kingFrom.Row, rookCol);
            var rook = tempBoard[rookPosition] as Rook;

            if (rook == null || rook.HasMoved) return false;

            // Проверка пустых клеток между королем и ладьей
            int startCol = Math.Min(kingFrom.Col, rookCol) + 1;
            int endCol = Math.Max(kingFrom.Col, rookCol);
            for (int col = startCol; col < endCol; col++)
            {
                if (!tempBoard.IsEmpty(new Position(kingFrom.Row, col))) return false;
            }

            // Выполняем "виртуальную" рокировку на временной доске
            tempBoard.ApplyCastling(kingFrom, kingTo);

            // Проверяем безопасность всех задействованных клеток
            var checker = new CheckChecker(tempBoard);
            for (int col = Math.Min(kingFrom.Col, kingTo.Col);
                 col <= Math.Max(kingFrom.Col, kingTo.Col);
                 col++)
            {
                var pos = new Position(kingFrom.Row, col);
                if (checker.IsPositionUnderAttack(pos, _game.CurrentPlayer.Opposite()))
                    return false;
            }

            return true;
        }
    }

    // Базовый класс для всех шахматных фигур
    public abstract class ChessPiece
    {
        public PlayerColor Color { get; }
        public abstract PieceType Type { get; }
        public bool HasMoved { get; set; }
        public Player player { get; set; }
        protected ChessPiece(Player player)
        {
            Color = player.color;
            this.player = player;
        }
        public int ValueOfPiece { get; protected set; }

        public abstract IEnumerable<Position> GetPossibleMoves(Position current, Board board, GameState gameState);
        public abstract IEnumerable<Position> GetAttackMoves(Position current, Board board);
        public abstract ChessPiece Clone();
        public virtual Image GetImage(string spritesPath, int picHeight = 100, int picWidth = 100)
        {
            var y = Color == PlayerColor.White ? 0 : 150;
            var x = 150 * ((int)Type - 1);
            return LoadImagePart(spritesPath, x, y, picHeight, picWidth);
        }

        private Image LoadImagePart(string path, int x, int y, int picHeight=100, int picWidth=100)
        {
            var image = new Bitmap(picHeight, picWidth);
            using var g = Graphics.FromImage(image);
            g.DrawImage(Image.FromFile(path),
                new Rectangle(0, 0, picWidth, picHeight), // куда рисуем (форма)
                new Rectangle(x, y, 150, 150),  // откуда рисуем  (png)
                GraphicsUnit.Pixel);
            return image;
        }
        protected IEnumerable<Position> GetLinearMoves(
            Position current,
            Board board,
            IEnumerable<(int dr, int dc)> directions)
        {
            foreach (var (dr, dc) in directions)
            {
                for (int step = 1; ; step++)
                {
                    Position pos = current.Add(dr * step, dc * step);
                    if (!pos.IsValid()) break;

                    if (board.IsEmpty(pos))
                    {
                        yield return pos;
                    }
                    else
                    {
                        if (board.IsEnemy(pos, Color))
                            yield return pos;
                        break;
                    }
                }
            }
        }
    }

    // Конкретные классы фигур
    public class Pawn : ChessPiece
    {
        public override PieceType Type => PieceType.Pawn;
        public Pawn(Player player) : base(player) { }
        public int ValueOfPiece { get; protected set; } = 1;
        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board, GameState gameState)
        {
            var moves = new List<Position>();
            int direction = Color == PlayerColor.White ? -1 : 1;

            var next = current.AddRow(direction);
            if (board.IsEmpty(next))
            {
                moves.Add(next);

                if (!HasMoved && board.IsEmpty(next.AddRow(direction)))
                    moves.Add(next.AddRow(direction));
            }

            return moves;
        }


        public override IEnumerable<Position> GetAttackMoves(Position current, Board board)
        {
            var attacks = new List<Position>();
            int direction = Color == PlayerColor.White ? -1 : 1;

            var leftDiag = current.Add(direction, -1);
            var rightDiag = current.Add(direction, 1);

            if (board.IsEnemy(leftDiag, Color)) attacks.Add(leftDiag);
            if (board.IsEnemy(rightDiag, Color)) attacks.Add(rightDiag);

            var left = current.Add(0, -1);
            var leftStart = current.Add(direction * 2, -1);
            var right = current.Add(0, 1);
            var rightStart = current.Add(direction * 2, 1);

            Move enemyPawn1 = new Move(leftStart, left, PieceType.Pawn);
            Move enemyPawn2 = new Move(rightStart, right, PieceType.Pawn);

            if (board.PreviousMove == enemyPawn1) attacks.Add(leftDiag);
            if (board.PreviousMove == enemyPawn2) attacks.Add(rightDiag);

            return attacks;
        }
        public override ChessPiece Clone()
        {
            return new Pawn(this.player) { HasMoved = this.HasMoved };
        }
    }
    public class Bishop : ChessPiece
    {
        public override PieceType Type => PieceType.Bishop;
        public int ValueOfPiece { get; protected set; } = 3;
        public Bishop(Player player) : base(player) { }

        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board, GameState gameState)
        {
            return GetLinearMoves(current, board, new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) });
        }

        public override IEnumerable<Position> GetAttackMoves(Position current, Board board)
            => GetPossibleMoves(current, board, board.gameState);
        public override ChessPiece Clone()
        {
            return new Bishop(this.player) { HasMoved = this.HasMoved };
        }
    }
    public class Rook : ChessPiece
    {
        public override PieceType Type => PieceType.Rook;
        public int ValueOfPiece { get; protected set; } = 5;
        public Rook(Player player) : base(player) { }

        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board, GameState gameState)
        {
            return GetLinearMoves(current, board, new[] { (1, 0), (-1, 0), (0, 1), (0, -1) });
        }

        public override IEnumerable<Position> GetAttackMoves(Position current, Board board)
            => GetPossibleMoves(current, board, board.gameState);
        public override ChessPiece Clone()
        {
            return new Rook(this.player) { HasMoved = this.HasMoved };
        }
    }
    public class Knight : ChessPiece
    {
        public override PieceType Type => PieceType.Knight;
        public int ValueOfPiece { get; protected set; } = 3;
        public Knight(Player player) : base(player) { }

        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board, GameState gameState)
        {
            int[] dx = { 2, 2, -2, -2, 1, 1, -1, -1 };
            int[] dy = { 1, -1, 1, -1, 2, -2, 2, -2 };

            for (int i = 0; i < 8; i++)
            {
                Position pos = current.Add(dx[i], dy[i]);
                if (pos.IsValid() && (board.IsEmpty(pos) || board.IsEnemy(pos, Color)))
                    yield return pos;
            }
        }

        public override IEnumerable<Position> GetAttackMoves(Position current, Board board)
            => GetPossibleMoves(current, board, board.gameState);
        public override ChessPiece Clone()
        {
            return new Knight(this.player) { HasMoved = this.HasMoved };
        }
    }
    public class Queen : ChessPiece
    {
        public override PieceType Type => PieceType.Queen;
        public int ValueOfPiece { get; protected set; } = 10;
        public Queen(Player player) : base(player) { }

        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board, GameState gameState)
        {
            var directions = new[]
            {
                (1, 0), (-1, 0), (0, 1), (0, -1),
                (1, 1), (1, -1), (-1, 1), (-1, -1)
            };

            return GetLinearMoves(current, board, directions);
        }

        public override IEnumerable<Position> GetAttackMoves(Position current, Board board)
            => GetPossibleMoves(current, board, board.gameState);
        public override ChessPiece Clone()
        {
            return new Queen(this.player) { HasMoved = this.HasMoved };
        }
    }
    public class King : ChessPiece
    {
        public override PieceType Type => PieceType.King;

        public King(Player player) : base(player) { }
        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board, GameState gameState)
        {
            var moves = new List<Position>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Position pos = current.Add(dx, dy);
                    if (pos.IsValid() && (board.IsEmpty(pos) || board.IsEnemy(pos, Color)))
                        yield return pos;
                }
            }

            if (!HasMoved && gameState != GameState.Check)
            {
                Console.WriteLine(new System.Diagnostics.StackTrace().ToString());
                // Короткая рокировка
                if (CanCastleKingside(current, board))
                    yield return new Position(current.Row, current.Col + 2);

                // Длинная рокировка
                if (CanCastleQueenside(current, board))
                    yield return new Position(current.Row, current.Col - 2);
            }
        }

        private bool CanCastleKingside(Position kingPos, Board board)
        {
            Position rookPos = new Position(kingPos.Row, 7);
            return CheckCastlingConditions(kingPos, rookPos, 1, board);
        }

        private bool CanCastleQueenside(Position kingPos, Board board)
        {
            Position rookPos = new Position(kingPos.Row, 0);
            return CheckCastlingConditions(kingPos, rookPos, -1, board);
        }

        private bool CheckCastlingConditions(Position kingPos, Position rookPos, int direction, Board board)
        {
            // Проверяем наличие ладьи
            var rook = board[rookPos] as Rook;
            if (rook == null || rook.HasMoved) return false;

            // Проверяем пустые клетки между королем и ладьей
            for (int col = kingPos.Col + direction; col != rookPos.Col; col += direction)
            {
                if (!board.IsEmpty(new Position(kingPos.Row, col))) return false;
            }

            // Проверяем безопасность клеток
            var checker = new CheckChecker(board);
            if (checker.IsInCheck(Color)) return false;

            for (int i = 1; i <= 2; i++)
            {
                Position checkPos = kingPos.AddCol(direction * i);
                if (checker.IsPositionUnderAttack(checkPos, Color.Opposite()))
                    return false;
            }

            return true;
        }
        public override IEnumerable<Position> GetAttackMoves(Position current, Board board)
        {
                        var moves = new List<Position>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Position pos = current.Add(dx, dy);
                    if (pos.IsValid() && (board.IsEmpty(pos) || board.IsEnemy(pos, Color)))
                        yield return pos;
                }
            }
        }
        public override ChessPiece Clone()
        {
            return new King(this.player) { HasMoved = this.HasMoved };
        }
    }

    // Класс доски
    public class Board
    {
        private ChessPiece[,] _grid = new ChessPiece[8, 8];
        public Move PreviousMove { get; set; } = null;
        public GameState gameState { get; }
        public event EventHandler<PieceMovedEventArgs> PieceMoved;
        protected virtual void OnPieceMoved(PieceMovedEventArgs e)
        {
            PieceMoved?.Invoke(this, e);
        }
        public ChessPiece this[Position pos]
        {
            get
            {
                if (!pos.IsValid())
                    throw new ArgumentOutOfRangeException(nameof(pos), "Invalid position");

                return _grid[pos.Row, pos.Col];
            }
            set
            {
                if (!pos.IsValid())
                    throw new ArgumentOutOfRangeException(nameof(pos), "Invalid position");

                _grid[pos.Row, pos.Col] = value;
            }
        }
        public Board Clone()
        {
            var clone = new Board();
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // Создаём копию фигуры (если она есть)
                    clone._grid[row, col] = _grid[row, col]?.Clone();
                }
            }
            return clone;
        }
        public void Initialize()
        {
            Player whitePlayer = new Player(PlayerColor.White);
            Player blackPlayer = new Player(PlayerColor.Black);
            // Инициализация начальной позиции

            _grid[0, 0] = new Rook(blackPlayer);
            _grid[0, 1] = new Knight(blackPlayer);
            _grid[0, 2] = new Bishop(blackPlayer);
            _grid[0, 3] = new Queen(blackPlayer);
            _grid[0, 4] = new King(blackPlayer);
            _grid[0, 5] = new Bishop(blackPlayer);
            _grid[0, 6] = new Knight(blackPlayer);
            _grid[0, 7] = new Rook(blackPlayer);

            // Чёрные пешки (ряд 1)
            for (int col = 0; col < 8; col++)
            {
                _grid[1, col] = new Pawn(blackPlayer);
            }

            // Белые фигуры (нижняя часть доски, ряд 7)
            _grid[7, 0] = new Rook(whitePlayer);
            _grid[7, 1] = new Knight(whitePlayer);
            _grid[7, 2] = new Bishop(whitePlayer);
            _grid[7, 3] = new Queen(whitePlayer);
            _grid[7, 4] = new King(whitePlayer);
            _grid[7, 5] = new Bishop(whitePlayer);
            _grid[7, 6] = new Knight(whitePlayer);
            _grid[7, 7] = new Rook(whitePlayer);

            // Белые пешки (ряд 6)
            for (int col = 0; col < 8; col++)
            {
                _grid[6, col] = new Pawn(whitePlayer);
            }
        }
        public void MovePiece(Position from, Position to, bool temp=false)
        {
            ChessPiece piece = _grid[from.Row, from.Col];
            if (_grid[to.Row, to.Col] != null && temp == false)
            {
                Player player = piece.player;
                player.EatenPieces.Add(_grid[to.Row, to.Col]);
                
                OnPieceMoved(new PieceMovedEventArgs(piece, _grid[to.Row, to.Col], from, to));
            }
              
            _grid[from.Row, from.Col] = null; // Удаляем фигуру с исходной позиции
            _grid[to.Row, to.Col] = piece;    // Размещаем на новой позиции
        }

        public void ApplyCastling(Position kingFrom, Position kingTo)
        {
            bool isKingside = kingTo.Col > kingFrom.Col;
            int rookFromCol = isKingside ? 7 : 0;
            int rookToCol = isKingside ? 5 : 3;

            // Перемещение короля
            var king = this[kingFrom] as King;
            this[kingTo] = king;
            king.HasMoved = true;

            // Перемещение ладьи
            var rookPositionFrom = new Position(kingFrom.Row, rookFromCol);
            var rook = this[rookPositionFrom] as Rook;

            if (rook == null)
                throw new InvalidOperationException("Ладья не найдена для рокировки");

            this[rookPositionFrom] = null;
            this[new Position(kingFrom.Row, rookToCol)] = rook;
            this[new Position(kingTo.Row, kingTo.Col)] = king;
            rook.HasMoved = true;
        }

        public bool IsInBounds(Position pos) =>
            pos.Row >= 0 && pos.Row < 8 && pos.Col >= 0 && pos.Col < 8;

        public bool IsEmpty(Position pos) =>
            IsInBounds(pos) && this[pos] == null;

        public bool IsEnemy(Position pos, PlayerColor color)
        {
            if (!IsInBounds(pos) || IsEmpty(pos))
                return false;

            return this[pos].Color != color;
        }
    }

    // Класс игры
    public class ChessGame
    {
        public Board _board { get; set; } = new Board();
        public PlayerColor CurrentPlayer { get; private set; } = PlayerColor.White;
        public GameState State { get; private set; } = GameState.InProgress;
        private readonly MoveValidator _moveValidator;
        public Timers _timers { private get; set; } = new Timers();
        private List<Move> history { get; set; } = [];
        public ChessGame(Timers timers)
        {
            _timers = timers;
            _moveValidator = new MoveValidator(this);
            _board.Initialize();
        }

        public bool TryMakeMove(Position from, Position to)
        {
            if (!_moveValidator.IsMoveValid(from, to))
                return false;

            var piece = _board[from];

            // Обработка рокировки
            if (piece.Type == PieceType.King && Math.Abs(from.Col - to.Col) == 2)
            {
                _board.ApplyCastling(from, to);
            }
            // Обработка взятия на проходе
            if (piece.Type == PieceType.Pawn
                && _board.IsEmpty(to)
                && from.Col != to.Col)
            {
                Position capturedPawnPos = new Position(from.Row, to.Col);
                piece.player.EatenPieces.Add(_board[capturedPawnPos]);
                _board[capturedPawnPos] = null;
                _board.MovePiece(from, to);
            }
            else
            {
                // Стандартный ход
                _board.MovePiece(from, to);
            }

            piece.HasMoved = true;

            if (piece.Type == PieceType.Pawn && IsPromotionRow(to))
            {
                ShowPromotionDialog(piece.player, to);
            }
            PlayerColor opponent = CurrentPlayer.Opposite();
            UpdateGameState(opponent);

            if (State == GameState.InProgress || State == GameState.Check)
            {
                CurrentPlayer = opponent;
                SwitchTimer(opponent);
            }

            _board.PreviousMove = new Move(from, to, piece.Type);

            return true;
        }
        private void SwitchTimer(PlayerColor playerColor)
        {
            if (playerColor == PlayerColor.Black)
            {
                _timers.timerBlack.Start();
                _timers.timerWhite.Stop();
            }
            else
            {
                _timers.timerBlack.Stop();
                _timers.timerWhite.Start();
            }
        }
        private bool IsPromotionRow(Position pos)
        {
            var piece = _board[pos];
            if (piece == null || piece.Type != PieceType.Pawn)
                return false;

            // Для белых пешек последняя горизонталь — 0 (верх доски)
            // Для черных пешек последняя горизонталь — 7 (низ доски)
            return (piece.Color == PlayerColor.White && pos.Row == 0)
                || (piece.Color == PlayerColor.Black && pos.Row == 7);
        }
        private void ShowPromotionDialog(Player player, Position pos)
        {
            var promoForm = new PromotionForm(player);
            if (promoForm.ShowDialog() == DialogResult.OK)
            {
                _board[pos] = CreatePiece(promoForm.SelectedType, player);
            }
        }
        private ChessPiece CreatePiece(PieceType type, Player player)
        {
            return type switch
            {
                PieceType.Queen => new Queen(player),
                PieceType.Rook => new Rook(player),
                PieceType.Bishop => new Bishop(player),
                PieceType.Knight => new Knight(player),
                _ => throw new ArgumentException("Недопустимый тип фигуры")
            };
        }
        private void UpdateGameState(PlayerColor playerToCheck)
        {
            var checker = new CheckChecker(_board);

            if (checker.IsCheckmate(playerToCheck))
            {
                State = GameState.Checkmate;
            }
            else if (checker.IsStalemate(playerToCheck))
            {
                State = GameState.Stalemate;
            }
            else if (checker.IsInCheck(playerToCheck))
            {
                State = GameState.Check;
            }
            else
            {
                State = GameState.InProgress;
            }
        }
    }

    // Форма для отображения
    public partial class ChessForm : Form
    {
        private ChessGame _game;
        private Button[,] _buttons = new Button[8, 8];
        private Position? _selectedPosition;
        private Label _statusLabel;
        private Timers _timers;
        private static string projectDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)
           .Parent
           .Parent
           .Parent
           .FullName;
        private string chessSprites = Path.Combine(projectDir, "sprites", "chess.png");
        public ChessForm()
        {
            _timers = new Timers();
            InitializeComponent();
            InitializeBoard();
            _game = new ChessGame(_timers);
            InitializeStatusLabel();
            UpdateBoard();
            AddLabels();
            _game._board.PieceMoved += ShowDiff;
            // Фиксированный размер окна (ширина, высота)
            this.Size = new Size(1200, 840);

            // Центрирование окна при запуске
            this.StartPosition = FormStartPosition.CenterScreen;
        }
        public Color GetTileColor(Position position)
        {
            return (position.Row + position.Col) % 2 == 0
                ? Color.White
                : Color.Silver;
        }
        public void ClearHighlights()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var position = new Position(row, col);
                    _buttons[row, col].BackColor = GetTileColor(position);
                }
            }
        }
        private void InitializeBoard()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var position = new Position(row, col);
                    var button = new Button
                    {
                        Size = new Size(100, 100),
                        Location = new Point(col * 100, row * 100),
                        Tag = new Position(row, col),
                        BackColor = GetTileColor(position)
                    };
                    button.Click += OnTileClick;
                    _buttons[row, col] = button;
                    Controls.Add(button);
                }
            }
        }
        private void InitializeStatusLabel()
        {
            _statusLabel = new Label
            {
                Text = "Статус: " + _game.State.ToString(),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.TopRight,
                Height = 30,
                Font = new Font("Times New Roman", 18, FontStyle.Bold),
            };
            Controls.Add(_statusLabel);
        }
        private void UpdateBoard()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Position pos = new Position(row, col);
                    var button = _buttons[row, col];
                    var piece = _game._board[pos];
                    button.BackgroundImage = piece?.GetImage(chessSprites);
                }
            }
            _statusLabel.Text = "Статус: " + _game.State.ToString();
        }

        private void OnTileClick(object sender, EventArgs e)
        {
            var position = (Position)((Button)sender).Tag;
            if (_game.State == GameState.Checkmate || _timers.GameOver == true)
            {
                MessageBox.Show($"Мат! Победил {_game.CurrentPlayer.Opposite().ToString()}");
                ResetGame();
            }
            else if (_game.State == GameState.Stalemate)
            {
                MessageBox.Show("Пат! Ничья");
                ResetGame();
            }
            else if (_selectedPosition == null)  // Если до этого была выбрана фигура
            {
                if (_game._board[position]?.Color == _game.CurrentPlayer) // Если игрок выбрал свою фигуру
                {
                    _selectedPosition = position;
                    HighlightMoves(position);
                }
            }
            else // Если игрок хочет сделать ход
            {
                if (_game.TryMakeMove(_selectedPosition, position))
                {
                    UpdateBoard();
                }
                ClearHighlights();
                _selectedPosition = null;
            }
        }
        private void ResetGame()
        {
            ClearLabels();
            Timers new_timers = new Timers();
            _game = new ChessGame(new_timers);
            _timers = new_timers;
            AddLabels();
            UpdateBoard();
            ClearHighlights();
        }
        private void HighlightMoves(Position position)
        {
            if (_buttons[position.Row, position.Col].BackColor == Color.Silver)
            {
                _buttons[position.Row, position.Col].BackColor = Color.DarkGray;
            }
            else if (_buttons[position.Row, position.Col].BackColor == Color.White)
            {
                _buttons[position.Row, position.Col].BackColor = Color.OldLace;
            }
            var moves = _game._board[position]
                .GetPossibleMoves(position, _game._board, _game.State)
                .Concat(_game._board[position]
                .GetAttackMoves(position, _game._board));

            var validator = new MoveValidator(_game);
            foreach (var move in moves)
            {
                if (validator.WouldCauseSelfCheck(position, move))
                    continue;
                _buttons[move.Row, move.Col].BackColor = Color.LightGreen;
            }
        }
        private void AddLabels()
        {
            Controls.Add(_timers.blackColon);
            Controls.Add(_timers.blackSeconds);
            Controls.Add(_timers.blackMinutes);
            Controls.Add(_timers.whiteColon);
            Controls.Add(_timers.whiteMinutes);
            Controls.Add(_timers.whiteSeconds);
        }
        private void ClearLabels()
        {
            Controls.Remove(_timers.blackSeconds);
            Controls.Remove(_timers.blackMinutes);
            Controls.Remove(_timers.blackColon);
            Controls.Remove(_timers.whiteSeconds);
            Controls.Remove(_timers.whiteMinutes);
            Controls.Remove(_timers.whiteColon);
        }
        private void AddPieceToDiff(ChessPiece piece, int x, int y, int size=50)
        {
            string chessSprites = Path.Combine(projectDir, "sprites", "chess.png");
            Image pieceImage = piece.GetImage(chessSprites, size, size);
            PictureBox piecePictureBox = new PictureBox()
            {
                Name = $"{piece.Color}{piece.Type}",
                Image  = pieceImage,
                Location = new Point(x, y),
                Size = new Size(size, size),
                BackColor = Color.Transparent,
            };
            Controls.Add(piecePictureBox);
            piecePictureBox.BringToFront();
        }
        public void ShowDiff(object sender, PieceMovedEventArgs e)
        {
            const int size = 60;
            var pieceSettings = new Dictionary<PieceType, (int startX, int whiteY, int blackY, int xStep)>
            {
                { PieceType.Pawn,    (810, 660, 100, size - size/4) },
                { PieceType.Bishop,  (810, 600, 160, size - size/4) },
                { PieceType.Knight,  (940, 600, 160, size) },
                { PieceType.Rook,    (810, 540, 220, size) },
                { PieceType.Queen,   (810, 480, 280, size) }
            };

            var currentX = new Dictionary<PieceType, int>
            {
                { PieceType.Pawn,    pieceSettings[PieceType.Pawn].startX },
                { PieceType.Bishop,  pieceSettings[PieceType.Bishop].startX },
                { PieceType.Knight,  pieceSettings[PieceType.Knight].startX },
                { PieceType.Rook,    pieceSettings[PieceType.Rook].startX },
                { PieceType.Queen,   pieceSettings[PieceType.Queen].startX }
            };

            foreach (var piece in e.MovedPiece.player.EatenPieces)
            {
                if (piece.Type == PieceType.King) continue;
                var settings = pieceSettings[piece.Type];
                var y = piece.Color == PlayerColor.Black ? settings.blackY : settings.whiteY;
        
                AddPieceToDiff(piece, currentX[piece.Type], y, size);
                currentX[piece.Type] += settings.xStep;
            }
        }
    }
}
