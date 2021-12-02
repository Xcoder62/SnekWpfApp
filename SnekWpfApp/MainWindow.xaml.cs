using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace SnekWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SpeechSynthesizer sS = new SpeechSynthesizer();
        private DispatcherTimer gameTickTimer = new DispatcherTimer();
        // starting the game constants

        public bool voiceOn { get; set; }
        const int SnakeSquareSize = 20;
        const int SnakeStartLength = 3;
        const int SnakeStartSpeed = 400;
        const int SnakeSpeedThreshold = 100;
        const int MaxHighscoreListEntryCount = 5;

        // Food constants
        private UIElement snakeFood = null;
        private SolidColorBrush foodBrush = Brushes.Red;

        private Random rnd = new Random();

        private SolidColorBrush snakeBodyBrush = Brushes.Green;
        private SolidColorBrush snakeHeadBrush = Brushes.YellowGreen;
        private List<SnakePart> snakeParts = new List<SnakePart>();

        public enum SnakeDirection { Left, Right, Up, Down };
        private SnakeDirection snakeDirection = SnakeDirection.Right;
        private int snakeLength;
        private int currentScore = 0;




        public MainWindow()
        {
            InitializeComponent();
            gameTickTimer.Tick += GameTickTimer_Tick;
            LoadHighscoreList();
        }

        private void StartNewGame()
        {

            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Collapsed;
            bdrEndOfGame.Visibility = Visibility.Collapsed;


            // remove all potential dead snake parts and leftover food
            foreach(SnakePart snakeBodyPart in snakeParts)
            {
                if(snakeBodyPart.UiElement != null)
                {
                    GameArea.Children.Remove(snakeBodyPart.UiElement);
                }
            }
            snakeParts.Clear();
            if(snakeFood != null)
            {
                GameArea.Children.Remove(snakeFood);
            }

            currentScore = 0;
            snakeLength = SnakeStartLength;
            snakeDirection = SnakeDirection.Right;
            snakeParts.Add(new SnakePart() { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);

            // Draw the snake
            // give it some food
            DrawSnake();
            DrawSnakeFood();

            // Update the title
            UpdateGameStatus();

            // go go go
            gameTickTimer.IsEnabled = true;


        }

        private void EndGame()
        {
            bool isNewHighscore = false;
            if(currentScore > 0)
            {
                int lowestHighscore = (this.HighscoreList.Count > 0 ? this.HighscoreList.Min(x => x.Score) : 0);
                if ((currentScore > lowestHighscore) || (this.HighscoreList.Count < MaxHighscoreListEntryCount))
                {
                    bdrNewHighscore.Visibility = Visibility.Visible;
                    txtPlayerName.Focus();
                    isNewHighscore = true;
                }
            }
            if (!isNewHighscore)
            {
                tbFinalScore.Text = currentScore.ToString();
                bdrEndOfGame.Visibility = Visibility.Visible;
            }
            gameTickTimer.IsEnabled = false;
            if (cbIsVoiceOn.IsChecked == true)
            {
                SpeakEndOfGameInfo(isNewHighscore);
            }
            
        }

        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
            //StartNewGame();
        }

        public ObservableCollection<SnakeHighscore> HighscoreList
        {
            get; set;
        } = new ObservableCollection<SnakeHighscore>();

        private void LoadHighscoreList()
        {

            // TODO create xml file if it doesn't exist.
            if(File.Exists("snake_highscorelist.xml"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SnakeHighscore>));
                using (Stream reader = new FileStream("snake_highscorelist.xml", FileMode.Open))
                {
                    List<SnakeHighscore> tempList = (List<SnakeHighscore>)serializer.Deserialize(reader);
                    this.HighscoreList.Clear();
                    foreach(var item in tempList.OrderByDescending(x => x.Score))
                    {
                        this.HighscoreList.Add(item);
                    }
                }
            }
        }

        private void SaveHighscoreList()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SnakeHighscore>));
            using(Stream writer = new FileStream("snake_highscorelist.xml", FileMode.Create))
            {
                serializer.Serialize(writer, this.HighscoreList);
            }
        }

        private void DrawSnake()
        {
            foreach(SnakePart snakePart in snakeParts)
            {
                if(snakePart.UiElement == null)
                {
                    snakePart.UiElement = new Rectangle()
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = (snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush)
                    };

                    GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }

        private void DrawGameArea()
        {
            bool doneDrawingBackground = false;
            int nextX = 0, nextY = 0;
            int rowCounter = 0;
            bool nextIsOdd = false;

            while(doneDrawingBackground == false)
            {
                Rectangle rect = new Rectangle
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = nextIsOdd ? Brushes.White : Brushes.Black
                };

                GameArea.Children.Add(rect);
                Canvas.SetTop(rect, nextY);
                Canvas.SetLeft(rect, nextX);

                nextIsOdd = !nextIsOdd;
                nextX += SnakeSquareSize;
                if(nextX >= GameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += SnakeSquareSize;
                    rowCounter++;
                    nextIsOdd = (rowCounter % 2 != 0);
                }

                if(nextY >= GameArea.ActualHeight)
                {
                    doneDrawingBackground = true;
                }


            }
        }

        private void MoveSnake()
        {
            // Remove the last part of the snake, in preparation of the new part added below
            while(snakeParts.Count >= snakeLength)
            {
                GameArea.Children.Remove(snakeParts[0].UiElement);
                snakeParts.RemoveAt(0);
            }
            // new square will be head
            // old sqauares will be painted regularly, while head gets head color.
            foreach(SnakePart snakePart in snakeParts)
            {
                (snakePart.UiElement as Rectangle).Fill = snakeBodyBrush;
                snakePart.IsHead = false;
            }

            // Determine in which direction to expand the snake, based on the current direction
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;

            switch(snakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= SnakeSquareSize;
                    break;
                case SnakeDirection.Right:
                    nextX += SnakeSquareSize;
                    break;
                case SnakeDirection.Up:
                    nextY -= SnakeSquareSize;
                    break;
                case SnakeDirection.Down:
                    nextY += SnakeSquareSize;
                    break;
            }

            // Add the head part to our list of snake parts
            snakeParts.Add(new SnakePart() { Position = new Point(nextX, nextY), IsHead = true });
            // now draw the snek
            DrawSnake();
            // Collision
            DoCollisionCheck();


        }

        private void EatSnakeFood()
        {
            if (cbIsVoiceOn.IsChecked == true)
            {
                sS.SpeakAsync("yummy");
            }
            
            snakeLength++;
            currentScore++;
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)gameTickTimer.Interval.TotalMilliseconds - (currentScore * 2));
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            GameArea.Children.Remove(snakeFood);
            DrawSnakeFood();
            UpdateGameStatus();
        }

        private void UpdateGameStatus()
        {
            //this.Title = "SnakeWPF - Score: " + currentScore + " - Game speed: " + gameTickTimer.Interval.TotalMilliseconds;
            this.tbStatusScore.Text = currentScore.ToString();
            this.tbStatusSpeed.Text = gameTickTimer.Interval.TotalMilliseconds.ToString();
        }

        //private void EndGame()
        //{
        //    gameTickTimer.IsEnabled = false;
        //    MessageBox.Show("Oh No!, You died!\n\nTo Start a new game, press the space bar...", "SnekWPF.exe");
        //}

        private void SpeakEndOfGameInfo(bool isNewHighscore)
        {
            PromptBuilder promptBuilder = new PromptBuilder();

            promptBuilder.StartStyle(new PromptStyle()
            {
                Emphasis = PromptEmphasis.Reduced,
                Rate = PromptRate.Slow,
                Volume = PromptVolume.ExtraLoud
            });
            promptBuilder.AppendText("oh no");
            promptBuilder.AppendBreak(TimeSpan.FromMilliseconds(200));
            promptBuilder.AppendText("you died");
            promptBuilder.EndStyle();

            if (isNewHighscore)
            {
                promptBuilder.AppendBreak(TimeSpan.FromMilliseconds(500));
                promptBuilder.StartStyle(new PromptStyle()
                {
                    Emphasis = PromptEmphasis.Moderate,
                    Rate = PromptRate.Medium,
                    Volume = PromptVolume.Medium
                });
                promptBuilder.AppendText("new high score:");
                promptBuilder.AppendBreak(TimeSpan.FromMilliseconds(200));
                promptBuilder.AppendTextWithHint(currentScore.ToString(), SayAs.NumberCardinal);
                promptBuilder.EndStyle();
            }
            sS.SpeakAsync(promptBuilder);
        }

        private void DoCollisionCheck()
        {
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];

            if((snakeHead.Position.X == Canvas.GetLeft(snakeFood)) && (snakeHead.Position.Y == Canvas.GetTop(snakeFood)))
            {
                EatSnakeFood(); // TODO
                return;
            }

            if ((snakeHead.Position.Y < 0) || (snakeHead.Position.Y >= GameArea.ActualHeight) || (snakeHead.Position.X < 0) || (snakeHead.Position.X >= GameArea.ActualWidth))
            {
                EndGame(); // TODO
            }

            foreach(SnakePart snakeBodyPart in snakeParts.Take(snakeParts.Count - 1))
            {
                if ((snakeHead.Position.X == snakeBodyPart.Position.X) && (snakeHead.Position.Y == snakeBodyPart.Position.Y))
                {
                    EndGame();
                }
            }


        }




        private Point GetNextFoodPosition()
        {
            int maxX = (int)(GameArea.ActualWidth / SnakeSquareSize);
            int maxY = (int)(GameArea.ActualHeight / SnakeSquareSize);
            int foodX = rnd.Next(0, maxX) * SnakeSquareSize;
            int foodY = rnd.Next(0, maxY) * SnakeSquareSize;

            foreach(SnakePart snakePart in snakeParts)
            {
                if((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
                {
                    return GetNextFoodPosition();
                }
            }

            return new Point(foodX, foodY);
        }

        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition();
            snakeFood = new Ellipse()
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = foodBrush
            };

            GameArea.Children.Add(snakeFood);
            Canvas.SetTop(snakeFood, foodPosition.Y);
            Canvas.SetLeft(snakeFood, foodPosition.X);

        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            SnakeDirection originalSnakeDirection = snakeDirection;
            switch(e.Key)
            {
                case Key.Up:
                    if(snakeDirection != SnakeDirection.Down)
                    {
                        snakeDirection = SnakeDirection.Up;
                        
                    }
                    break;

                case Key.Down:
                    if(snakeDirection != SnakeDirection.Up)
                    {
                        snakeDirection = SnakeDirection.Down;
                    }
                    break;

                case Key.Left:
                    if(snakeDirection != SnakeDirection.Right)
                    {
                        snakeDirection = SnakeDirection.Left;
                    }
                    break;

                case Key.Right:
                    if(snakeDirection != SnakeDirection.Left)
                    {
                        snakeDirection = SnakeDirection.Right;
                    }
                    break;
                case Key.Enter:
                    StartNewGame();
                    break;
            }

            if(snakeDirection != originalSnakeDirection)
            {
                MoveSnake();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnShowHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }

        private void btnAddToHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = 0;
            // Where should new entry be inserted?
            if ((this.HighscoreList.Count > 0) && (currentScore < this.HighscoreList.Max(x => x.Score)))
            {
                SnakeHighscore justAbove = this.HighscoreList.OrderByDescending(x => x.Score).First(x => x.Score >= currentScore);
                if (justAbove != null)
                {
                    newIndex = this.HighscoreList.IndexOf(justAbove) + 1;
                }
            }
                // Create & insert the new entry
                this.HighscoreList.Insert(newIndex, new SnakeHighscore() { PlayerName = txtPlayerName.Text, Score = currentScore });
                // amt of entries should not exceed maximum
                while (this.HighscoreList.Count > MaxHighscoreListEntryCount)
                {
                    this.HighscoreList.RemoveAt(MaxHighscoreListEntryCount);
                }

                SaveHighscoreList();

                bdrNewHighscore.Visibility = Visibility.Collapsed;
                bdrHighscoreList.Visibility = Visibility.Visible;

            
        }

        private void cbIsVoiceOn_Click(object sender, RoutedEventArgs e)
        {
            window.Focus();
        }
    }
}
