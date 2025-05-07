using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.Timer;
using static System.Windows.Forms.Label;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ChessOOP
{
    public class Timers
    {
        public Label blackMinutes { get; set; }
        public Label blackSeconds { get; set; }
        public Label blackColon { get; set; }
        public System.Windows.Forms.Timer timerBlack { get; set; }
        public Label whiteMinutes { get; set; }
        public Label whiteSeconds { get; set; }
        public Label whiteColon { get; set; }
        public bool GameOver { get; private set; } = false;
        public System.Windows.Forms.Timer timerWhite { get; set; }
        public Timers()
        {
            InitializeTimers();
        }
        private void InitializeTimers()
        {
            this.timerBlack = new System.Windows.Forms.Timer();
            this.timerBlack.Interval = 1000;
            this.timerBlack.Tick += new EventHandler(BlackEventHandler);
            this.blackMinutes = new Label
            {
                Text = "10",
                Location = new Point(890, 30),
                Height = 100,
                Font = new Font("Times New Roman", 50, FontStyle.Bold),
            };
            this.blackColon = new Label
            {
                Text = ":",
                Location = new Point(970, 25),
                Height = 100,
                Width = 40,
                Font = new Font("Times New Roman", 50, FontStyle.Bold),
            };
            this.blackSeconds = new Label
            {
                Text = "00",
                Location = new Point(1000, 30),
                Height = 100,
                Font = new Font("Times New Roman", 50, FontStyle.Bold),
            };


            this.timerWhite = new System.Windows.Forms.Timer();
            this.timerWhite.Interval = 1000;
            this.timerWhite.Tick += new EventHandler(WhiteEventHandler);
            this.whiteMinutes = new Label
            {
                Text = "10",
                Location = new Point(890, 720),
                Height = 100,
                Font = new Font("Times New Roman", 50, FontStyle.Bold),
            };
            this.whiteColon = new Label
            {
                Text = ":",
                Location = new Point(970, 715),
                Height = 100,
                Width = 40,
                Font = new Font("Times New Roman", 50, FontStyle.Bold),
            };
            this.whiteSeconds = new Label
            {
                Text = "00",
                Location = new Point(1000, 720),
                Height = 100,
                Font = new Font("Times New Roman", 50, FontStyle.Bold),
            };
        }
        private void BlackEventHandler(Object myObject, EventArgs myEventArgs)
        {
            if (blackMinutes.Visible)
            {
                int blackSeconds_int = int.Parse(blackSeconds.Text);
                int blackMinutes_int = int.Parse(blackMinutes.Text);

                if (blackMinutes_int == 0 && blackSeconds_int == 1)
                {
                    timerBlack.Enabled = false;
                    timerWhite.Enabled = false;
                    GameOver = true;
                }

                if (blackSeconds_int == 0)
                {
                    blackSeconds_int = 59;
                    blackMinutes_int--;
                    if (blackMinutes_int < 10)
                    {
                        blackMinutes.Text = "0" + blackMinutes_int.ToString();
                    }
                    else
                    {
                        blackMinutes.Text = blackMinutes_int.ToString();
                    }
                    blackSeconds.Text = blackSeconds_int.ToString();
                }
                else
                {
                    blackSeconds_int--;
                    if (blackSeconds_int < 10)
                    {
                        blackSeconds.Text = "0" + blackSeconds_int.ToString();
                    }
                    else
                    {
                        blackSeconds.Text = blackSeconds_int.ToString();
                    }
                    if (blackMinutes_int < 10)
                    {
                        blackMinutes.Text = "0" + blackMinutes_int.ToString();
                    }
                    else
                    {
                        blackMinutes.Text = blackMinutes_int.ToString();
                    }
                }
            }
        }
        private void WhiteEventHandler(Object myObject, EventArgs myEventArgs)
        {
            if (whiteMinutes.Visible)
            {
                int whiteSeconds_int = int.Parse(whiteSeconds.Text);
                int whiteMinutes_int = int.Parse(whiteMinutes.Text);

                if (whiteMinutes_int == 0 && whiteSeconds_int == 1)
                {
                    timerBlack.Enabled = false;
                    timerWhite.Enabled = false;
                    GameOver = true;
                }

                if (whiteSeconds_int == 0)
                {
                    whiteSeconds_int = 59;
                    whiteMinutes_int--;
                    if (whiteMinutes_int < 10)
                    {
                        whiteMinutes.Text = "0" + whiteMinutes_int.ToString();
                    }
                    else
                    {
                        whiteMinutes.Text = whiteMinutes_int.ToString();
                    }
                    whiteSeconds.Text = whiteSeconds_int.ToString();
                }
                else
                {
                    whiteSeconds_int--;
                    if (whiteSeconds_int < 10)
                    {
                        whiteSeconds.Text = "0" + whiteSeconds_int.ToString();
                    }
                    else
                    {
                        whiteSeconds.Text = whiteSeconds_int.ToString();
                    }
                    if (whiteMinutes_int < 10)
                    {
                        whiteMinutes.Text = "0" + whiteMinutes_int.ToString();
                    }
                    else
                    {
                        whiteMinutes.Text = whiteMinutes_int.ToString();
                    }
                }
            }
        }
    }
}
