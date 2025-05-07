using System.Windows.Forms;

namespace ChessOOP
{
    public partial class PromotionForm : Form
    {
        public PieceType SelectedType { get; private set; } = PieceType.Queen;
        private static string projectDir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)
           .Parent
           .Parent
           .Parent
           .FullName;
        private string chessSprites = Path.Combine(projectDir, "sprites", "chess.png");
        public PromotionForm(Player player)
        {
            InitializeComponent();
            InitializeButtons(player);
        }

        private void InitializeButtons(Player player)
        {
            // Список доступных фигур для превращения
            var promotionPieces = new[]
            {
                PieceType.Queen,
                PieceType.Rook,
                PieceType.Bishop,
                PieceType.Knight
            };

            int buttonSize = 80; // Размер кнопки
            int spacing = 10;    // Отступ между кнопками
            int startX = 10;     // Начальная координата X

            foreach (var pieceType in promotionPieces)
            {
                // Создаем кнопку
                var button = new Button
                {
                    Size = new Size(buttonSize, buttonSize),
                    Location = new Point(startX, spacing),
                    Tag = pieceType, // Сохраняем тип фигуры в Tag
                    Image = GetPieceImage(pieceType, player),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White
                };

                // Обработчик клика
                button.Click += (sender, e) =>
                {
                    SelectedType = (PieceType)((Button)sender).Tag;
                    DialogResult = DialogResult.OK;
                    Close();
                };

                // Добавляем кнопку на форму
                this.Controls.Add(button);
                startX += buttonSize + spacing; // Сдвигаем позицию для следующей кнопки
            }

            // Настраиваем размер формы
            this.ClientSize = new Size(
                startX + spacing,
                buttonSize + spacing * 2
            );
        }

        private Image GetPieceImage(PieceType type, Player player )
        {
            // Используйте вашу логику загрузки спрайтов
            var piece = CreatePiece(type, player);
            return piece.GetImage(chessSprites);
        }

        private void OnPieceSelected(object sender, EventArgs e)
        {
            SelectedType = (PieceType)((Button)sender).Tag;
            DialogResult = DialogResult.OK;
            Close();
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
    }
}