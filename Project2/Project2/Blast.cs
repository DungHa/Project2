using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Project2
{
    class Blast
    {
        // Image representing the missile
        public Texture2D blast;

        // Position of the missile relative to the upper left side of the screen
        public Vector2 Position;

        // State of the missile
        public bool Active;

        // The amount of damage the missile can inflict to an enemy
        public int Damage;

        // Represents the viewable boundary of the game
        Viewport viewport;

        // Get the width of the missile
        public int Width
        {
            get { return blast.Width; }
        }

        // Get the height of the missile
        public int Height
        {
            get { return blast.Height; }
        }

        // Determines how fast the missile moves
        float blastMoveSpeed;

        private Rectangle localBounds;

        int maxDist;
        int minDist;


        public void Initialize(Viewport viewport, Texture2D texture, Vector2 position, float blastMoveSpeed)
        {
            blast = texture;
            Position = position;
            this.viewport = viewport;

            Active = true;

            Damage = 10;

            this.maxDist = (int)position.X+60;
            this.minDist = (int)position.X-60;
            this.blastMoveSpeed = blastMoveSpeed;

            LoadContent();

        }

        public void LoadContent()
        { 
        
            // Calculate bounds within texture size.            
            int width = (int)(blast.Width);
            int left = (blast.Width - width);
            int height = (int)(blast.Height);
            int top = blast.Height - height;
            localBounds = new Rectangle(left, top, width, height);
        }

        public Rectangle BoundingRectangle
        {
            get
            {
                //Vector2 botMiddle = new Vector2(blast.Width / 2.0f, blast.Height);
                int left = (int)Math.Round(Position.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        public void Update()
        {
            // missiles always move to the right
            Position.X += blastMoveSpeed;

            // Deactivate
            if ((Position.X > maxDist)||(Position.X < minDist))
                Active = false;
        }
        

        public void Draw(SpriteBatch spriteBatch, SpriteEffects flip)
        {

            //draws missile    
            spriteBatch.Draw(blast, Position, null, Color.White, 0f,
            new Vector2(Width / 2, Height / 2), 1f, flip, 1f);
        }
    }
}
