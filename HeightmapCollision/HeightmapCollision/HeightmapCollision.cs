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
        MAINMENU, INGAME, NOCHANGE, EXIT, INGAME2P, SELECTPLAYERS, FINISH
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

        const float FrictionConst = .04f;

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

        //main menu buttons
        Button startButton;
        Button exitButton;
        
        Rectangle startPos;
        Rectangle exitPos;

        //selectplayers buttons
        Button onePlayerButton;
        Button twoPlayerButton;
        Button cancelButton;

        Texture2D cursor;
        Vector2 cursorPos;

        Texture2D hand;
        Vector2 handPos;

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

        bool gravity;

        Vector3 movement;

        bool hasJumped;
        float jumpHeight;

        public int currentLevel;
        static public int numLevels = 4;

        public static finishValues[] levelValues = new finishValues[]
        {
            new finishValues(2500, 2800, 2500, 2800), //Level One
            new finishValues(-3150, -2850, 2500, 2800), // Level Two
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
            input = new InputHandler(graphics);
            Content.RootDirectory = "Content";
            gravity = false;
            hasJumped = false;
            currentLevel = 0;
	        p1Velocity = Vector3.Zero;
            p2Velocity = Vector3.Zero;
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
            //leftViewport.X = original.X;
            //leftViewport.Y = original.Y;
            //leftViewport.Width = original.Width / 2;
            //leftViewport.Height = original.Height;

            rightViewport = new Viewport(new Rectangle(original.X + leftViewport.Width, original.Y, original.Width / 2, original.Height));
            //rightViewport.X = original.X + leftViewport.Width;
            //rightViewport.Y = original.Y;
            //rightViewport.Width = original.Width / 2;
            //rightViewport.Height = original.Height;

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
            
            sphere = Content.Load<Model>("sphere");
            flag = Content.Load<Model>("flag");

            cursor = Content.Load<Texture2D>("Cursor");
            hand = Content.Load<Texture2D>("Cursor");

            //mainmenubuttons
            startPos = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("PlayButton").Width / 2, GraphicsDevice.Viewport.Height / 4 - Content.Load<Texture2D>("PlayButtonHi").Height/2,
                Content.Load<Texture2D>("PlayButton").Width, Content.Load<Texture2D>("PlayButtonHi").Height);
            exitPos = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("ExitButton").Width / 2, 2*GraphicsDevice.Viewport.Height / 3 - Content.Load<Texture2D>("ExitButtonHi").Height / 2,
                Content.Load<Texture2D>("ExitButton").Width, Content.Load<Texture2D>("ExitButtonHi").Height);

            exitButton = new Button(exitPos, Content.Load<Texture2D>("ExitButton"), Content.Load<Texture2D>("ExitButtonHi"), GameState.EXIT);
            startButton = new Button(startPos, Content.Load<Texture2D>("PlayButton"), Content.Load<Texture2D>("PlayButtonHi"), GameState.SELECTPLAYERS);

            //selectPlayers buttons
            Rectangle position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("OnePlayer").Width / 2, 75, 400, 200);
            onePlayerButton = new Button(position, Content.Load<Texture2D>("OnePlayer"), Content.Load<Texture2D>("OnePlayerHi"), GameState.INGAME);
            position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("OnePlayer").Width / 2, 275, 400, 200);
            twoPlayerButton = new Button(position, Content.Load<Texture2D>("TwoPlayer"), Content.Load<Texture2D>("TwoPlayerHi"), GameState.INGAME2P);
            position = new Rectangle(GraphicsDevice.Viewport.Width / 2 - Content.Load<Texture2D>("OnePlayer").Width / 2, 475, 400, 200);
            cancelButton = new Button(position, Content.Load<Texture2D>("MainMenu"), Content.Load<Texture2D>("MainMenuHi"), GameState.MAINMENU);

            loadLevel();
        }

        private void loadLevel()
        {
            string levelName = "level_";
            levelName += Convert.ToString(currentLevel + 1);

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
            input.update();

            switch (currentState)
            {
                case GameState.INGAME:
                    HandleInput(PlayerIndex.One);
                    UpdateCamera(PlayerIndex.One);
                    Console.WriteLine("Ball position is: {0}", p1Position.ToString());
                    if (isOnFinish(p1Position))
                    {
                        currentState = GameState.MAINMENU;

                        if (currentLevel < (numLevels - 1))
                        {
                            ++currentLevel;
                            loadLevel();
                        }
                    }
                    break;
                case GameState.INGAME2P:
                    HandleInput(PlayerIndex.One);
                    UpdateCamera(PlayerIndex.One);
                    HandleInput(PlayerIndex.Two);
                    UpdateCamera(PlayerIndex.Two);
                    if (isOnFinish(p1Position) && isOnFinish(p2Position))
                    {
                        currentState = GameState.MAINMENU;
                        if (currentLevel < (numLevels - 1))
                        {
                            ++currentLevel;
                            loadLevel();
                        }
                    }
                    break;
                case GameState.MAINMENU:
                    UpdateMainMenu(gameTime);
                    break;
                case GameState.SELECTPLAYERS:
                    UpdatePlayerSelect(gameTime);
                    break;
                case GameState.EXIT:
                    this.Exit();
                    break;
                case GameState.FINISH:
                    UpdateFinishGame(gameTime);
                    break;
                default:
                    //error message here
                    break;
            }

            base.Update(gameTime);
        }

        private void UpdateFinishGame(GameTime gameTime)
        {
            
        }

        private void UpdatePlayerSelect(GameTime gameTime)
        {
            GameState buttonState;

            buttonState = onePlayerButton.Update(gameTime, input.getMouse(), input.getHandPosition(PlayerIndex.One));

            if (buttonState != GameState.NOCHANGE)
            {
                currentState = buttonState;
                if (currentState == GameState.INGAME)
                {
                    graphics.GraphicsDevice.BlendState = BlendState.Opaque;
                    graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
                }
                return;
            }

            buttonState = twoPlayerButton.Update(gameTime, input.getMouse(), input.getHandPosition(PlayerIndex.One));

            if (buttonState != GameState.NOCHANGE)
            {
                currentState = buttonState;
                if (currentState == GameState.INGAME2P)
                {
                    graphics.GraphicsDevice.BlendState = BlendState.Opaque;
                    graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
                    //spherePosition = Vector3.Zero;
                }
                return;
            }


            buttonState = cancelButton.Update(gameTime, input.getMouse(), input.getHandPosition(PlayerIndex.One));

            if (buttonState != GameState.NOCHANGE)
            {
                currentState = buttonState;
                return;
            }

            cursorPos = new Vector2(input.getMouse().X, input.getMouse().Y);
            handPos = input.getHandPosition(PlayerIndex.One);
        }

        private void UpdateMainMenu(GameTime gameTime)
        {
            GameState buttonState;

            buttonState = startButton.Update(gameTime, input.getMouse(), input.getHandPosition(PlayerIndex.One));

            if (buttonState != GameState.NOCHANGE)
            {
                graphics.GraphicsDevice.BlendState = BlendState.Opaque;
                graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
                currentState = buttonState;
                //if (currentState == GameState.INGAME)
                //{
                //    spherePosition = Vector3.Zero;
                //}
                return;
            }

            buttonState = exitButton.Update(gameTime, input.getMouse(), input.getHandPosition(PlayerIndex.One));

            if (buttonState != GameState.NOCHANGE)
            {
                currentState = buttonState;
                return;
            }

            cursorPos = new Vector2(input.getMouse().X, input.getMouse().Y);
            handPos = input.getHandPosition(PlayerIndex.One);
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


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice device = graphics.GraphicsDevice;
            
            device.Clear(Color.Black);

            switch (currentState)
            {
                case GameState.INGAME:
                    DrawModel(terrain, Matrix.Identity, p1View, projectionMatrix);
                    DrawModel(sphere, p1RollingMatrix * 
                        Matrix.CreateTranslation(p1Position), p1View, projectionMatrix);
                    DrawModel(flag, Matrix.CreateTranslation(flagPosition), p1View, projectionMatrix);
                    
                    break;
                case GameState.INGAME2P:
                    Viewport original = graphics.GraphicsDevice.Viewport;
                    //player one
                    graphics.GraphicsDevice.Viewport = rightViewport;
                    DrawModel(terrain, Matrix.Identity, p1View, p1Projection);
                    DrawModel(sphere, p1RollingMatrix *
                        Matrix.CreateTranslation(p1Position), p1View, p1Projection);
                    DrawModel(sphere, p2RollingMatrix *
                        Matrix.CreateTranslation(p2Position), p1View, p1Projection);
                    //player two
                    graphics.GraphicsDevice.Viewport = leftViewport;
                    DrawModel(terrain, Matrix.Identity, p2View, p2Projection);
                    DrawModel(sphere, p2RollingMatrix *
                        Matrix.CreateTranslation(p2Position), p2View, p2Projection);
                    DrawModel(sphere, p1RollingMatrix *
                        Matrix.CreateTranslation(p1Position), p2View, p2Projection);

                    graphics.GraphicsDevice.Viewport = original;
                    
                    // If there was any alpha blended translucent geometry in
                    // the scene, that would be drawn here.
                    break;
                case GameState.MAINMENU:
                    spriteBatch.Begin();
                    startButton.Draw(spriteBatch);
                    exitButton.Draw(spriteBatch);
                    spriteBatch.Draw(cursor, cursorPos, Color.White);
                    spriteBatch.Draw(hand, handPos, Color.White);
                    spriteBatch.End();
                    break;
                case GameState.SELECTPLAYERS:
                    spriteBatch.Begin();
                    onePlayerButton.Draw(spriteBatch);
                    twoPlayerButton.Draw(spriteBatch);
                    cancelButton.Draw(spriteBatch);
                    spriteBatch.Draw(cursor, cursorPos, Color.White);
                    spriteBatch.Draw(hand, handPos, Color.White);
                    spriteBatch.End();
                    break;
                //case GameState.FINISH:
                    //Need A notice that the player won as well as a next level button
                    //And a exit button

                default:
                    //error message
                    break;
            }


            base.Draw(gameTime);
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

            sphereFacingDirection += input.turnAmount(player) *SphereTurnSpeed;


            // Next, we want to move the sphere forward or back. to do this, 
            // we'll create a Vector3 and modify use the user's input to modify the Z
            // component, which corresponds to the forward direction.
            
            movement = Vector3.Zero;

            movement.Z = input.moveAmount(player);
            //movement.X = input.strafeAmount(player);

            Vector3 newSpherePosition = spherePosition + currentVelocity;

            Vector3 oldNormal;
            Vector3 newNormal;
            if (heightMapInfo.IsOnHeightmap(newSpherePosition))
            {
                heightMapInfo.GetHeightAndNormal(newSpherePosition, out newHeight, out newNormal);
            }

            if (input.jumped() && !hasJumped && !gravity)
            {
                hasJumped = true;
                jumpHeight = spherePosition.Y + SphereRadius * 2;
            }


            if (gravity)
            {
                if ((spherePosition.Y - movement.Y) > (newHeight + SphereRadius))
                {
                    movement.Y -= gravityConst;
                }
                else
                {
                    newSpherePosition.Y = newHeight + SphereRadius;
                    gravity = false;
                    currentVelocity.Y = 0;
                    movement.Y = 0;
                }
            }

            if (hasJumped)
            {
                if (spherePosition.Y < jumpHeight)
                {
                    movement.Y = jumpConst;
                }
                else
                {
                    gravity = true;
                    hasJumped = false;
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
                
                if (!gravity && !hasJumped)
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
                        if (hasJumped)
                        {
                            newSpherePosition.Y = newHeight + SphereRadius + 3;
                        }
                    }
                    else
                    {
                        //Console.WriteLine("I am not going up a wall ever!");
                        if (!gravity && !hasJumped)
                        {
                            newSpherePosition = spherePosition;
                        }
                        else if (newSpherePosition.Y < newHeight + 10)
                        {
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
                            if (!hasJumped)
                            {
                                gravity = true;
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
                        if (gravity)
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

            if (input.reset())
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
