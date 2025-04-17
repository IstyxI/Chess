using Microsoft.VisualBasic.Devices;
using System.Numerics;

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
                var moves = piece.GetPossibleMoves(from, _board)
                    .Concat(piece.GetAttackMoves(from, _board));

                foreach (var to in moves)
                {
                    var tempBoard = _board.Clone();
                    tempBoard.MovePiece(from, to);
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
                if (piece.GetPossibleMoves(from, _board).Any())
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
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var position = new Position(row, col);
                    ChessPiece piece = _board[position];
                    if (piece != null && piece.Color == attackerColor)
                    {
                        var attacks = piece.GetAttackMoves(new Position(row, col), _board);
                        if (attacks.Contains(targetPosition))
                        {
                            return true;
                        }
                    }
                }
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

            // 2. Специальные правила (рокировка и т.д.)
            var piece = _board[from];
            if (piece.Type == PieceType.King && IsCastlingMove(from, to))
            {
                var tempBoard = _board.Clone();
                return ValidateCastling(from, to);
            }

            // 3. Проверка правил для конкретной фигуры
            if (!piece.GetPossibleMoves(from, _board)
                .Concat(_board[from]
                .GetAttackMoves(from, _board))
                .Contains(to))
                return false;

            // 4. Проверка шаха после хода
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

        private bool WouldCauseSelfCheck(Position from, Position to)
        {
            // Симуляция хода и проверка, не остался ли король под шахом
            var tempBoard = _board.Clone();
            tempBoard.MovePiece(from, to);
            return new CheckChecker(tempBoard).IsInCheck(_game.CurrentPlayer);
        }

        private bool ValidateCastling(Position kingFrom, Position kingTo)
        {
            // Создаем временную копию доски
            var tempBoard = _board.Clone();
            var tempGame = new ChessGame { _board = tempBoard };

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

        protected ChessPiece(PlayerColor color)
        {
            Color = color;
        }

        public abstract IEnumerable<Position> GetPossibleMoves(Position current, Board board);
        public abstract IEnumerable<Position> GetAttackMoves(Position current, Board board);
        public abstract ChessPiece Clone();
        public virtual Image GetImage(string spritesPath)
        {
            var y = Color == PlayerColor.White ? 0 : 150;
            var x = 150 * ((int)Type - 1);
            return LoadImagePart(spritesPath, x, y);
        }

        private Image LoadImagePart(string path, int x, int y)
        {
            var image = new Bitmap(100, 100);
            using var g = Graphics.FromImage(image);
            g.DrawImage(Image.FromFile(path),
                new Rectangle(0, 0, 100, 100),
                new Rectangle(x, y, 150, 150),
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

        public Pawn(PlayerColor color) : base(color) { }

        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board)
        {
            var moves = new List<Position>();
            int direction = Color == PlayerColor.White ? -1 : 1;

            // Логика ходов пешки
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
            return new Pawn(this.Color) { HasMoved = this.HasMoved };
        }
    }
    public class Bishop : ChessPiece
    {
        public override PieceType Type => PieceType.Bishop;

        public Bishop(PlayerColor color) : base(color) { }

        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board)
        {
            return GetLinearMoves(current, board, new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) });
        }

        public override IEnumerable<Position> GetAttackMoves(Position current, Board board)
            => GetPossibleMoves(current, board);
        public override ChessPiece Clone()
        {
            return new Bishop(this.Color) { HasMoved = this.HasMoved };
        }
    }
    public class Rook : ChessPiece
    {
        public override PieceType Type => PieceType.Rook;

        public Rook(PlayerColor color) : base(color) { }

        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board)
        {
            return GetLinearMoves(current, board, new[] { (1, 0), (-1, 0), (0, 1), (0, -1) });
        }

        public override IEnumerable<Position> GetAttackMoves(Position current, Board board)
            => GetPossibleMoves(current, board);
        public override ChessPiece Clone()
        {
            return new Rook(this.Color) { HasMoved = this.HasMoved };
        }
    }
    public class Knight : ChessPiece
    {
        public override PieceType Type => PieceType.Knight;

        public Knight(PlayerColor color) : base(color) { }

        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board)
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
            => GetPossibleMoves(current, board);
        public override ChessPiece Clone()
        {
            return new Knight(this.Color) { HasMoved = this.HasMoved };
        }
    }
    public class Queen : ChessPiece
    {
        public override PieceType Type => PieceType.Queen;

        public Queen(PlayerColor color) : base(color) { }

        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board)
        {
            var directions = new[]
            {
                (1, 0), (-1, 0), (0, 1), (0, -1),
                (1, 1), (1, -1), (-1, 1), (-1, -1)
            };

            return GetLinearMoves(current, board, directions);
        }

        public override IEnumerable<Position> GetAttackMoves(Position current, Board board)
            => GetPossibleMoves(current, board);
        public override ChessPiece Clone()
        {
            return new Queen(this.Color) { HasMoved = this.HasMoved };
        }
    }
    public class King : ChessPiece
    {
        public override PieceType Type => PieceType.King;

        public King(PlayerColor color) : base(color) { }

        public override IEnumerable<Position> GetPossibleMoves(Position current, Board board)
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
            // Добавляем возможные рокировки
            if (!HasMoved)
            {
                // Короткая рокировка (kingside)
                if (CanCastleKingside(current, board))
                    yield return new Position(current.Row, current.Col + 2);

                // Длинная рокировка (queenside)
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
            => GetPossibleMoves(current, board);
        public override ChessPiece Clone()
        {
            return new King(this.Color) { HasMoved = this.HasMoved };
        }
    }

    // Класс доски
    public class Board
    {
        private ChessPiece[,] _grid = new ChessPiece[8, 8];
        public Move PreviousMove { get; set; } = null;
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
            // Инициализация начальной позиции

            // Чёрные фигуры (верхняя часть доски, ряд 0)
            _grid[0, 0] = new Rook(PlayerColor.Black);    // a8
            _grid[0, 1] = new Knight(PlayerColor.Black);  // b8
            _grid[0, 2] = new Bishop(PlayerColor.Black);  // c8
            _grid[0, 3] = new Queen(PlayerColor.Black);   // d8
            _grid[0, 4] = new King(PlayerColor.Black);    // e8
            _grid[0, 5] = new Bishop(PlayerColor.Black);  // f8
            _grid[0, 6] = new Knight(PlayerColor.Black);  // g8
            _grid[0, 7] = new Rook(PlayerColor.Black);    // h8

            // Чёрные пешки (ряд 1)
            for (int col = 0; col < 8; col++)
            {
                _grid[1, col] = new Pawn(PlayerColor.Black); // a7, b7, ..., h7
            }

            // Белые фигуры (нижняя часть доски, ряд 7)
            _grid[7, 0] = new Rook(PlayerColor.White);    // a1
            _grid[7, 1] = new Knight(PlayerColor.White);  // b1
            _grid[7, 2] = new Bishop(PlayerColor.White);  // c1
            _grid[7, 3] = new Queen(PlayerColor.White);   // d1
            _grid[7, 4] = new King(PlayerColor.White);    // e1
            _grid[7, 5] = new Bishop(PlayerColor.White);  // f1
            _grid[7, 6] = new Knight(PlayerColor.White);  // g1
            _grid[7, 7] = new Rook(PlayerColor.White);    // h1

            // Белые пешки (ряд 6)
            for (int col = 0; col < 8; col++)
            {
                _grid[6, col] = new Pawn(PlayerColor.White); // a2, b2, ..., h2
            }
        }
        public void MovePiece(Position from, Position to)
        {
            ChessPiece piece = _grid[from.Row, from.Col];
            _grid[from.Row, from.Col] = null; // Удаляем фигуру с исходной позиции
            _grid[to.Row, to.Col] = piece;    // Размещаем на новой позиции
        }

        public void ApplyCastling(Position kingFrom, Position kingTo)
        {
            bool isKingside = kingTo.Col > kingFrom.Col;
            int rookFromCol = isKingside ? 7 : 0;
            int rookToCol = isKingside ? 5 : 3;

            // Перемещение короля
            var king = (King)this[kingFrom];
            this[kingFrom] = null;
            this[kingTo] = king;
            king.HasMoved = true;

            // Перемещение ладьи
            var rookPositionFrom = new Position(kingFrom.Row, rookFromCol);
            var rook = this[rookPositionFrom] as Rook;

            if (rook == null)
                throw new InvalidOperationException("Ладья не найдена для рокировки");

            this[rookPositionFrom] = null;
            this[new Position(kingFrom.Row, rookToCol)] = rook;
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
        private List<Move> history { get; set; } = [];
        public ChessGame()
        {
            _moveValidator = new MoveValidator(this);
            _board.Initialize();
        }

        public bool TryMakeMove(Position from, Position to)
        {
            if (!_moveValidator.IsMoveValid(from, to))
                return false;

            var piece = _board[from];

            // Специальная обработка рокировки
            if (piece.Type == PieceType.King && Math.Abs(from.Col - to.Col) == 2)
            {
                _board.ApplyCastling(from, to);
            }
            if (piece.Type == PieceType.Pawn
                && _board.IsEmpty(to)
                && from.Col != to.Col)
            {
                Position capturedPawnPos = new Position(from.Row, to.Col);
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
                ShowPromotionDialog(piece.Color, to);
            }
            PlayerColor opponent = CurrentPlayer.Opposite();
            UpdateGameState(opponent); // Передаем оппонента

            if (State == GameState.InProgress || State == GameState.Check)
            {
                CurrentPlayer = opponent;
            }

            _board.PreviousMove = new Move(from, to, piece.Type);

            return true;
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
        private void ShowPromotionDialog(PlayerColor color, Position pos)
        {
            var promoForm = new PromotionForm(color);
            if (promoForm.ShowDialog() == DialogResult.OK)
            {
                _board[pos] = CreatePiece(promoForm.SelectedType, color);
            }
        }
        private ChessPiece CreatePiece(PieceType type, PlayerColor color)
        {
            return type switch
            {
                PieceType.Queen => new Queen(color),
                PieceType.Rook => new Rook(color),
                PieceType.Bishop => new Bishop(color),
                PieceType.Knight => new Knight(color),
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
        private static string projectDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)
           .Parent
           .Parent
           .Parent
           .FullName;
        private string chessSprites = Path.Combine(projectDir, "sprites", "chess.png");
        public ChessForm()
        {
            InitializeComponent();
            InitializeBoard();
            _game = new ChessGame();
            InitializeStatusLabel();
            UpdateBoard();
            // Фиксированный размер окна (ширина, высота)
            this.Size = new Size(1200, 840);

            // Центрирование окна при запуске
            this.StartPosition = FormStartPosition.CenterScreen;
        }
        public Color GetTileColor(Position position)
        {
            return (position.Row + position.Col) % 2 == 0
                ? Color.White
                : Color.DarkGray;
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
                Height = 30
            };
            _statusLabel.Font = new Font("Times New Roman", 18, FontStyle.Bold);
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
            if (_game.State == GameState.Checkmate)
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
            _game = new ChessGame();
            UpdateBoard();
            ClearHighlights();
        }
        private void HighlightMoves(Position position)
        {
            var moves = _game._board[position]
                .GetPossibleMoves(position, _game._board)
                .Concat(_game._board[position]
                .GetAttackMoves(position, _game._board));

            foreach (var move in moves)
            {
                _buttons[move.Row, move.Col].BackColor = Color.LightGreen;
            }
        }
    }
}
