using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace HeightmapCollision
{
    class InputHandler
    {
        //set up kinect stuff
        // Kinect declarations
        KinectSensor sensor = null;
        Skeleton[] skeletons = null;
        Skeleton currentSkeleton = null;
        Skeleton skeletonPlayerOne = null;
        Skeleton skeletonPlayerTwo = null;
        int IDPlayerOne = -1;
        int IDPlayerTwo = -1;
        float armLength;
        float armLengthP1;
        float armLengthP2;
        GamePadState gp;
        GamePadState gp2;

        KeyboardState currentKeyboardState;
        MouseState currentMouseState;

        //for viewport use
        GraphicsDeviceManager graphics;

        public InputHandler(GraphicsDeviceManager graphicsManager)
        {
            graphics = graphicsManager;
            
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
        }


        //update kinect information
        public void update()
        {
            //Get mouse, and keyboard info
            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();
            gp = GamePad.GetState(PlayerIndex.One);
            gp2 = GamePad.GetState(PlayerIndex.Two);

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
            }

        }

        public bool kinectAttached()
        {
            if (sensor == null)
                return false;
            return true;
        }

        //move forward amount
        public float moveAmount(PlayerIndex player)
        {
            float result = 0;

            if (player == PlayerIndex.One)
            {
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
                Vector2 gpMovement = gp.ThumbSticks.Left;
            }
            else if (player == PlayerIndex.Two)
            {
                currentSkeleton = skeletonPlayerTwo;
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
                currentSkeleton = skeletonPlayerOne;
                if (leaningLeft())
                    result -= 1;
                if (leaningRight())
                    result += 1;
            }
            else if (player == PlayerIndex.Two)
            {
                currentSkeleton = skeletonPlayerTwo;
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
            if (currentSkeleton != null)
            {
                Joint shoulder = currentSkeleton.Joints[JointType.ShoulderLeft];
                Joint spine = currentSkeleton.Joints[JointType.Spine];

                if (Math.Abs(shoulder.Position.X - spine.Position.X) < 0.15)
                    return true;
            }

            return false;
        }

        bool leaningLeft()
        {
            if (currentSkeleton != null)
            {
                Joint shoulder = currentSkeleton.Joints[JointType.ShoulderRight];
                Joint spine = currentSkeleton.Joints[JointType.Spine];

                if (Math.Abs(shoulder.Position.X - spine.Position.X) < 0.15)
                    return true;
            }

            return false;
        }

        bool leaningForward()
        {
            if (currentSkeleton != null)
            {
                Joint shoulder = currentSkeleton.Joints[JointType.ShoulderCenter];
                Joint spine = currentSkeleton.Joints[JointType.Spine];

                if (shoulder.Position.Z < spine.Position.Z)
                    return true;
            }

            return false;
        }

        bool leaningBack()
        {
            if (currentSkeleton != null)
            {
                Joint shoulder = currentSkeleton.Joints[JointType.ShoulderCenter];
                Joint spine = currentSkeleton.Joints[JointType.Spine];

                if (shoulder.Position.Z - 0.07f  > spine.Position.Z)
                    return true;
            }

            return false;
        }

        //turn amount
        public float turnAmount(PlayerIndex player)
        {
            float result = 0;
            if (player == PlayerIndex.One)
            {
                currentSkeleton = skeletonPlayerOne;
                
                if (currentKeyboardState.IsKeyDown(Keys.Left) ||
                    leftArmExtended(player) || (gp.DPad.Left == ButtonState.Pressed))
                {
                    result += 1;
                }
                if (currentKeyboardState.IsKeyDown(Keys.Right) ||
                    rightArmExtended(player) || (gp.DPad.Right == ButtonState.Pressed))
                {
                    result -= 1;
                }
            }
            else if (player == PlayerIndex.Two)
            {
                currentSkeleton = skeletonPlayerTwo;
                if (currentKeyboardState.IsKeyDown(Keys.A) ||
                    leftArmExtended(player) || (gp2.DPad.Left == ButtonState.Pressed))
                {
                    result += 1;
                }
                if (currentKeyboardState.IsKeyDown(Keys.D) ||
                    rightArmExtended(player) || (gp2.DPad.Right == ButtonState.Pressed))
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

            if (distance(rHand, rShoulder) > 2 * armLength)
                return true;

            return false;
        }

        bool leftArmExtended(PlayerIndex player)
        {
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

            if (distance(lHand, lShoulder) > 2 * armLength)
                return true;
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
                    (gp.Buttons.Y == ButtonState.Pressed))
                {
                    return true;
                }
            }
            if (player == PlayerIndex.Two)
            {
                if (currentKeyboardState.IsKeyDown(Keys.R) ||
                    (gp2.Buttons.Y == ButtonState.Pressed))
                {
                    return true;
                }
            }
            return false;
        }

        //get mouse data

        public MouseState getMouse()
        {
            return currentMouseState;
        }


        //get hand position
        public Vector2 getHandPosition(PlayerIndex player, Viewport viewport)
        {
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
        }

        float computeArmLength()
        {
            if (currentSkeleton != null)
            {
                Joint rShoulder = currentSkeleton.Joints[JointType.ShoulderRight];
                Joint rElbow = currentSkeleton.Joints[JointType.ElbowRight];

                return distance(rShoulder, rElbow);
            }
            else
                return armLength;
        }
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
    }
}
