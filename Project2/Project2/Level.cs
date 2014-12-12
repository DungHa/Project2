using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.IO;


namespace Project2
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Level : IDisposable
    {
        // Physical structure of the level.
        private Tile[,] tiles;
        private Background background;
        // The layer which entities are drawn on top of.
        private const int EntityLayer = 1;

        private SpriteEffects flip = SpriteEffects.None;
        public SpriteEffects Flip
        {
            get { return flip; }
            set { flip = value; }
        }

        // Entities in the level.
        
        
        public Player Player
        {
            get { return player; }
        }
        Player player;

        private bool once = false;

        private List<Orb> gems = new List<Orb>();
        private List<Enemy> enemies = new List<Enemy>();

        // Key locations in the level.        
        private Vector2 start;
        private Point exit = InvalidPosition;
        private static readonly Point InvalidPosition = new Point(-1, -1);

        // Level game state.
        private Random random = new Random(354668); // Arbitrary, but constant seed
        private float cameraPosition;
        public int Score
        {
            get { return score; }
        }
        int score;

        public bool ReachedExit
        {
            get { return reachedExit; }
        }
        bool reachedExit;



        bool moveDirection;
        public bool MoveDirection
        {
            get { return moveDirection; }
            set { moveDirection = value; }
        }
        

        public TimeSpan TimeRemaining
        {
            get { return timeRemaining; }
        }
        TimeSpan timeRemaining;

        private const int PointsPerSecond = 5;

        // Level content.        
        public ContentManager Content
        {
            get { return content; }
        }
        ContentManager content;

        private SoundEffect blastSound;

        private SoundEffect exitReachedSound;
        private SoundEffect timeUp;

        private SoundEffect enemyDie;

        private Viewport viewport;
        public Viewport ViewPort
        {
            get { return viewport; }
        }


        float blastMoveSpeed;
        public float BlastMoveSpeed
        {
            get { return blastMoveSpeed; }
            set { blastMoveSpeed = value; }
        }
        
        Texture2D blastTexture;
        List<Blast> Blasts;

        // The rate of fire of the player laser
        TimeSpan fireTime;
        TimeSpan previousFireTime;


        /// <summary>
        /// Constructs a new level.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider that will be used to construct a ContentManager.
        /// </param>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        public Level(IServiceProvider serviceProvider, Stream fileStream, int levelIndex,
            Viewport viewport)
        {
            // Create a new content manager to load content used just by this level.
            content = new ContentManager(serviceProvider, "Content");


            blastTexture = Content.Load<Texture2D>("Sprites/blast");
            Blasts = new List<Blast>();
            // Set the laser to fire every quarter second
            fireTime = TimeSpan.FromSeconds(2.0f);
            blastMoveSpeed = -10.0f;
            timeRemaining = TimeSpan.FromMinutes(2.0f);
            moveDirection = false;
            LoadTiles(fileStream);

            

            // Load background layer textures. For now, all levels must
            // use the same backgrounds and only use the left-most part of them.
            string backgroundLevel = string.Format("Backgrounds/level{0}", levelIndex + 1);
            background = new Background(Content, backgroundLevel, 0.2f);

            

            // Load sounds.
            exitReachedSound = Content.Load<SoundEffect>("Sounds/ExitReached");
            timeUp = content.Load<SoundEffect>("Sounds/timeUp");
            blastSound = content.Load<SoundEffect>("Sounds/blastSound");
            enemyDie = content.Load<SoundEffect>("Sounds/enemyDie");
        }


        private void AddBlast(Vector2 position, float blastMoveSpeed)
        {
            Blast newBlast = new Blast();
            newBlast.Initialize(ViewPort, blastTexture, position, blastMoveSpeed);
            Blasts.Add(newBlast);
        }

        private void UpdateBlast()
        {
            for (int i = Blasts.Count - 1; i >= 0; i--)
            {
                Blasts[i].Update();
                if (Blasts[i].Active == false)
                {
                    Blasts.RemoveAt(i);
                }
            }

        }



        /// <summary>
        /// Iterates over every tile in the structure file and loads its
        /// appearance and behavior. This method also validates that the
        /// file is well-formed with a player start point, exit, etc.
        /// </summary>
        /// <param name="fileStream">
        /// A stream containing the tile data.
        /// </param>
        private void LoadTiles(Stream fileStream)
        {
            // Load the level and ensure all of the lines are the same length.
            int width;
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line = reader.ReadLine();
                width = line.Length;
                while (line != null)
                {
                    lines.Add(line);
                    if (line.Length != width)
                        throw new Exception(String.Format("The length of line {0} is different from all preceeding lines.", lines.Count));
                    line = reader.ReadLine();
                }
            }

            // Allocate the tile grid.
            tiles = new Tile[width, lines.Count];

            // Loop over every tile position,
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    // to load each tile.
                    char tileType = lines[y][x];
                    tiles[x, y] = LoadTile(tileType, x, y);
                }
            }

            // Verify that the level has a beginning and an end.
            
            if (Player == null)
              throw new NotSupportedException("A level must have a starting point.");
            
            if (exit == InvalidPosition)
                throw new NotSupportedException("A level must have an exit.");

        }

        /// <summary>
        /// Loads an individual tile's appearance and behavior.
        /// </summary>
        /// <param name="tileType">
        /// The character loaded from the structure file which
        /// indicates what should be loaded.
        /// </param>
        /// <param name="x">
        /// The X location of this tile in tile space.
        /// </param>
        /// <param name="y">
        /// The Y location of this tile in tile space.
        /// </param>
        /// <returns>The loaded tile.</returns>
        private Tile LoadTile(char tileType, int x, int y)
        {
            switch (tileType)
            {
                // Blank space
                case '.':
                    return new Tile(null, TileCollision.Passable);

                // Exit
                case 'X':
                    return LoadExitTile(x, y);

                // Gem
                    
                case 'G':
                    return LoadGemTile(x, y, false, false);

                // Power-up gem
                case 'P':
                    return LoadGemTile(x, y, true, false);
                    
                case 'S':
                    return LoadGemTile(x, y, false, true);

                // Floating platform
                case '-':
                    return LoadTile("Platform", TileCollision.Platform);

                    
                // Various enemies
                case 'A':
                return LoadEnemyTile(x, y, "MonsterVer1");
                    
                case 'B':
                return LoadEnemyTile(x, y, "MonsterVer2");
                    /*
                case 'C':
                //return LoadEnemyTile(x, y, "MonsterC");
                case 'D':
                //return LoadEnemyTile(x, y, "MonsterD");
                    */

                // Platform block
                case '~':
                    return LoadVarietyTile("BlockB", 2, TileCollision.Platform);

                // Passable block
                case ':':
                    return LoadVarietyTile("BlockB", 2, TileCollision.Passable);

                // Player 1 start point
                case '1':
                    return LoadStartTile(x, y);

                // Impassable block
                case '#':
                    return LoadVarietyTile("Ground", 5, TileCollision.Impassable);

                // Unknown tile type character
                default:
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
            }
        }

        /// <summary>
        /// Creates a new tile. The other tile loading methods typically chain to this
        /// method after performing their special logic.
        /// </summary>
        /// <param name="name">
        /// Path to a tile texture relative to the Content/Tiles directory.
        /// </param>
        /// <param name="collision">
        /// The tile collision type for the new tile.
        /// </param>
        /// <returns>The new tile.</returns>
        
        private Tile LoadTile(string name, TileCollision collision)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
        }
        


        /// <summary>
        /// Loads a tile with a random appearance.
        /// </summary>
        /// <param name="baseName">
        /// The content name prefix for this group of tile variations. Tile groups are
        /// name LikeThis0.png and LikeThis1.png and LikeThis2.png.
        /// </param>
        /// <param name="variationCount">
        /// The number of variations in this group.
        /// </param>
        
        private Tile LoadVarietyTile(string baseName, int variationCount, TileCollision collision)
        {
            int index = random.Next(variationCount);
            return LoadTile(baseName + index, collision);
        }
         


        /// <summary>
        /// Instantiates a player, puts him in the level, and remembers where to put him when he is resurrected.
        /// </summary>
        private Tile LoadStartTile(int x, int y)
        {
            if (Player != null)
                throw new NotSupportedException("A level may only have one starting point.");

            start = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            player = new Player(this, start);
            
            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Remembers the location of the level's exit.
        /// </summary>
        private Tile LoadExitTile(int x, int y)
        {
            if (exit != InvalidPosition)
                throw new NotSupportedException("A level may only have one exit.");

            exit = GetBounds(x, y).Center;

            return LoadTile("Exit", TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates an enemy and puts him in the level.
        /// </summary>
        private Tile LoadEnemyTile(int x, int y, string spriteSet)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            enemies.Add(new Enemy(this, position, spriteSet));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a gem and puts it in the level.
        /// </summary>
        
        private Tile LoadGemTile(int x, int y, bool isPowerUp, bool isSpecial)
        {
            Point position = GetBounds(x, y).Center;
            gems.Add(new Orb(this, new Vector2(position.X, position.Y), isPowerUp, isSpecial));

            return new Tile(null, TileCollision.Passable);
        }
        
        /// <summary>
        /// Unloads the level content.
        /// </summary>
        public void Dispose()
        {
            Content.Unload();
        }




        /// <summary>
        /// Gets the collision mode of the tile at a particular location.
        /// This method handles tiles outside of the levels boundries by making it
        /// impossible to escape past the left or right edges, but allowing things
        /// to jump beyond the top of the level and fall off the bottom.
        /// </summary>
        
        public TileCollision GetCollision(int x, int y)
        {
            // Prevent escaping past the level ends.
            if (x < 0 || x >= Width)
                return TileCollision.Impassable;
            // Allow jumping past the level top and falling through the bottom.
            if (y < 0 || y >= Height)
                return TileCollision.Passable;

            return tiles[x, y].Collision;
        }
        
        /// <summary>
        /// Gets the bounding rectangle of a tile in world space.
        /// </summary>        
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        /// <summary>
        /// Width of level measured in tiles.
        /// </summary>
        public int Width
        {
            get { return tiles.GetLength(0); }
        }

        /// <summary>
        /// Height of the level measured in tiles.
        /// </summary>
        public int Height
        {
            get { return tiles.GetLength(1); }
        }



        /// <summary>
        /// Updates all objects in the world, performs collision between them,
        /// and handles the time limit with scoring.
        /// </summary>
        public void Update(
            GameTime gameTime,
            KeyboardState keyboardState,
            GamePadState gamePadState)

        {

            if (TimeRemaining == TimeSpan.Zero && !once)
            {
                MediaPlayer.Pause();
                timeUp.Play();
                once = true;
            }

            // Pause while the player is dead or time is expired.
            if (!Player.IsAlive || TimeRemaining == TimeSpan.Zero)
            {
                // Still want to perform physics on the player.
                Player.ApplyPhysics(gameTime);
            }
            else if (ReachedExit)
            {
                // Animate the time being converted into points.
                int seconds = (int)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 100.0f);
                seconds = Math.Min(seconds, (int)Math.Ceiling(TimeRemaining.TotalSeconds));
                timeRemaining -= TimeSpan.FromSeconds(seconds);
                score += seconds * PointsPerSecond;
            }
            else
            {
                timeRemaining -= gameTime.ElapsedGameTime;
                Player.Update(gameTime, keyboardState, gamePadState);
                UpdateGems(gameTime);

                // Falling off the bottom of the level kills the player.
                if (Player.BoundingRectangle.Top >= Height * Tile.Height)
                    OnPlayerKilled(null);

                UpdateEnemies(gameTime);

                // The player has reached the exit if they are standing on the ground and
                // his bounding rectangle contains the center of the exit tile. They can only
                // exit when they have collected all of the gems.
                if (Player.IsAlive &&
                    Player.IsOnGround &&
                    Player.BoundingRectangle.Contains(exit)&&
                    Score > 1000&&
                    enemies.Count == 0)
                {
                    OnExitReached();
                }
            }


            // Clamp the time remaining at zero.
            if (timeRemaining < TimeSpan.Zero)
                timeRemaining = TimeSpan.Zero;

            HandleBlast(gameTime,keyboardState,gamePadState);
            UpdateBlast();
            
        }


        private void HandleBlast(GameTime gameTime, KeyboardState keyboardState,
            GamePadState gamePadState)
        {
            // fire when triggered
            if ((gameTime.TotalGameTime - previousFireTime > fireTime) &&
                (keyboardState.IsKeyDown(Keys.J) || gamePadState.IsButtonDown(Buttons.X)))
            {
                blastSound.Play();
                // Reset time
                previousFireTime = gameTime.TotalGameTime;

                // missile location at the player position
                if(moveDirection)
                    AddBlast(player.Position + new Vector2(50,-30), blastMoveSpeed);
                else if(!moveDirection)
                    AddBlast(player.Position + new Vector2(-50, -30), blastMoveSpeed);

            }
        }


        /// <summary>
        /// Animates each gem and checks to allows the player to collect them.
        /// </summary>
         
        private void UpdateGems(GameTime gameTime)
        {
            for (int i = 0; i < gems.Count; ++i)
            {
                Orb gem = gems[i];

                gem.Update(gameTime);

                if (gem.BoundingCircle.Intersects(Player.BoundingRectangle))
                {
                    gems.RemoveAt(i--);
                    OnGemCollected(gem, Player);
                }
            }
        }
          

        /// <summary>
        /// Animates each enemy and allow them to kill the player.
        /// </summary>
        
        private void UpdateEnemies(GameTime gameTime)
        {
            if (Player.IsSpecial)
             {
               OnSpecial();
             }


            if (enemies != null && enemies.Count > 0)
            {
                for (int e = 0; e < enemies.Count; ++e)
                {
                    enemies[e].Update(gameTime);

                    // Touching an enemy instantly kills the player
                    for (int i = 0; i < Blasts.Count; i++)
                    {
                        if (enemies[e].BoundingRectangle.Intersects(Blasts[i].BoundingRectangle))
                        {
                            OnEnemyKilled(enemies[e], Player, e, gameTime);
                            Blasts.RemoveAt(i);
                        }
                    }

                    if(e >= 0 && e < enemies.Count)
                    {
                        if (enemies[e].IsAlive && enemies[e].BoundingRectangle.Intersects(Player.BoundingRectangle))
                        {
                            if (Player.IsPoweredUp)
                            {
                                OnEnemyKilled(enemies[e], Player, e, gameTime);
                            }                          
                            else
                            {
                                OnPlayerKilled(enemies[e]);
                            }
                        }
                    }


                }
            }
        }
        private void OnEnemyKilled(Enemy enemy, Player killedBy, int e, GameTime gameTime)
        {           
            enemies.RemoveAt(e);
            enemy.OnKilled(killedBy);
            enemyDie.Play();
            score += 30;
            
        }

        private void OnSpecial()
        {
            if (enemies != null && enemies.Count > 0)
            {
                for (int e = 0; e < enemies.Count; ++e)
                {
                    enemies.RemoveAt(e);
                    score += 30;
                }
            }
        }


        
        /// <summary>
        /// Called when a gem is collected.
        /// </summary>
        /// <param name="gem">The gem that was collected.</param>
        /// <param name="collectedBy">The player who collected this gem.</param>
        private void OnGemCollected(Orb gem, Player collectedBy)
        {
            score += gem.PointValue;

            gem.OnCollected(collectedBy);
        }
        
        /// <summary>
        /// Called when the player is killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This is null if the player was not killed by an
        /// enemy, such as when a player falls into a hole.
        /// </param>
        
        private void OnPlayerKilled(Enemy killedBy)
        {
            Player.OnKilled(killedBy);
        }
        
        
        /// <summary>
        /// Called when the player reaches the level's exit.
        /// </summary>
        private void OnExitReached()
        {
            //Player.OnReachedExit();//animation
            MediaPlayer.Pause();
            exitReachedSound.Play();
            reachedExit = true;
        }
        
        /// <summary>
        /// Restores the player to the starting point to try the level again.
        /// </summary>
        public void StartNewLife()
        {
            Player.Reset(start);
        }


        
        

        /// <summary>
        /// Draw everything in the level from background to foreground.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {

            spriteBatch.Begin();
            background.Draw(spriteBatch, cameraPosition);
            spriteBatch.End();

            ScrollCamera(spriteBatch.GraphicsDevice.Viewport);
            Matrix cameraTransform = Matrix.CreateTranslation(-cameraPosition, 0.0f, 0.0f);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.Default,
                        RasterizerState.CullCounterClockwise, null, cameraTransform);

            DrawTiles(spriteBatch);
            
            foreach (Orb gem in gems)
                gem.Draw(gameTime, spriteBatch);

            Player.Draw(gameTime, spriteBatch);

            foreach (Blast blast in Blasts)
                blast.Draw(spriteBatch, flip);

            foreach (Enemy enemy in enemies)
                enemy.Draw(gameTime, spriteBatch);

            
            spriteBatch.End();


        }

        /// <summary>
        /// Draws each tile in the level.
        /// </summary>
        private void DrawTiles(SpriteBatch spriteBatch)
        {
            // Calculate the visible range of tiles.
            int left = (int)Math.Floor(cameraPosition / Tile.Width);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width / Tile.Width;
            right = Math.Min(right, Width - 1);

            // For each tile position
            for (int y = 0; y < Height; ++y)
            {
                for (int x = left; x <= right; ++x)
                {
                    // If there is a visible tile in that position
                    Texture2D texture = tiles[x, y].Texture;
                    if (texture != null)
                    {
                        // Draw it in screen space.
                        Vector2 position = new Vector2(x, y) * Tile.Size;
                        spriteBatch.Draw(texture, position, Color.White);
                        
                    }
                }
            }
        }

        private void ScrollCamera(Viewport viewport)
        {

            const float ViewMargin = 0.35f;


            // Calculate the edges of the screen.
            float marginWidth = viewport.Width * ViewMargin;
            float marginLeft = cameraPosition + marginWidth;
            float marginRight = cameraPosition + viewport.Width - marginWidth;

            // Calculate how far to scroll when the player is near the edges of the screen.
            float cameraMovement = 0.0f;
            
            if (Player.Position.X < marginLeft)
                cameraMovement = Player.Position.X - marginLeft;
            else if (Player.Position.X > marginRight)
                cameraMovement = Player.Position.X - marginRight;
            
            // Update the camera position, but prevent scrolling off the ends of the level.
            float maxCameraPosition = Tile.Width * Width - viewport.Width;
            cameraPosition = MathHelper.Clamp(cameraPosition + cameraMovement, 0.0f, maxCameraPosition);
        }
    }
}