#region File Description
//-----------------------------------------------------------------------------
// HeightmapCollision.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace HeightmapCollision
{
    /// <summary>
    /// Sample showing how to use get the height of a programmatically generated
    /// heightmap.
    /// </summary>

    public struct finishValues
    {
        public int xMin;
        public int xMax;
        public int yMin;
        public int yMax;
        public Vector3 initialPosition;

        public finishValues(int p, int p_2, int p_3, int p_4)
        {
            xMin = p;
            xMax = p_2;
            yMin = p_3;
            yMax = p_4;
            initialPosition = Vector3.Zero;
        }
        public finishValues(int p, int p_2, int p_3, int p_4, Vector3 initial)
        {
            xMin = p;
            xMax = p_2;
            yMin = p_3;
            yMax = p_4;
            initialPosition = initial;
        }

    }

    public enum GameState
    {
        MAINMENU, INGAME, NOCHANGE, EXIT, INGAME2P, SELECTPLAYERS, FINISH, BETWEENLEVELS, READY, HIGHSCORES, HELP
    };

    public class HeightmapCollisionGame : Microsoft.Xna.Framework.Game
    {
        #region Constants

        // This constant controls how quickly the sphere can move forward and backward
        const float MaxVelocity = 20;

        // how quickly the sphere can turn from side to side
        const float SphereTurnSpeed = .025f;

        // the radius of the sphere. We'll use this to keep the sphere above the ground,
        // and when computing how far the sphere has rolled.
        const float SphereRadius = 12.0f;

        const float FrictionConst = .06f;

        //value used for falling ball
        const float gravityConst = 2.0f;

        // Used for jumping
        const float jumpConst = .2f;

        // This vector controls how much the camera's position is offset from the
        // sphere. This value can be changed to move the camera further away from or
        // closer to the sphere.
        readonly Vector3 CameraPositionOffset = new Vector3(0, 40, 150);

        // This value controls the point the camera will aim at. This value is an offset
        // from the sphere's position.
        readonly Vector3 CameraTargetOffset = new Vector3(0, 30, 0);

        #endregion

        #region Fields
        //for drawing time to the screen
        SpriteFont font;

        //main menu buttons
        Button startButton;
        Button helpButton;
        //Button highScoresButton;
        Button exitButton;
        
        //selectplayers buttons
        Button onePlayerButton;
        Button twoPlayerButton;
        Button cancelButton;

        //BetweenLevels buttons
        Button readyButton;
        Button mainMenuButton;

        Button readyButtonP1;
        Button readyButtonP2;
        Button mainMenuButtonP1;
        Button mainMenuButtonP2;

        //Finish buttons (if necessary)
        Button mainMenuButtonFinish;
        Texture2D finishGraphic;
        Rectangle finishGraphicPosition;

        Texture2D backgroundTexture;

        bool p1IsReady = false;
        bool p2IsReady = false;
        bool p1HasFinished = false;
        bool p2HasFinished = false;
        bool onePlayerGame = false;

        TimeSpan p1TotalTime;
        TimeSpan p1LevelTime;
        TimeSpan p2TotalTime;
        TimeSpan p2LevelTime;

        TimeSpan levelStart;

        Texture2D cursor;
        Vector2 cursorPos;

        Texture2D hand;
        Vector2 handPosP1;
        Vector2 handPosP2;

        SpriteBatch spriteBatch;        

        GraphicsDeviceManager graphics;
        Viewport leftViewport;
        Viewport rightViewport;

        Model terrain;

        Matrix projectionMatrix;
        Matrix p1View;
        Matrix p2View;

        Matrix p1Projection;
        Matrix p2Projection;

        Vector3 spherePosition;
        Vector3 flagPosition;
        Vector3 p1Position;
        Vector3 p2Position;
        float sphereFacingDirection;
        float p1Facing;
        float p2Facing;
        Matrix sphereRollingMatrix = Matrix.Identity;
        Matrix p1RollingMatrix = Matrix.Identity;
        Matrix p2RollingMatrix = Matrix.Identity;

        Model sphere;
        Model flag;
        HeightMapInfo heightMapInfo;

        InputHandler input;

        GameState currentState;

        float oldHeight;
        float newHeight;

	    Vector3 currentVelocity;
        Vector3 p1Velocity;
        Vector3 p2Velocity;

        bool[] gravity;
        bool[] hasJumped;
        float[] jumpHeight;

        Vector3 movement;

        int mainNum;
        int levelNum;
        int betweenNum;
        int betweenNumP2;

        bool mouseFocus = true;
        bool up = false;
        bool down = false;
        bool upP2 = false;
        bool downP2 = false;
        bool justStarted = true;

        public int currentLevel;
        static public int numLevels = 4;

        public static finishValues[] levelValues = new finishValues[]
        {
            new finishValues(2500, 2800, 2500, 2800), //Level One
            new finishValues(-3250, -2650, 2450, 2900), // Level Two
            new finishValues(2600, 2750, 2600, 2750), //Level Three
            new finishValues(-3220,-2601,-3812,-3193, new Vector3(-3073, 0, 1847)) // Level Four
            //This is the sphere position approximated
        };

        #endregion

        #region Initialization


        public HeightmapCollisionGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = 720;
            graphics.PreferredBackBufferWidth = 1280;
            graphics.IsFullScreen = true;
            input = new InputHandler(graphics);
            Content.RootDirectory = "Content";
            gravity = new bool[2];
            gravity[0] = false;
            gravity[1] = false;
            hasJumped = new bool[2];
            hasJumped[0] = false;
            hasJumped[1] = false;
            jumpHeight = new float[2];
            currentLevel = 0;
	        p1Velocity = Vector3.Zero;
            p2Velocity = Vector3.Zero;
            mainNum = 0;
            levelNum = 0;
            betweenNum = 0;
            betweenNumP2 = 0;
        }

        bool isOnFinish(Vector3 s)
        {
            //Console.WriteLine("X: {0} Z: {1}", spherePosition.X, spherePosition.Z);
            if ((s.X <= levelValues[currentLevel].xMax) &&
                (s.X >= levelValues[currentLevel].xMin) &&
                (s.Z <= levelValues[currentLevel].yMax) &&
                (s.Z >= levelValues[currentLevel].yMin))
            {
                //currentState = GameState.MAINMENU; //For testing
                
                //currentState = GameState.FINISH
                return true;
                
            }
            return false;
        }
        
        protected override void Initialize()
        {
            // now that the GraphicsDevice has been created, we can create the projection matrix.
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45.0f), GraphicsDevice.Viewport.AspectRatio, 1f, 10000);
           

            currentState = GameState.MAINMENU;

            Viewport original = graphics.GraphicsDevice.Viewport;

            leftViewport = new Viewport(new Rectangle(original.X, original.Y,original.Width / 2, original.Height));

            rightViewport = new Viewport(new Rectangle(original.X + leftViewport.Width, original.Y, original.Width / 2, original.Height));

            p1Projection = Matrix.CreatePerspectiveFieldOfView(
               MathHelper.ToRadians(45.0f), rightViewport.AspectRatio, 1f, 10000);
            p2Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45.0f), leftViewport.AspectRatio, 1f, 10000);

            base.Initialize();
        }


        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(graphics.GraphicsDevice);

            font = Content.Load<SpriteFont>("SpriteFont1");
            
            sphere = Content.Load<Model>("sphere");
            flag = Content.Load<Model>("flag");

            cursor = Content.Load<Texture2D>("Cursor");
            hand = Content.Load<Texture2D>("Cursor");
            finishGraphic = Content.Load<Texture2D>("youwin");

            backgroundTexture = Content.Load<Texture2D>("background");

            //mainmenubuttons
            Rectangle position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("PlayButton").Width / 2, 75,
                Content.Load<Texture2D>("PlayButton").Width, Content.Load<Texture2D>("PlayButtonHi").Height);
            startButton = new Button(position, Content.Load<Texture2D>("PlayButton"), Content.Load<Texture2D>("PlayButtonHi"), GameState.SELECTPLAYERS, 0);

            position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("ExitButton").Width / 2, 475,
                Content.Load<Texture2D>("ExitButton").Width, Content.Load<Texture2D>("ExitButtonHi").Height);

            exitButton = new Button(position, Content.Load<Texture2D>("ExitButton"), Content.Load<Texture2D>("ExitButtonHi"), GameState.EXIT,2);

            position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("HelpButton").Width / 2, 275, 
                Content.Load<Texture2D>("HelpButton").Width, Content.Load<Texture2D>("HelpButton").Height);
            helpButton = new Button(position, Content.Load<Texture2D>("HelpButton"), Content.Load<Texture2D>("HelpButtonHi"), GameState.HELP,1);

            //position = new Rectangle(0, 200, 200, 200);
            //highScoresButton = new Button(position, Content.Load<Texture2D>("NextLevel"), Content.Load<Texture2D>("NextLevelHi"), GameState.HIGHSCORES);

            
            //selectPlayers buttons
            position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("OnePlayer").Width / 2, 75, 400, 200);
            onePlayerButton = new Button(position, Content.Load<Texture2D>("OnePlayer"), Content.Load<Texture2D>("OnePlayerHi"), GameState.INGAME,0);
            position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("OnePlayer").Width / 2, 275, 400, 200);
            twoPlayerButton = new Button(position, Content.Load<Texture2D>("TwoPlayer"), Content.Load<Texture2D>("TwoPlayerHi"), GameState.INGAME2P,1);
            position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("OnePlayer").Width / 2, 475, 400, 200);
            cancelButton = new Button(position, Content.Load<Texture2D>("MainMenu"), Content.Load<Texture2D>("MainMenuHi"), GameState.MAINMENU,2);

            //betweenlevels buttons
            position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("NextLevel").Width / 2, 50, Content.Load<Texture2D>("NextLevel").Width, Content.Load<Texture2D>("NextLevel").Height);
            readyButton = new Button(position, Content.Load<Texture2D>("NextLevel"), Content.Load<Texture2D>("NextLevelHi"), GameState.INGAME,0);
            position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("MainMenu").Width / 2, 400, Content.Load<Texture2D>("MainMenu").Width, Content.Load<Texture2D>("MainMenu").Height);
            mainMenuButton = new Button(position, Content.Load<Texture2D>("MainMenu"), Content.Load<Texture2D>("MainMenuHi"), GameState.MAINMENU,1);

            //also need buttons for the two player version
            position = new Rectangle(leftViewport.Width / 2 - Content.Load<Texture2D>("NextLevel").Width / 2 , 50, Content.Load<Texture2D>("NextLevel").Width, Content.Load<Texture2D>("NextLevel").Height);
            readyButtonP2 = new Button(position, Content.Load<Texture2D>("NextLevel"), Content.Load<Texture2D>("NextLevelHi"), GameState.INGAME,0);
            position = new Rectangle(leftViewport.Width / 2 - Content.Load<Texture2D>("MainMenu").Width / 2, 400, Content.Load<Texture2D>("MainMenu").Width, Content.Load<Texture2D>("MainMenu").Height);
            mainMenuButtonP2 = new Button(position, Content.Load<Texture2D>("MainMenu"), Content.Load<Texture2D>("MainMenuHi"), GameState.MAINMENU,1);
            position = new Rectangle(leftViewport.Width / 2 - Content.Load<Texture2D>("NextLevel").Width / 2 + leftViewport.Width, 50, Content.Load<Texture2D>("NextLevel").Width, Content.Load<Texture2D>("NextLevel").Height);
            readyButtonP1 = new Button(position, Content.Load<Texture2D>("NextLevel"), Content.Load<Texture2D>("NextLevelHi"), GameState.INGAME,0);
            position = new Rectangle(leftViewport.Width / 2 - Content.Load<Texture2D>("MainMenu").Width / 2 + leftViewport.Width, 400, Content.Load<Texture2D>("MainMenu").Width, Content.Load<Texture2D>("MainMenu").Height);
            mainMenuButtonP1 = new Button(position, Content.Load<Texture2D>("MainMenu"), Content.Load<Texture2D>("MainMenuHi"), GameState.MAINMENU,1);
            /*
            position = new Rectangle(0, 0, Content.Load<Texture2D>("PlayButton").Width, Content.Load<Texture2D>("PlayButtonHi").Height);
            readyButtonP2 = new Button(position, Content.Load<Texture2D>("PlayButton"), Content.Load<Texture2D>("PlayButtonHi"), GameState.INGAME,0);
            position = new Rectangle(0, 400, 200, 200);
            mainMenuButtonP2 = new Button(position, Content.Load<Texture2D>("ExitButton"), Content.Load<Texture2D>("ExitButtonHi"), GameState.MAINMENU,1);
            
            position = new Rectangle(0 + leftViewport.Width, 0, Content.Load<Texture2D>("PlayButton").Width, Content.Load<Texture2D>("PlayButtonHi").Height);
            readyButtonP1 = new Button(position, Content.Load<Texture2D>("PlayButton"), Content.Load<Texture2D>("PlayButtonHi"), GameState.INGAME,0);
            position = new Rectangle(0 + leftViewport.Width, 400, 200, 200);
            mainMenuButtonP1 = new Button(position, Content.Load<Texture2D>("ExitButton"), Content.Load<Texture2D>("ExitButtonHi"), GameState.MAINMENU,1);
            */

            //finish buttons
            position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("MainMenu").Width / 2, 475, Content.Load<Texture2D>("MainMenu").Width, Content.Load<Texture2D>("MainMenu").Height);
            mainMenuButtonFinish = new Button(position, Content.Load<Texture2D>("MainMenu"), Content.Load<Texture2D>("MainMenuHi"), GameState.MAINMENU, 0);
            finishGraphicPosition = new Rectangle(GraphicsDevice.Viewport.Width / 2 - finishGraphic.Width / 2, 0, finishGraphic.Width, finishGraphic.Height);


            loadLevel();
        }

        private void loadLevel()
        {
            string levelName = "level_";
            levelName += (String) Convert.ToString(currentLevel + 1);

            p1Position = levelValues[currentLevel].initialPosition;
            p2Position = levelValues[currentLevel].initialPosition;
            p1Facing = 0;
            p2Facing = 0;
	        p1Velocity = Vector3.Zero;
            p2Velocity = Vector3.Zero;

            flagPosition = Vector3.Zero;
            flagPosition.X = levelValues[currentLevel].xMax;
            flagPosition.Z = levelValues[currentLevel].yMax;
            terrain = Content.Load<Model>(levelName);

            // The terrain processor attached a HeightMapInfo to the terrain model's
            // Tag. We'll save that to a member variable now, and use it to
            // calculate the terrain's heights later.
            heightMapInfo = terrain.Tag as HeightMapInfo;

            if (heightMapInfo == null)
            {
                string message = "The terrain model did not have a HeightMapInfo " +
                    "object attached. Are you sure you are using the " +
                    "TerrainProcessor?";
                throw new InvalidOperationException(message);
            }
        }


        #endregion

        #region Update and Draw


        /// <summary>
        /// Allows the game to run logic.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            input.update(gameTime);

            if (input.getMouse().LeftButton == ButtonState.Pressed)
            {
                mouseFocus = true;
            }

            bool newup = false;
            bool newdown = false;
            bool newupP2 = false;
            bool newdownP2 = false;

            if ((currentState != GameState.INGAME) && (currentState != GameState.INGAME2P))
            {
                if (!up && (input.isNewKeyPress(Keys.Up) ||
                    (GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y > .1)))
                {
                    newup = true;
                    up = true;
                    mouseFocus = false;
                }
                else if (up && !(GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y > .1))
                {
                    up = false;
                }

                if (!down && (input.isNewKeyPress(Keys.Down) ||
                    (GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y < -.1)))
                {
                    down = true;
                    newdown = true;
                    mouseFocus = false;
                }
                else if (down && !(GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y < -.1))
                {
                    down = false;
                }

                if (!upP2 && (input.isNewKeyPress(Keys.W) ||
                (GamePad.GetState(PlayerIndex.Two).ThumbSticks.Left.Y > .1)))
                {
                    upP2 = true;
                    newupP2 = true;
                    mouseFocus = false;
                }
                else if (upP2 && !(GamePad.GetState(PlayerIndex.Two).ThumbSticks.Left.Y > .1))
                {
                    upP2 = false;
                }

                if (!downP2 && (input.isNewKeyPress(Keys.S) ||
                    (GamePad.GetState(PlayerIndex.Two).ThumbSticks.Left.Y < -.1)))
                {
                    downP2 = true;
                    newdownP2 = true;
                    mouseFocus = false;
                }
                else if (downP2 && !(GamePad.GetState(PlayerIndex.Two).ThumbSticks.Left.Y < -.1))
                {
                    downP2 = false;
                }
            }
            else
            {
                while (GamePad.GetState(PlayerIndex.One).Buttons.Start == ButtonState.Pressed)
                {
                    currentState = GameState.MAINMENU;
                }
                while (GamePad.GetState(PlayerIndex.Two).Buttons.Start == ButtonState.Pressed)
                {
                    currentState = GameState.MAINMENU;
                }
            }

            switch (currentState)
            {
                case GameState.INGAME:

                    if (!p1HasFinished)
                    {
                        HandleInput(PlayerIndex.One);
                        UpdateCamera(PlayerIndex.One);
                        p1LevelTime = gameTime.TotalGameTime - levelStart;
                    }
                   // Console.WriteLine("Ball position is: {0}", p1Position.ToString());
                    if (isOnFinish(p1Position) && !p1HasFinished)
                    {
                        p1TotalTime += p1LevelTime;
                        p1HasFinished = true;
                    }
                    if (p1HasFinished)
                    {
                        p1HasFinished = false;
                        if (currentLevel < (numLevels - 1))
                        {
                            currentState = GameState.BETWEENLEVELS;
                            ++currentLevel;
                            loadLevel();
                        }
                        else
                        {
                            currentState = GameState.FINISH;
                        }
                    }
                    break;
                case GameState.INGAME2P:

                    if (!p1HasFinished)
                    {
                        HandleInput(PlayerIndex.One);
                        UpdateCamera(PlayerIndex.One);
                        p1LevelTime = gameTime.TotalGameTime - levelStart;
                    }

                    if (!p2HasFinished)
                    {
                        HandleInput(PlayerIndex.Two);
                        UpdateCamera(PlayerIndex.Two);
                        p2LevelTime = gameTime.TotalGameTime - levelStart;
                    }

                    if (isOnFinish(p1Position) && !p1HasFinished)
                    {
                        p1TotalTime += p1LevelTime;
                        p1HasFinished = true;
                    }
                    if (isOnFinish(p2Position) && !p2HasFinished)
                    {
                        p2TotalTime += p2LevelTime;
                        p2HasFinished = true;
                    }
                    if (p1HasFinished && p2HasFinished)
                    {
                        p1HasFinished = false;
                        p2HasFinished = false;
                        if (currentLevel < (numLevels - 1))
                        {
                            currentState = GameState.BETWEENLEVELS;
                            ++currentLevel;
                            loadLevel();
                        }
                        else
                        {
                            currentState = GameState.FINISH;
                        }
                    }
                    break;
                case GameState.MAINMENU:
                    if (newup || newupP2)
                    {
                        if (!justStarted)
                        {
                            if (mainNum == 0)
                            {
                                mainNum = 2;
                            }
                            else
                            {
                                mainNum--;
                            }
                        }
                        else
                        {
                            justStarted = false;
                        }
                    }
                    if (newdown || newdownP2)
                    {
                        if (!justStarted)
                        {
                            if (mainNum == 2)
                            {
                                mainNum = 0;
                            }
                            else
                            {
                                mainNum++;
                            }
                        }
                        else
                        {
                            justStarted = false;
                        }
                    }
                    UpdateMainMenu(gameTime);
                    break;
                case GameState.SELECTPLAYERS:
                    if (newup || newupP2)
                    {
                        if (levelNum == 0)
                        {
                            levelNum = 2;
                        }
                        else
                        {
                            levelNum--;
                        }
                    }
                    if (newdown || newdownP2)
                    {
                        if (levelNum == 2)
                        {
                            levelNum = 0;
                        }
                        else
                        {
                            levelNum++;
                        }
                    }
                    UpdatePlayerSelect(gameTime);
                    break;
                case GameState.BETWEENLEVELS:
                    if (newup || newdown)
                    {
                        if (betweenNum == 1)
                        {
                            betweenNum = 0;
                        }
                        else
                        {
                            betweenNum = 1;
                        }
                    }
                    if (newupP2 || newdownP2)
                    {
                        if (betweenNumP2 == 1)
                        {
                            betweenNumP2 = 0;
                        }
                        else
                        {
                            betweenNumP2 = 1;
                        }
                    }
                    UpdateBetweenLevels(gameTime);
                    break;
                case GameState.EXIT:
                    this.Exit();
                    break;
                case GameState.FINISH:
                    UpdateFinishGame(gameTime);
                    
                    break;
                case GameState.HELP:
                    UpdateFinishGame(gameTime);
                    break;
                default:
                    //error message here
                    break;
            }

            base.Update(gameTime);
        }

        private void UpdateBetweenLevels(GameTime gameTime)
        {
            if (onePlayerGame)
            {
                handPosP1 = input.getHandPosition(PlayerIndex.One, graphics.GraphicsDevice.Viewport);
                GameState buttonState;
                buttonState = mainMenuButton.Update(gameTime, input.getMouse(), handPosP1, betweenNum, PlayerIndex.One, mouseFocus);
                if (buttonState != GameState.NOCHANGE)
                {
                    p1TotalTime = TimeSpan.Zero;
                    p2TotalTime = TimeSpan.Zero;
                    currentState = buttonState;
                    currentLevel = 0;
                    loadLevel();
                    return;
                }

                buttonState = readyButton.Update(gameTime, input.getMouse(), handPosP1, betweenNum, PlayerIndex.One, mouseFocus);

                if (buttonState != GameState.NOCHANGE)
                {
                    levelStart = gameTime.TotalGameTime;
                    currentState = buttonState;
                }
                cursorPos = new Vector2(input.getMouse().X, input.getMouse().Y);
            }
            else
            {
                GameState buttonState;
                handPosP1 = input.getHandPosition(PlayerIndex.One, rightViewport);
                handPosP2 = input.getHandPosition(PlayerIndex.Two, leftViewport);
                buttonState = mainMenuButtonP1.Update(gameTime, input.getMouse(), handPosP1, betweenNum, PlayerIndex.One, mouseFocus);
                if (buttonState != GameState.NOCHANGE)
                {
                    p1TotalTime = TimeSpan.Zero;
                    p2TotalTime = TimeSpan.Zero;
                    currentState = buttonState;
                    currentLevel = 0;
                    loadLevel();
                    return;
                }

                buttonState = readyButtonP1.Update(gameTime, input.getMouse(), handPosP1, betweenNum, PlayerIndex.One, mouseFocus);

                if (buttonState != GameState.NOCHANGE)
                {
                    p1IsReady = true;
                }

                buttonState = mainMenuButtonP2.Update(gameTime, input.getMouse(), handPosP2, betweenNumP2, PlayerIndex.Two, mouseFocus);
                if (buttonState != GameState.NOCHANGE)
                {
                    p1TotalTime = TimeSpan.Zero;
                    p2TotalTime = TimeSpan.Zero;
                    currentState = buttonState;
                    currentLevel = 0;
                    loadLevel();
                    return;
                }

                buttonState = readyButtonP2.Update(gameTime, input.getMouse(), handPosP2, betweenNumP2, PlayerIndex.Two, mouseFocus);

                if (buttonState != GameState.NOCHANGE)
                {
                    p2IsReady = true;
                }
                cursorPos = new Vector2(input.getMouse().X, input.getMouse().Y);

                if (p1IsReady && p2IsReady)
                {
                    levelStart = gameTime.TotalGameTime;
                    currentState = GameState.INGAME2P;
                    p1IsReady = false;
                    p2IsReady = false;
                }
            }
        }

        private void UpdateFinishGame(GameTime gameTime)
        {
            GameState buttonState;
            handPosP1 = input.getHandPosition(PlayerIndex.One, graphics.GraphicsDevice.Viewport);
            cursorPos = new Vector2(input.getMouse().X, input.getMouse().Y);
            int finishNum = 0; //brian fix this for xbox selection
            buttonState = mainMenuButtonFinish.Update(gameTime, input.getMouse(), handPosP1, finishNum, PlayerIndex.One, mouseFocus);
            if (buttonState != GameState.NOCHANGE)
            {
                currentState = buttonState;
            }
        }

        private void UpdatePlayerSelect(GameTime gameTime)
        {
            GameState buttonState;
            handPosP1 = input.getHandPosition(PlayerIndex.One, graphics.GraphicsDevice.Viewport);

            buttonState = onePlayerButton.Update(gameTime, input.getMouse(), handPosP1, levelNum, PlayerIndex.One, mouseFocus);

            if (buttonState != GameState.NOCHANGE)
            {
                currentState = buttonState;
                if (currentState == GameState.INGAME)
                {
                    levelStart = gameTime.TotalGameTime;
                    onePlayerGame = true;
                }
                return;
            }

            buttonState = twoPlayerButton.Update(gameTime, input.getMouse(), handPosP1, levelNum, PlayerIndex.Two, mouseFocus);

            if (buttonState != GameState.NOCHANGE)
            {
                currentState = buttonState;
                if (currentState == GameState.INGAME2P)
                {
                    levelStart = gameTime.TotalGameTime;
                    onePlayerGame = false;
                }
                return;
            }


            buttonState = cancelButton.Update(gameTime, input.getMouse(), handPosP1, levelNum, PlayerIndex.One, mouseFocus);

            if (buttonState != GameState.NOCHANGE)
            {
                currentState = buttonState;
                return;
            }

            cursorPos = new Vector2(input.getMouse().X, input.getMouse().Y);
        }

        private void UpdateMainMenu(GameTime gameTime)
        {
            GameState buttonState;

            handPosP1 = input.getHandPosition(PlayerIndex.One, graphics.GraphicsDevice.Viewport);

            buttonState = startButton.Update(gameTime, input.getMouse(), handPosP1, mainNum, PlayerIndex.One, mouseFocus);

            if (buttonState != GameState.NOCHANGE)
            {
                currentState = buttonState;
                return;
            }

            buttonState = exitButton.Update(gameTime, input.getMouse(), handPosP1, mainNum, PlayerIndex.One, mouseFocus);

            if (buttonState != GameState.NOCHANGE)
            {
                currentState = buttonState;
                return;
            }

            buttonState = helpButton.Update(gameTime, input.getMouse(), handPosP1, mainNum, PlayerIndex.One, mouseFocus);
            if (buttonState != GameState.NOCHANGE)
            {
#if XBOX360
                currentState = buttonState;
#else
                System.Diagnostics.Process.Start("http://www-personal.umich.edu/~mjbader/instructions.html");
#endif
                return;

            }
            //buttonState = highScoresButton.Update(gameTime, input.getMouse(), handPosP1, mainNum);

            //if (buttonState != GameState.NOCHANGE)
            //{
            //    currentState = buttonState;
            //    return;
            //}

            cursorPos = new Vector2(input.getMouse().X, input.getMouse().Y);
        }

        /// <summary>
        /// this function will calculate the camera's position and the position of 
        /// its target. From those, we'll update the viewMatrix.
        /// </summary>
        private void UpdateCamera(PlayerIndex player)
        {
            // The camera's position depends on the sphere's facing direction: when the
            // sphere turns, the camera needs to stay behind it. So, we'll calculate a
            // rotation matrix using the sphere's facing direction, and use it to
            // transform the two offset values that control the camera.
            Matrix cameraFacingMatrix = Matrix.CreateRotationY(sphereFacingDirection);
            Vector3 positionOffset = Vector3.Transform(CameraPositionOffset,
                cameraFacingMatrix);
            Vector3 targetOffset = Vector3.Transform(CameraTargetOffset,
                cameraFacingMatrix);

            // once we've transformed the camera's position offset vector, it's easy to
            // figure out where we think the camera should be.
            Vector3 cameraPosition = spherePosition + positionOffset;

            // We don't want the camera to go beneath the heightmap, so if the camera is
            // over the terrain, we'll move it up.
            if (heightMapInfo.IsOnHeightmap(cameraPosition))
            {
                // we don't want the camera to go beneath the terrain's height +
                // a small offset.
                float minimumHeight;
                Vector3 dummyNormal;
                heightMapInfo.GetHeightAndNormal(cameraPosition, out minimumHeight, out dummyNormal);

                if (cameraPosition.Y < minimumHeight)
                {
                    cameraPosition.Y = minimumHeight;
                }
            }

            // next, we need to calculate the point that the camera is aiming it. That's
            // simple enough - the camera is aiming at the sphere, and has to take the 
            // targetOffset into account.
            Vector3 cameraTarget = spherePosition + targetOffset;


            // with those values, we'll calculate the viewMatrix.
            if (player == PlayerIndex.One)
                p1View = Matrix.CreateLookAt(cameraPosition,
                                              cameraTarget,
                                              Vector3.Up);
            else if (player == PlayerIndex.Two)
                p2View = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);
        }

        public String formatString(String s)
        {
     
            String result = "";
            for (int i = 0; i < s.Length; i++)
            {
                if ((i > 2) && (i < s.Length - 5))
                    result += s[i];
            }
            return result;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice device = graphics.GraphicsDevice;
            
            device.Clear(Color.Black);

           // if (currentState != GameState.INGAME && currentState != GameState.INGAME2P)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(backgroundTexture, GraphicsDevice.Viewport.Bounds, Color.White);
                spriteBatch.End();
            }

            switch (currentState)
            {
                case GameState.INGAME:
                    
                    graphics.GraphicsDevice.BlendState = BlendState.Opaque;
                    graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

                    DrawModel(terrain, Matrix.Identity, p1View, projectionMatrix);
                    DrawModel(sphere, p1RollingMatrix * 
                        Matrix.CreateTranslation(p1Position), p1View, projectionMatrix);
                    //DrawModel(flag, Matrix.CreateTranslation(flagPosition), p1View, projectionMatrix);

                    spriteBatch.Begin();
                    spriteBatch.DrawString(font, formatString(p1LevelTime.ToString()), new Vector2(0, 0), Color.White);
                    spriteBatch.End();

                    break;
                case GameState.INGAME2P:

                    graphics.GraphicsDevice.BlendState = BlendState.Opaque;
                    graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

                    Viewport original = graphics.GraphicsDevice.Viewport;
                    //player one
                    if (!p1HasFinished)
                    {
                        graphics.GraphicsDevice.Viewport = rightViewport;
                        DrawModel(terrain, Matrix.Identity, p1View, p1Projection);
                        DrawModel(sphere, p1RollingMatrix *
                            Matrix.CreateTranslation(p1Position), p1View, p1Projection);
                        DrawModel(sphere, p2RollingMatrix *
                            Matrix.CreateTranslation(p2Position), p1View, p1Projection);
                    }
                    //player two
                    if (!p2HasFinished)
                    {
                        graphics.GraphicsDevice.Viewport = leftViewport;
                        DrawModel(terrain, Matrix.Identity, p2View, p2Projection);
                        DrawModel(sphere, p2RollingMatrix *
                            Matrix.CreateTranslation(p2Position), p2View, p2Projection);
                        DrawModel(sphere, p1RollingMatrix *
                            Matrix.CreateTranslation(p1Position), p2View, p2Projection);
                    }

                    graphics.GraphicsDevice.Viewport = original;

                    spriteBatch.Begin();
                    if (!p1HasFinished)
                        spriteBatch.DrawString(font, formatString(p1LevelTime.ToString()), new Vector2(rightViewport.X, rightViewport.Y), Color.White);
                    else
                    {
                        spriteBatch.DrawString(font, "Level Time: " + formatString(p1LevelTime.ToString()), new Vector2(rightViewport.X + 175, rightViewport.Y + rightViewport.Height / 2), Color.Black);
                        spriteBatch.DrawString(font, "Congratulations!", new Vector2(rightViewport.X + 200, rightViewport.Y + rightViewport.Height* 3 / 8), Color.Black);
                    }

                    if (!p2HasFinished)
                        spriteBatch.DrawString(font, formatString(p2LevelTime.ToString()), new Vector2(leftViewport.X, leftViewport.Y), Color.White);
                    else
                    {
                        spriteBatch.DrawString(font, "Level Time: " + formatString(p2LevelTime.ToString()), new Vector2(leftViewport.X + 175, leftViewport.Y + leftViewport.Height / 2), Color.Black);
                        spriteBatch.DrawString(font, "Congratulations!", new Vector2(leftViewport.X + 200, leftViewport.Y + leftViewport.Height * 3 / 8), Color.Black);
                    }
                    spriteBatch.End();
                    
                    // If there was any alpha blended translucent geometry in
                    // the scene, that would be drawn here.
                    break;
                case GameState.MAINMENU:
                    spriteBatch.Begin();
                    startButton.Draw(spriteBatch);
                    exitButton.Draw(spriteBatch);
                    helpButton.Draw(spriteBatch);
                    //highScoresButton.Draw(spriteBatch);
#if !XBOX360
                    spriteBatch.Draw(cursor, cursorPos, Color.White);
                    spriteBatch.Draw(hand, handPosP1, Color.White);
#endif
                    spriteBatch.End();
                    break;
                case GameState.BETWEENLEVELS:
                    DrawBetweenLevels();
                    break;
                case GameState.SELECTPLAYERS:
                    spriteBatch.Begin();
                    onePlayerButton.Draw(spriteBatch);
                    twoPlayerButton.Draw(spriteBatch);
                    cancelButton.Draw(spriteBatch);
#if !XBOX360
                    spriteBatch.Draw(cursor, cursorPos, Color.White);
                    spriteBatch.Draw(hand, handPosP1, Color.White);
#endif
                    spriteBatch.End();
                    break;
                case GameState.FINISH:
                    //Need A notice that the player won
                    spriteBatch.Begin();
                    mainMenuButtonFinish.Draw(spriteBatch);
                    spriteBatch.Draw(finishGraphic, finishGraphicPosition, Color.White);

                    if (onePlayerGame)
                    {
                        spriteBatch.DrawString(font, "Player 1 Total Time: " + formatString(p1TotalTime.ToString()), new Vector2(425, 350), Color.Black);
                        spriteBatch.DrawString(font, "Player 1 Level Time: " + formatString(p1LevelTime.ToString()), new Vector2(425, 400), Color.Black);
                    }
                    else
                    {
                        spriteBatch.DrawString(font, "Player 1 Total Time: " + formatString(p1TotalTime.ToString()), new Vector2(rightViewport.X + 125, 350), Color.Black);
                        spriteBatch.DrawString(font, "Player 1 Level Time: " + formatString(p1LevelTime.ToString()), new Vector2(rightViewport.X + 125, 400), Color.Black);
                        spriteBatch.DrawString(font, "Player 2 Total Time: " + formatString(p2TotalTime.ToString()), new Vector2(125, 350), Color.Black);
                        spriteBatch.DrawString(font, "Player 2 Level Time: " + formatString(p2LevelTime.ToString()), new Vector2(125, 400), Color.Black);
                    }
#if !XBOX360
                    spriteBatch.Draw(cursor, handPosP1, Color.White);
                    spriteBatch.Draw(cursor, cursorPos, Color.White);                 
#endif
                    spriteBatch.End();
                    break;
                case GameState.HELP:
                    spriteBatch.Begin();
                    mainMenuButtonFinish.Draw(spriteBatch);
                    spriteBatch.DrawString(font, "Controls", new Vector2(475, 50), Color.Black);
                    spriteBatch.DrawString(font, "Move: Left analog stick", new Vector2(475, 150), Color.Black);
                    spriteBatch.DrawString(font, "Turn: Right analog stick", new Vector2(475, 200), Color.Black);
                    spriteBatch.DrawString(font, "Jump: A button", new Vector2(475, 250), Color.Black);
                    spriteBatch.DrawString(font, "Pause: Start button", new Vector2(475, 350), Color.Black);
                    spriteBatch.DrawString(font, "Reset: Y button", new Vector2(475, 300), Color.Black);
                    spriteBatch.End();
                    break;
                default:
                    //error message
                    break;
            }


            base.Draw(gameTime);
        }

        void DrawBetweenLevels()
        {
            if (onePlayerGame)
            {
                spriteBatch.Begin();
                readyButton.Draw(spriteBatch);
                mainMenuButton.Draw(spriteBatch);

                spriteBatch.DrawString(font, "Player 1 Total Time: " + formatString(p1TotalTime.ToString()), new Vector2(425, 300), Color.Black);
                spriteBatch.DrawString(font, "Player 1 Level Time: " + formatString(p1LevelTime.ToString()), new Vector2(425, 350), Color.Black);
#if !XBOX360
                spriteBatch.Draw(cursor, cursorPos, Color.White);
                spriteBatch.Draw(hand, handPosP1, Color.White);
#endif
                spriteBatch.End();
            }
            else
            {
                spriteBatch.Begin();
                readyButtonP1.Draw(spriteBatch);
                mainMenuButtonP1.Draw(spriteBatch);
                readyButtonP2.Draw(spriteBatch);
                mainMenuButtonP2.Draw(spriteBatch);

                spriteBatch.DrawString(font, "Player 1 Total Time: " + formatString(p1TotalTime.ToString()), new Vector2(rightViewport.X + 125, 300), Color.Black);
                spriteBatch.DrawString(font, "Player 1 Level Time: " + formatString(p1LevelTime.ToString()), new Vector2(rightViewport.X + 125, 350), Color.Black);
                spriteBatch.DrawString(font, "Player 2 Total Time: " + formatString(p2TotalTime.ToString()), new Vector2(125, 300), Color.Black);
                spriteBatch.DrawString(font, "Player 2 Level Time: " + formatString(p2LevelTime.ToString()), new Vector2(125, 350), Color.Black);
#if !XBOX360
                spriteBatch.Draw(cursor, cursorPos, Color.White);
                spriteBatch.Draw(hand, handPosP1, Color.White);
                spriteBatch.Draw(hand, handPosP2, Color.White);
#endif
                spriteBatch.End();
            }

        }


        /// <summary>
        /// Helper for drawing the terrain model.
        /// </summary>
        void DrawModel(Model model, Matrix worldMatrix, Matrix view, Matrix projection)
        {
            Matrix[] boneTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = boneTransforms[mesh.ParentBone.Index] * worldMatrix;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;

                    // Set the fog to match the black background color
                    effect.FogEnabled = true;
                    effect.FogColor = Vector3.Zero;
                    effect.FogStart = 1000;
                    effect.FogEnd = 3200;

                }

                mesh.Draw();
            }
        }


        #endregion

        #region Handle Input

        /// <summary>
        /// Handles input for quitting the game.
        /// </summary>
        private void HandleInput(PlayerIndex player)
        {
            if (player == PlayerIndex.One)
            {
                spherePosition = p1Position;
                sphereFacingDirection = p1Facing;
                sphereRollingMatrix = p1RollingMatrix;
                currentVelocity = p1Velocity;
            }
            else if (player == PlayerIndex.Two)
            {
                spherePosition = p2Position;
                sphereFacingDirection = p2Facing;
                sphereRollingMatrix = p2RollingMatrix;
                currentVelocity = p2Velocity;
            }

            // Now move the sphere. First, we want to check to see if the sphere should
            // turn. turnAmount will be an accumulation of all the different possible
            // inputs.
            GamePadState gp = GamePad.GetState(player);
            float cameraMove = -gp.ThumbSticks.Right.X;
            if (cameraMove == 0)
            {
                sphereFacingDirection += input.turnAmount(player) * SphereTurnSpeed;
            }
            else
            {
                sphereFacingDirection += cameraMove * SphereTurnSpeed;
            }

            // Next, we want to move the sphere forward or back. to do this, 
            // we'll create a Vector3 and modify use the user's input to modify the Z
            // component, which corresponds to the forward direction.
            
            movement = Vector3.Zero;

            movement.Z = input.moveAmount(player);
            //movement.X = input.strafeAmount(player);

            
            Vector2 move = gp.ThumbSticks.Left;

            //move.Normalize();
            if (move != Vector2.Zero)
            {
                movement.X = move.X;
                movement.Z = -move.Y;
            }

            Vector3 newSpherePosition = spherePosition + currentVelocity;

            Vector3 oldNormal;
            Vector3 newNormal;
            if (heightMapInfo.IsOnHeightmap(newSpherePosition))
            {
                heightMapInfo.GetHeightAndNormal(newSpherePosition, out newHeight, out newNormal);
            }

            int playerIndex;
            if (player == PlayerIndex.One)
            {
                playerIndex = 0;
            }
            else
            {
                playerIndex = 1;
            }

            if (input.jumped(player) && !hasJumped[playerIndex] && !gravity[playerIndex])
            {
                hasJumped[playerIndex] = true;
                jumpHeight[playerIndex] = spherePosition.Y + SphereRadius * 2;
            }




            if (gravity[playerIndex])
            {
                if ((spherePosition.Y - movement.Y) > (newHeight + SphereRadius))
                {
                    movement.Y -= gravityConst;
                }
                else
                {
                    newSpherePosition.Y = newHeight + SphereRadius;
                    gravity[playerIndex] = false;
                    currentVelocity.Y = 0;
                    movement.Y = 0;
                }
            }

            if (hasJumped[playerIndex])
            {
                if (spherePosition.Y < jumpHeight[playerIndex])
                {
                    movement.Y = jumpConst;
                }
                else
                {
                    gravity[playerIndex] = true;
                    hasJumped[playerIndex] = false;
                    movement.Y = 0;
                }
            }

            // next, we'll create a rotation matrix from the sphereFacingDirection, and
            // use it to transform the vector. If we didn't do this, pressing "up" would
            // always move the ball along +Z. By transforming it, we can move in the
            // direction the sphere is "facing."
            Matrix sphereFacingMatrix = Matrix.CreateRotationY(sphereFacingDirection);

            currentVelocity += Vector3.Transform(movement, sphereFacingMatrix);
            
            if (Math.Abs(currentVelocity.X) < 0.05f)
            {
                currentVelocity.X = 0;
            }
            else
            {
                currentVelocity.X -= currentVelocity.X * FrictionConst;
            }
            if (Math.Abs(currentVelocity.Z) < 0.05f)
            {
                currentVelocity.Z = 0;
            }
            else
            {
                currentVelocity.Z -= currentVelocity.Z * FrictionConst;
            }

            if (currentVelocity.X < -MaxVelocity)
                currentVelocity.X = -MaxVelocity;
            if (currentVelocity.X > MaxVelocity)
                currentVelocity.X = MaxVelocity;
            if (currentVelocity.Z < -MaxVelocity)
                currentVelocity.Z = -MaxVelocity;
            if (currentVelocity.Z > MaxVelocity)
                currentVelocity.Z = MaxVelocity;

            if (heightMapInfo.IsOnHeightmap(newSpherePosition))
            {
                // finally, we need to see how high the terrain is at the sphere's new
                // position. GetHeight will give us that information, which is offset by
                // the size of the sphere. If we didn't offset by the size of the
                // sphere, it would be drawn halfway through the world, which looks 
                // a little odd.
                

                heightMapInfo.GetHeightAndNormal(spherePosition, out oldHeight, out oldNormal);
                heightMapInfo.GetHeightAndNormal(newSpherePosition, out newHeight, out newNormal);
                
                if (!gravity[playerIndex] && !hasJumped[playerIndex])
                {
                    newSpherePosition.Y = oldHeight + SphereRadius;
                }
                Vector3 checkSpherePosition = newSpherePosition;
                Vector3 result;
                if (oldHeight < newHeight)
                {
                    if ((Math.Acos(Vector3.Dot(newNormal, Vector3.Up))) < .4)
                    {
                        //Console.WriteLine("I am here: {0}", Math.Acos(Vector3.Dot(newNormal, Vector3.Up)));
                        if (hasJumped[playerIndex])
                        {
                            newSpherePosition.Y = newHeight + SphereRadius + 3;
                        }
                    }
                    else
                    {
                        //Console.WriteLine("I am not going up a wall ever!");
                        if ( (!gravity[playerIndex] && !hasJumped[playerIndex])
                            || (newSpherePosition.Y < newHeight + 10))
                        {
                            Vector3 tempPosition = newSpherePosition;
                            tempPosition.X -= currentVelocity.X;
                            float tempHeight;
                            heightMapInfo.GetHeightAndNormal(tempPosition, out tempHeight, out newNormal);
                            if (tempHeight == newHeight)
                            {
                                currentVelocity = Vector3.Reflect(currentVelocity, Vector3.UnitZ);
                            }
                            else
                            {
                                currentVelocity = Vector3.Reflect(currentVelocity, Vector3.UnitX);
                            }
                            newSpherePosition = spherePosition;
                        }
                    }
                }

                else //newHeight <= oldHeight
                {
                    if ((newHeight < oldHeight + SphereRadius) && (newHeight != oldHeight))
                    {
                        if ((Math.Acos(Vector3.Dot(newNormal, Vector3.Up))) < .4)
                        {
                            //Console.WriteLine("I am here: {0}", Math.Acos(Vector3.Dot(newNormal, Vector3.Up)));
                        }
                        else
                        {
                            // Console.WriteLine("I am here: {0}", Math.Acos(Vector3.Dot(newNormal, Vector3.Up)));
                            if (!hasJumped[playerIndex])
                            {
                                gravity[playerIndex] = true;
                            }
                        }
                    }
                    result.Y = spherePosition.Y + SphereRadius;
                    result.X = newSpherePosition.X - spherePosition.X;
                    result.Z = newSpherePosition.Z - spherePosition.Z;

                    checkSpherePosition.Y = spherePosition.Y;
                    checkSpherePosition.X += SphereRadius * Math.Sign(result.X);
                    checkSpherePosition.Z += SphereRadius * Math.Sign(result.Z);
                    bool radiusCheck = false;
                    if (heightMapInfo.IsOnHeightmap(checkSpherePosition))
                    {
                        heightMapInfo.GetHeightAndNormal(checkSpherePosition, out newHeight, out newNormal);
                        if (gravity[playerIndex])
                        {
                            if (newHeight > spherePosition.Y)
                            {
                                radiusCheck = true;
                            }
                        }
                        else if (newHeight > oldHeight)
                        {
                            radiusCheck = true;
                        }
                    }
                    else
                    {
                        radiusCheck = true;
                    }
                    if (radiusCheck)
                    {
                        newSpherePosition.Y = spherePosition.Y;
                        newSpherePosition.X -= SphereRadius * Math.Sign(result.X); ;
                        newSpherePosition.Z -= SphereRadius * Math.Sign(result.Z); ;
                    }

                }                
            }
            else
            {
                Vector3 tempPosition = newSpherePosition;
                Vector3 tempVelocity;
                tempPosition.X -= currentVelocity.X;
                if (heightMapInfo.IsOnHeightmap(tempPosition))
                {
                    tempVelocity = Vector3.Reflect(currentVelocity, Vector3.UnitX);
                    if (heightMapInfo.IsOnHeightmap(spherePosition + tempVelocity))
                    {
                        heightMapInfo.GetHeightAndNormal(spherePosition + tempVelocity, out newHeight, out newNormal);
                        if (newHeight <= oldHeight + 10.0f)
                        {
                            currentVelocity = tempVelocity;
                        }
                    }
                }
                else
                {
                    tempVelocity = Vector3.Reflect(currentVelocity, Vector3.UnitZ);
                    if (heightMapInfo.IsOnHeightmap(spherePosition + tempVelocity))
                    {
                        heightMapInfo.GetHeightAndNormal(spherePosition + tempVelocity, out newHeight, out newNormal);
                        if (newHeight <= oldHeight + 10.0f)
                        {
                            currentVelocity = tempVelocity;
                        }
                    }
                }
                newSpherePosition = spherePosition;
            }

            // now we need to roll the ball "forward." to do this, we first calculate
            // how far it has moved.
            float distanceMoved = Vector3.Distance(spherePosition, newSpherePosition);

            // The length of an arc on a circle or sphere is defined as L = theta * r,
            // where theta is the angle that defines the arc, and r is the radius of
            // the circle.
            // we know L, that's the distance the sphere has moved. we know r, that's
            // our constant "sphereRadius". We want to know theta - that will tell us
            // how much to rotate the sphere. we rearrange the equation to get...
            float theta = distanceMoved / SphereRadius;

            // now that we know how much to rotate the sphere, we have to figure out 
            // whether it will roll forward or backward. We'll base this on the user's
            // input.
            int rollDirection = movement.Z > 0 ? 1 : -1;

            // finally, we'll roll it by rotating around the sphere's "right" vector.
            sphereRollingMatrix *= Matrix.CreateFromAxisAngle(sphereFacingMatrix.Right,
                theta * rollDirection);

            // once we've finished all computations, we can set spherePosition to the
            // new position that we calculated.
            spherePosition = newSpherePosition;

            // Check for exit.
            if (input.exit())
            {
                spherePosition = levelValues[currentLevel].initialPosition;
                sphereFacingDirection = 0;
                currentState = GameState.MAINMENU;
            }

            if (input.reset(player))
            {
                spherePosition = levelValues[currentLevel].initialPosition;
                sphereFacingDirection = 0;
            }

            if (player == PlayerIndex.One)
            {
                p1Position = spherePosition;
                p1Facing = sphereFacingDirection;
                p1RollingMatrix = sphereRollingMatrix;
                p1Velocity = currentVelocity;
            }
            else if (player == PlayerIndex.Two)
            {
                p2Position = spherePosition;
                p2Facing = sphereFacingDirection;
                p2RollingMatrix = sphereRollingMatrix;
                p2Velocity = currentVelocity;
            }
        }

        #endregion
    }


    #region Entry Point

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static class Program
    {
        static void Main()
        {
            using (HeightmapCollisionGame game = new HeightmapCollisionGame())
            {
                game.Run();
            }
        }
    }

    #endregion
}
