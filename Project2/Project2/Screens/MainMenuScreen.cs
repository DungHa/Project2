

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
#endregion

namespace Project2
{
    /// <summary>
    /// The main menu screen is the first thing displayed when the game starts up.
    /// </summary>
    class MainMenuScreen : MenuScreen
    {
        #region Initialization

        GameMain gameMain;
        IServiceProvider services;
        Viewport viewport;
        SpriteBatch spriteBatch;

        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        public MainMenuScreen(GameMain gameMain, IServiceProvider services, Viewport viewport, SpriteBatch spriteBatch)
            : base("The Zen")
        {
            this.gameMain = gameMain;
            this.services = services;
            this.viewport = viewport;
            this.spriteBatch = spriteBatch;
            // Create our menu entries.
            MenuEntry playGameMenuEntry = new MenuEntry("Play Game");
            //MenuEntry optionsMenuEntry = new MenuEntry("Options");
            MenuEntry exitMenuEntry = new MenuEntry("Exit");

            MenuEntry howToEntry = new MenuEntry("Help");

            MenuEntry aboutEntry = new MenuEntry("About");

            // Hook up menu event handlers.
            playGameMenuEntry.Selected += PlayGameMenuEntrySelected;
            //optionsMenuEntry.Selected += OptionsMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

            howToEntry.Selected += OnHowTo;

            aboutEntry.Selected += OnAbout;

            // Add entries to the menu.
            MenuEntries.Add(playGameMenuEntry);
            MenuEntries.Add(aboutEntry);
            MenuEntries.Add(howToEntry);
            MenuEntries.Add(exitMenuEntry);

            
        }


        #endregion

        #region Handle Input


        /// <summary>
        /// Event handler for when the Play Game menu entry is selected.
        /// </summary>
        void PlayGameMenuEntrySelected(object sender, PlayerIndexEventArgs e)
        {
            LoadingScreen.Load(ScreenManager, true, e.PlayerIndex,
                               new GameplayScreen(gameMain, services, viewport, spriteBatch));
        }

        
        protected void OnHowTo(object sender, PlayerIndexEventArgs e)
        {
            const string message = "Movement: WASD/ARROW, Directional Pad/Left Analog"+
                "\nAttack: J(Keyboard), X(Gamepad)"+
                "\nDash: K(Keyboard, B(Gamepad)"+
                "\nUse Powerup: L(Keyboard), Y(Gamepad)"+
                "\nJump: Space(Keyboard, A(Gamepad)"+
                "\nPause: Esc(Keyboard), Start(Gamepad)"+
                "\n\nPowerup = Red Orb(Invincible)"+
                "\nSpecial = Black Orb(Clears ALL Enemies)"+
                "\n\nTo complete level: reach the exit"+
                "\nachieve atleast 1000 pts"+
                "\nand defeat ALL enemies on the level!";

            MessageBoxScreen helpBox = new MessageBoxScreen(message, false);

            ScreenManager.AddScreen(helpBox, PlayerIndex.One);
            
        }

        protected void OnAbout(object sender, PlayerIndexEventArgs e)
        {
            const string message = "DEJ enterprise brings you the hero"+
                                   "\nfrom its commercial success 'The Last Starfighter';"+
                                   "\nSee the man before the pilot!!"+
                                   "\n\nNavigate challenging terrains and defeat ALL enemies"+
                                   "\nto survive all the way to the finish within just 2 MINUTES!";

            MessageBoxScreen aboutBox = new MessageBoxScreen(message, false);

            ScreenManager.AddScreen(aboutBox, PlayerIndex.One);

        }


        


        /// <summary>
        /// When the user cancels the main menu, ask if they want to exit the sample.
        /// </summary>
        protected override void OnCancel(PlayerIndex playerIndex)
        {
            const string message = "Its not too late to change your mind!";

            MessageBoxScreen confirmExitMessageBox = new MessageBoxScreen(message);

            confirmExitMessageBox.Accepted += ConfirmExitMessageBoxAccepted;

            ScreenManager.AddScreen(confirmExitMessageBox, playerIndex);
        }


        /// <summary>
        /// Event handler for when the user selects ok on the "are you sure
        /// you want to exit" message box.
        /// </summary>
        void ConfirmExitMessageBoxAccepted(object sender, PlayerIndexEventArgs e)
        {
            ScreenManager.Game.Exit();
        }


        #endregion
    }
}
