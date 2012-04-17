using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !XBOX360

using Microsoft.Kinect;

#endif

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HeightmapCollision
{
    class InputHandler
    {
        #if !XBOX360
        //set up kinect stuff
        // Kinect declarations
        KinectSensor sensor = null;
        Skeleton[] skeletons = null;
        Skeleton currentSkeleton = null;
        Skeleton skeletonPlayerOne = null;
        Skeleton skeletonPlayerTwo = null;
        #endif       
        int IDPlayerOne = -1;
        int IDPlayerTwo = -1;
        float armLength;
        float armLengthP1;
        float armLengthP2;
        GamePadState gp;
        GamePadState gp2;
        GameTime currentTime;
        float handUpTime;
        float handUpTimeP1;
        float handUpTimeP2;
        bool handUpP1 = false;
        bool handUpP2 = false;
        bool handUp;

        bool skeletonsValid = false;


        KeyboardState currentKeyboardState;
        MouseState currentMouseState;

        //for viewport use
        GraphicsDeviceManager graphics;

        public InputHandler(GraphicsDeviceManager graphicsManager)
        {
            graphics = graphicsManager;
            
            #if !XBOX360
            // Enable && initialize Kinect
            if (KinectSensor.KinectSensors.Count > 0)
            {
                sensor = KinectSensor.KinectSensors[0];
                if (sensor.Status == KinectStatus.Connected)
                {
                    TransformSmoothParameters parameters = new TransformSmoothParameters
                    {
                        Smoothing = 0.5f,
                        Correction = 0.5f,
                        Prediction = 0.4f,
                        JitterRadius = .2f,
                        MaxDeviationRadius = 0.1f
                    };
                    sensor.SkeletonStream.Enable(parameters);
                    //sensor.SkeletonStream.Enable();
                }
                sensor.Start();
            }
            #endif
        }


        //update kinect information
        public void update(GameTime gameTime)
        {
            //Get mouse, and keyboard info
            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();
            gp = GamePad.GetState(PlayerIndex.One);
            gp2 = GamePad.GetState(PlayerIndex.Two);

            currentTime = gameTime;

            #if !XBOX360
            // Get Kinect input if connected
            if (sensor != null)
            {
                SkeletonFrame sframe = sensor.SkeletonStream.OpenNextFrame(0);

                if (sframe != null)
                {
                    if (skeletons == null || skeletons.Length != sframe.SkeletonArrayLength)
                    {
                        skeletons = new Skeleton[sframe.SkeletonArrayLength];
                    }

                    sframe.CopySkeletonDataTo(skeletons);

                    currentSkeleton = null;

                    bool p1found = false;
                    bool p2found = false;

                    for (int i = 0; i < sframe.SkeletonArrayLength; i++)
                    {
                        if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                        {
                            currentSkeleton = skeletons[i];
                            if (currentSkeleton.TrackingId == IDPlayerOne)
                            {
                                skeletonPlayerOne = currentSkeleton;
                                IDPlayerOne = skeletonPlayerOne.TrackingId;
                                p1found = true;
                            }
                            else if (currentSkeleton.TrackingId == IDPlayerTwo)
                            {
                                skeletonPlayerTwo = currentSkeleton;
                                IDPlayerTwo = skeletonPlayerTwo.TrackingId;
                                p2found = true;
                            }                            
                        }
                    }
                    if (!p1found || !p2found)
                    {
                        if (!p1found)
                            IDPlayerOne = -1;
                        if (!p2found)
                            IDPlayerTwo = -1;
                        for (int i = 0; i < sframe.SkeletonArrayLength; i++)
                        {
                            if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                            {
                                currentSkeleton = skeletons[i];
                                if (!p1found && currentSkeleton.TrackingId != IDPlayerTwo)
                                {
                                    skeletonPlayerOne = currentSkeleton;
                                    IDPlayerOne = skeletonPlayerOne.TrackingId;
                                    p1found = true;
                                }
                                else if (!p2found && currentSkeleton.TrackingId != IDPlayerOne)
                                {
                                    skeletonPlayerTwo = currentSkeleton;
                                    IDPlayerTwo = skeletonPlayerTwo.TrackingId;
                                    p2found = true;
                                }
                            }
                        }
                    }

                    currentSkeleton = skeletonPlayerOne;
                    armLengthP1 = computeArmLength();
                    currentSkeleton = skeletonPlayerTwo;
                    armLengthP2 = computeArmLength();
                    sframe.Dispose();
                }
                if (skeletonPlayerOne == null && skeletonPlayerTwo == null)
                    skeletonsValid = false;
                else skeletonsValid = true;
            }
            #endif
        }
#if !XBOX360
        public bool kinectAttached()
        {
            if (sensor == null)
                return false;
            return true;
        }
#endif
        //move forward amount
        public float moveAmount(PlayerIndex player)
        {
            float result = 0;

            if (player == PlayerIndex.One)
            {
#if !XBOX360
                currentSkeleton = skeletonPlayerOne;
                if (currentKeyboardState.IsKeyDown(Keys.Up) ||
                leaningForward() || (gp.DPad.Up == ButtonState.Pressed))
                {
                    result -= 1;
                }
                if (currentKeyboardState.IsKeyDown(Keys.Down) ||
                    leaningBack() || (gp.DPad.Down == ButtonState.Pressed))
                {
                    result += 1;
                }
#endif
                Vector2 gpMovement = gp.ThumbSticks.Left;
            }
            else if (player == PlayerIndex.Two)
            {
#if !XBOX360
                currentSkeleton = skeletonPlayerTwo;
#endif
                if (currentKeyboardState.IsKeyDown(Keys.W) ||
                leaningForward() || (gp2.DPad.Up == ButtonState.Pressed))
                {
                    result -= 1;
                }
                if (currentKeyboardState.IsKeyDown(Keys.S) ||
                    leaningBack() || (gp2.DPad.Down == ButtonState.Pressed))
                {
                    result += 1;
                }
            }

            
            result = MathHelper.Clamp(result, -1, 1);

            return result;
        }

        //strafe amount
        public float strafeAmount(PlayerIndex player)
        {
            float result = 0;
            if (player == PlayerIndex.One)
            {
#if !XBOX360
                currentSkeleton = skeletonPlayerOne;
#endif
                if (leaningLeft())
                    result -= 1;
                if (leaningRight())
                    result += 1;
            }
            else if (player == PlayerIndex.Two)
            {
#if !XBOX360
                currentSkeleton = skeletonPlayerTwo;
#endif
                if (leaningLeft())
                    result -= 1;
                if (leaningRight())
                    result += 1;
            }
            result = MathHelper.Clamp(result, -1, 1);

            return result;
        }

        bool leaningRight()
        {
#if !XBOX360
            if (currentSkeleton != null)
            {
                Joint shoulder = currentSkeleton.Joints[JointType.ShoulderLeft];
                Joint spine = currentSkeleton.Joints[JointType.Spine];

                if (Math.Abs(shoulder.Position.X - spine.Position.X) < 0.15)
                    return true;
            }
#endif
            return false;
        }

        bool leaningLeft()
        {
#if !XBOX360
            if (currentSkeleton != null)
            {
                Joint shoulder = currentSkeleton.Joints[JointType.ShoulderRight];
                Joint spine = currentSkeleton.Joints[JointType.Spine];

                if (Math.Abs(shoulder.Position.X - spine.Position.X) < 0.15)
                    return true;
            }
#endif
            return false;
        }

        bool leaningForward()
        {
#if !XBOX360
            if (currentSkeleton != null)
            {
                Joint shoulder = currentSkeleton.Joints[JointType.ShoulderCenter];
                Joint spine = currentSkeleton.Joints[JointType.Spine];

                if (shoulder.Position.Z < spine.Position.Z)
                    return true;
            }
#endif
            return false;
        }

        bool leaningBack()
        {
#if !XBOX360
            if (currentSkeleton != null)
            {
                Joint shoulder = currentSkeleton.Joints[JointType.ShoulderCenter];
                Joint spine = currentSkeleton.Joints[JointType.Spine];

                if (shoulder.Position.Z - 0.07f  > spine.Position.Z)
                    return true;
            }
#endif
            return false;
        }

        //turn amount
        public float turnAmount(PlayerIndex player)
        {
            float result = 0;
            if (player == PlayerIndex.One)
            {
#if !XBOX360
                currentSkeleton = skeletonPlayerOne;
#endif
                if (currentKeyboardState.IsKeyDown(Keys.Left) ||
                    !leftArmExtended(player) && skeletonsValid || (gp.DPad.Left == ButtonState.Pressed))
                {
                    result += 1;
                }
                if (currentKeyboardState.IsKeyDown(Keys.Right) ||
                    !rightArmExtended(player) && skeletonsValid || (gp.DPad.Right == ButtonState.Pressed))
                {
                    result -= 1;
                }
            }
            else if (player == PlayerIndex.Two)
            {
#if !XBOX360
                currentSkeleton = skeletonPlayerTwo;
#endif
                if (currentKeyboardState.IsKeyDown(Keys.A) ||
                    !leftArmExtended(player) && skeletonsValid || (gp2.DPad.Left == ButtonState.Pressed))
                {
                    result += 1;
                }
                if (currentKeyboardState.IsKeyDown(Keys.D) ||
                    !rightArmExtended(player) && skeletonsValid || (gp2.DPad.Right == ButtonState.Pressed))
                {
                    result -= 1;
                }
            }

            // clamp the turn amount between -1 and 1, and then use the finished
            // value to turn the sphere.
            result = MathHelper.Clamp(result, -1, 1);

            return result;
        }

        bool rightArmExtended(PlayerIndex player)
        {
#if !XBOX360
            if (player == PlayerIndex.One)
            {
                currentSkeleton = skeletonPlayerOne;
                armLength = armLengthP1;
            }
            else if (player == PlayerIndex.Two)
            {
                currentSkeleton = skeletonPlayerTwo;
                armLength = armLengthP2;
            } 

            if (currentSkeleton == null)
                return false;

            Joint rHand = currentSkeleton.Joints[JointType.HandRight];
            Joint rShoulder = currentSkeleton.Joints[JointType.ShoulderRight];

            if (distance(rHand, rShoulder) > 1.75 * armLength)
                return true;
#endif
            return false;
        }

        bool leftArmExtended(PlayerIndex player)
        {
#if !XBOX360
            if (player == PlayerIndex.One)
            {
                currentSkeleton = skeletonPlayerOne;
                armLength = armLengthP1;
            }
            else if (player == PlayerIndex.Two)
            {
                currentSkeleton = skeletonPlayerTwo;
                armLength = armLengthP2;
            }

            if (currentSkeleton == null)
                return false;

            Joint lHand = currentSkeleton.Joints[JointType.HandLeft];
            Joint lShoulder = currentSkeleton.Joints[JointType.ShoulderLeft];

            if (distance(lHand, lShoulder) > 1.75 * armLength)
                return true;
#endif
            return false;
        }

        //jump
        public bool jumped(PlayerIndex player)
        {
            if (player == PlayerIndex.One)
            {
                if (currentKeyboardState.IsKeyDown(Keys.RightShift) || 
                    (gp.Buttons.A == ButtonState.Pressed))
                {
                    return true;
                }
            }
            if (player == PlayerIndex.Two)
            {
                if (currentKeyboardState.IsKeyDown(Keys.F) || 
                    (gp2.Buttons.A == ButtonState.Pressed))
                {
                    return true;
                }
            }
            return false;
        }

        //exit
        public bool exit()
        {
            if (currentKeyboardState.IsKeyDown(Keys.Escape))
                return true;
            return false;
        }

        public bool reset(PlayerIndex player)
        {
            if (player == PlayerIndex.One)
            {
                if (currentKeyboardState.IsKeyDown(Keys.End) || 
                    (gp.Buttons.Y == ButtonState.Pressed)
                    || handAboveHead(player))
                {
                    return true;
                }
            }
            if (player == PlayerIndex.Two)
            {
                if (currentKeyboardState.IsKeyDown(Keys.R) ||
                    (gp2.Buttons.Y == ButtonState.Pressed)
                    || handAboveHead(player))
                {
                    return true;
                }
            }
            return false;
        }

        bool handAboveHead(PlayerIndex player)
        {
            bool result = false;
#if !XBOX360
            if(player == PlayerIndex.One)
            {
                currentSkeleton = skeletonPlayerOne;
                handUp = handUpP1;
                handUpTime = handUpTimeP1;
            }
            else if(player == PlayerIndex.Two)
            {
                currentSkeleton = skeletonPlayerTwo;
                handUpTime = handUpTimeP2;
                handUp = handUpP2;
            }

            if (!handUp)
            {
                if (currentSkeleton != null)
                {
                    if ((currentSkeleton.Joints[JointType.HandLeft].Position.Y >= currentSkeleton.Joints[JointType.Head].Position.Y) ||
                        (currentSkeleton.Joints[JointType.HandRight].Position.Y >= currentSkeleton.Joints[JointType.Head].Position.Y))
                    {
                        handUp = true;
                        //result = true;
                        handUpTime = currentTime.TotalGameTime.Seconds;
                    }
                }
            }
            else
            {
                if (currentSkeleton != null)
                {
                    if ((currentSkeleton.Joints[JointType.HandLeft].Position.Y < currentSkeleton.Joints[JointType.Head].Position.Y) &&
                        (currentSkeleton.Joints[JointType.HandRight].Position.Y < currentSkeleton.Joints[JointType.Head].Position.Y))
                    {
                        handUp = false;
                    }
                }
                //Console.WriteLine("current hand up duration: {0}", currentTime.TotalGameTime.Seconds - handUpTime);
                if ((currentTime.TotalGameTime.Seconds - handUpTime) >= 2)
                {
                    result = true;
                    handUp = false;
                    handUpTime = currentTime.TotalGameTime.Seconds;
                }
            }

            if(player == PlayerIndex.One)
            {
                handUpP1 = handUp;
                handUpTimeP1 = handUpTime;
            }
            else if(player == PlayerIndex.Two)
            {
                handUpTimeP2 = handUpTime;
                handUpP2 = handUp;
            }
#endif
            return result;
        }

        //get mouse data

        public MouseState getMouse()
        {
            return currentMouseState;
        }


        //get hand position
        public Vector2 getHandPosition(PlayerIndex player, Viewport viewport)
        {
#if !XBOX360
            if (player == PlayerIndex.One)
            {
                currentSkeleton = skeletonPlayerOne;
                armLength = armLengthP1;
            }
            else if (player == PlayerIndex.Two)
            {
                currentSkeleton = skeletonPlayerTwo;
                armLength = armLengthP2;
            }

            if (currentSkeleton == null)
                return new Vector2(-55);

            Vector2 handPosition = new Vector2();

            Joint hand = currentSkeleton.Joints[JointType.HandRight];

            float headY = currentSkeleton.Joints[JointType.Head].Position.Y;
            float spineY = currentSkeleton.Joints[JointType.Spine].Position.Y;
            float rShoulderX = currentSkeleton.Joints[JointType.ShoulderRight].Position.X;
            float lShoulderX = currentSkeleton.Joints[JointType.ShoulderLeft].Position.X;

            //Viewport viewport = graphics.GraphicsDevice.Viewport;

            int viewMinX = viewport.X;
            int viewMaxX = viewMinX + viewport.Width;
            int viewMinY = viewport.Y;
            int viewMaxY = viewport.Height + viewMinY;

            float leftSide = lShoulderX - armLength / 3;
            float rightSide = rShoulderX + armLength / 3;

            handPosition.X = hand.Position.X * ((viewMaxX - viewMinX) / (rightSide - leftSide))
                + (viewMinX - leftSide * ((viewMaxX - viewMinX) / (rightSide - leftSide)));

            handPosition.Y = hand.Position.Y * ((viewMaxY - viewMinY) / (spineY - headY)) +
                (viewMinY - headY * ((viewMaxY - viewMinY) / (spineY - headY)));

            return handPosition;
#else
            return Vector2.Zero;
#endif
        }

        float computeArmLength()
        {
#if !XBOX360
            if (currentSkeleton != null)
            {
                Joint rShoulder = currentSkeleton.Joints[JointType.ShoulderRight];
                Joint rElbow = currentSkeleton.Joints[JointType.ElbowRight];

                return distance(rShoulder, rElbow);
            }
            else
#endif
                return armLength;
        }
#if !XBOX360
        float distance(Joint a, Joint b)
        {
            double x = a.Position.X
                - b.Position.X;
            double y = a.Position.Y
                - b.Position.Y;
            double z = a.Position.Z
                - b.Position.Z;

            return (float)Math.Sqrt(x * x + y * y + z * z);
        }
#endif
    }
}
