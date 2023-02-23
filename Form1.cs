using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace TetrisMVP
{
    public enum Shape
    {
        T,
        L,
        O,
        I,
        Z
    }

    public enum Shift
    {
        Left,
        Right
    }

    public class Figure
    {
        public int[,] Position { get; }
        public static int Number { get; private set; }

        public Figure()
        {
            var rand = new Random();
            switch ((Shape)rand.Next(5))
            {
                case Shape.T:
                {
                    Position = new int[,]
                    {
                        { 2, 1, 3, 2 },
                        { 1, 1, 1, 2 }
                    };
                    break;
                }
                case Shape.L:
                    Position = new int[,]
                    {
                        { 1, 1, 1, 2 },
                        { 3, 1, 2, 3 }
                    };
                    break;
                case Shape.O:
                    Position = new int[,]
                    {
                        { 1, 2, 1, 2 },
                        { 1, 1, 2, 2 }
                    };
                    break;
                case Shape.I:
                    Position = new int[,]
                    {
                        { 1, 1, 1, 1 },
                        { 1, 2, 3, 4 }
                    };
                    break;
                case Shape.Z:
                    Position = new int[,]
                    {
                        { 1, 1, 2, 2 },
                        { 1, 2, 2, 3 }
                    };
                    break;
            }
            Number++;
        }

    }

    public class Field
    {
        public int[,] Cells { get; private set; }
        public Figure F { get; private set; }
        private Timer T;
        public int Decrease;

        public Field(int width, int height)
        {
            Cells = new int[width, height];
        }

        public void Start()
        {
            F = new Figure();
            T = new Timer();
            T.Interval = 500;
            T.Tick += MoveDown;
            T.Start();

            if (StateChanged != null) StateChanged();
        }

        public void Reload()
        {
            Cells = new int[Cells.GetLength(0), Cells.GetLength(1)];
            Start();
        }

        private bool IsWrongPlace()
        {
            for (var i = 0; i < 4; i++)
            {
                if (F.Position[0, i] < 0 || F.Position[0, i] >= Cells.GetLength(0) || 
                    F.Position[1, i] < 0 || F.Position[1, i] >= Cells.GetLength(1) || 
                    Cells[F.Position[0, i], F.Position[1, i]] != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearFilledLines()
        {
            var j = Cells.GetLength(1) - 1;
            while (j > 0)
            {
                var isFilledLine = true;
                for (var i = 0; i < Cells.GetLength(0); i++)
                {
                    if (Cells[i, j] == 0)
                    {
                        isFilledLine = false;
                        break;
                    }
                }

                if (isFilledLine)
                {
                    for (var i = 0; i < Cells.GetLength(0); i++)
                    {
                        for (var k = j; k > 0; k--)
                        {
                            Cells[i, k] = Cells[i, k - 1];
                            Cells[i, k - 1] = 0;
                        }
                    }
                }
                else j--;
            }
        }

        public void MoveDown(object sender, EventArgs args)
        {
            for (var i = 0; i < 4; i++)
            {
                F.Position[1, i]++;
            }

            if (IsWrongPlace())
            {
                for (var i = 0; i < 4; i++)
                {
                    F.Position[1, i]--;
                    Cells[F.Position[0, i], F.Position[1, i]] = Figure.Number;
                }

                ClearFilledLines();

                if (T.Interval > 200) T.Interval -= Decrease;
                F = new Figure();
            }

            if (Cells[1, 1] != 0 && GameOver != null)
            {
                T.Stop();
                GameOver();
            }
            if (StateChanged != null) StateChanged();
        }

        public void ShiftFigure(Shift dir)
        {
            for (var i = 0; i < 4; i++)
            {
                if (dir == Shift.Left) F.Position[0, i]--;
                else F.Position[0, i]++;
            }

            if (IsWrongPlace())
            {
                for (var i = 0; i < 4; i++)
                {
                    if (dir == Shift.Left) F.Position[0, i]++;
                    else F.Position[0, i]--;
                }
            }

            if (StateChanged != null) StateChanged();
        }

        public void FlipFigure()
        {
            for (var i = 1; i < 4; i++)
            {
                var x = F.Position[0, i];
                var y = F.Position[1, i];
                F.Position[0, i] = y - F.Position[1, 0] + F.Position[0, 0];
                F.Position[1, i] = -x + F.Position[0, 0] + F.Position[1, 0];
            }

            if (IsWrongPlace())
            {
                for (var i = 1; i < 4; i++)
                {
                    var x = F.Position[0, i];
                    var y = F.Position[1, i];
                    F.Position[0, i] = -y + F.Position[1, 0] + F.Position[0, 0];
                    F.Position[1, i] = x - F.Position[0, 0] + F.Position[1, 0];
                }
            }

            if (StateChanged != null) StateChanged();
        }

        public event Action StateChanged;
        public event Action GameOver;
    }

    public class Presenter
    {
        private readonly MainForm _view;
        private readonly Field _model;

        public Presenter(MainForm view, Field model)
        {
            _view = view;
            _model = model;
        }

        public void Run()
        {
            _model.StateChanged += RefreshView;
            _model.GameOver += _view.GameOver;
            _view.Reload += _model.Reload;
            _view.KeyDown += _view_KeyDown;
            _view.StartButtonClick += _model.Start;
            _view.CreateStartMenu();
            _view.DifficultyChanged += i =>
            {
                switch (i)
                {
                    case 0:
                        _model.Decrease = 10;
                        break;
                    case 1:
                        _model.Decrease = 15;
                        break;
                    case 2:
                        _model.Decrease = 30;
                        break;
                }
            };

            
            _view.Show();
        }

        private void RefreshView()
        {
            _view.Cells = _model.Cells;
            _view.Figure = _model.F.Position;
            _view.FigureNumber = Figure.Number;
            _view.Invalidate();
        }

        private void _view_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    _model.ShiftFigure(Shift.Left);
                    break;
                case Keys.Right:
                    _model.ShiftFigure(Shift.Right);
                    break;
                case Keys.Up:
                    _model.FlipFigure();
                    break;
                case Keys.Down:
                    _model.MoveDown(new object(), EventArgs.Empty);
                    break;
            }
        }
    }

    public partial class MainForm : Form
    {
        public int[,] Cells;
        public int[,] Figure;
        public int FigureNumber;
        public Panel StartMenu;

        private static readonly Brush[] BrushesList = new Brush[]
        {
            Brushes.Green,
            Brushes.Yellow,
            Brushes.Red,
            Brushes.DeepSkyBlue,
            Brushes.Magenta,
        };

        public new void Show()
        {
            Application.Run(this);
        }

        public void GameOver()
        {
            MessageBox.Show("GAME OVER!", "", MessageBoxButtons.OK);
            if (Reload != null) Reload();
        }


        public void MainPaint(object sender, PaintEventArgs args)
        {
            var g = args.Graphics;
            g.DrawString(
                "˂, ˃ - shift figure\n" +
                "˄ - flip figure\n" +
                "˅ - drop figure",
                new Font("Arial", 20, FontStyle.Bold),
                Brushes.Gray,
                new PointF(10, 10));

            for (var i = 0; i < Cells.GetLength(0); i++)
            {
                for (var j = 0; j < Cells.GetLength(1); j++)
                {
                    if (Cells[i, j] > 0)
                    {
                        g.FillRectangle(BrushesList[Cells[i, j] % 5], i * 50, j * 50, 50, 50);
                        g.DrawRectangle(Pens.Black, i * 50, j * 50, 50, 50);
                    }
                }
            }

            if (Figure != null)
                for (var i = 0; i < 4; i++)
                {
                    g.FillRectangle(BrushesList[FigureNumber % 5], Figure[0, i] * 50, Figure[1, i] * 50, 50, 50);
                    g.DrawRectangle(Pens.Black, Figure[0, i] * 50, Figure[1, i] * 50, 50, 50);
                }
        }

        public void CreateStartMenu()
        {
            StartMenu = new Panel();
            StartMenu.Dock = DockStyle.Fill;
            var image = new PictureBox()
            {
                ImageLocation = "tetris_image.jpg",
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            StartMenu.BackColor = Color.DeepSkyBlue;
            var button = new Button()
            {
                Text = "Start",
                Font = new Font("Align", 30, FontStyle.Bold),
                BackColor = Color.Green,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(170, 330),
                Size = new Size(160, 60)
            };
            button.Click += (sender, args) =>
            {
                StartMenu.Visible = false;
                Focus();
                if (StartButtonClick != null) StartButtonClick();
            };
            button.Enabled = false;

            var comboBox = new ComboBox()
            {
                Size = new Size(160, 50),
                Location = new Point(170, 420),
                Font = new Font("Align", 15, FontStyle.Bold),
                BackColor = Color.GreenYellow,
                DrawMode = DrawMode.OwnerDrawVariable,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBox.Items.Add("Easy");
            comboBox.Items.Add("Normal");
            comboBox.Items.Add("Hard");
            StartMenu.Controls.Add(button);
            StartMenu.Controls.Add(comboBox);
            StartMenu.Controls.Add(image);
            Controls.Add(StartMenu);

            comboBox.SelectedIndexChanged += (sender, args) =>
            {
                button.Enabled = true;
                if (DifficultyChanged != null) DifficultyChanged(comboBox.SelectedIndex);
            };
            comboBox.DrawItem += (sender, args) =>
            {
                var g = args.Graphics;
                switch (args.Index)
                {
                    case 0:
                        g.DrawString("Easy", Font = new Font("Align", 15, FontStyle.Bold), Brushes.Green, args.Bounds.X, args.Bounds.Y);
                        break;
                    case 1:
                        g.DrawString("Normal", Font = new Font("Align", 15, FontStyle.Bold), Brushes.Orange, args.Bounds.X, args.Bounds.Y);
                        break;
                    case 2:
                        g.DrawString("Hard", Font = new Font("Align", 15, FontStyle.Bold), Brushes.Red, args.Bounds.X, args.Bounds.Y);
                        break;
                }
            };
        }


        public MainForm()
        {
            Text = "Tetris";
            ClientSize = new Size(500, 600);
            DoubleBuffered = true;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            BackColor = Color.Black;

            Paint += MainPaint;
        }

        public event Action<int> DifficultyChanged;
        public event Action Reload;
        public event Action StartButtonClick;
    }
}
