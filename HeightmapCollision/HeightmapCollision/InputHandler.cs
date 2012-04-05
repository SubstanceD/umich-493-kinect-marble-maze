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

        KeyboardState currentKeyboardState;
        GamePadState currentGamePadState;
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
                        Smoothing = 1.0f,
                        Correction = 0.1f,
                        JitterRadius = 0.05f,
                        MaxDeviationRadius = 0.05f,
                        Prediction = 0.1f
                    };
                    sensor.SkeletonStream.Enable(parameters);
                }
                sensor.Start();
            }
        }


        //update kinect information
        public void update()
        {
            //Get mouse, gamepad, and keyboard info
            currentGamePadState = GamePad.GetState(PlayerIndex.One);
            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();

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

                    for (int i = 0; i < sframe.SkeletonArrayLength; i++)
                    {
                        if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                        {
                            currentSkeleton = skeletons[i];
                            break;
                        }
                    }
                }
            }

        }

        //move forward amount
        public float moveAmount()
        {
            float result = -currentGamePadState.ThumbSticks.Left.Y;

            if (currentKeyboardState.IsKeyDown(Keys.W) ||
                currentKeyboardState.IsKeyDown(Keys.Up) ||
                currentGamePadState.DPad.Up == ButtonState.Pressed)
            {
                result -= 1;
            }
            if (currentKeyboardState.IsKeyDown(Keys.S) ||
                currentKeyboardState.IsKeyDown(Keys.Down) ||
                currentGamePadState.DPad.Down == ButtonState.Pressed)
            {
                result += 1;
            }
            result = MathHelper.Clamp(result, -1, 1);

            return result;
        }

        //strafe amount
        public float strafeAmount()
        {
            float result = 0;
            if (currentKeyboardState.IsKeyDown(Keys.Z))
                result -= 1;
            if (currentKeyboardState.IsKeyDown(Keys.X))
                result += 1;
            result = MathHelper.Clamp(result, -1, 1);

            return result;
        }

        //turn amount
        public float turnAmount()
        {
            float result = -currentGamePadState.ThumbSticks.Left.X;
            if (currentKeyboardState.IsKeyDown(Keys.A) ||
                currentKeyboardState.IsKeyDown(Keys.Left) ||
                currentGamePadState.DPad.Left == ButtonState.Pressed ||
                leftArmExtended())
            {
                result += 1;
            }
            if (currentKeyboardState.IsKeyDown(Keys.D) ||
                currentKeyboardState.IsKeyDown(Keys.Right) ||
                currentGamePadState.DPad.Right == ButtonState.Pressed ||
                rightArmExtended())
            {
                result -= 1;
            }

            // clamp the turn amount between -1 and 1, and then use the finished
            // value to turn the sphere.
            result = MathHelper.Clamp(result, -1, 1);

            return result;
        }
        bool rightArmExtended()
        {
            if (currentSkeleton == null)
                return false;
            float armLength = computeArmLength();
            Joint rHand = currentSkeleton.Joints[JointType.HandRight];
            Joint rShoulder = currentSkeleton.Joints[JointType.ShoulderRight];

            if (distance(rHand, rShoulder) > 1.5 * armLength)
                return true;

            return false;
        }
        bool leftArmExtended()
        {
            if (currentSkeleton == null)
                return false;
            float armLength = computeArmLength();
            Joint lHand = currentSkeleton.Joints[JointType.HandLeft];
            Joint lShoulder = currentSkeleton.Joints[JointType.ShoulderLeft];

            if (distance(lHand, lShoulder) > 1.5 * armLength)
                return true;
            return false;
        }

        //jump
        public bool jumped()
        {
            if (currentKeyboardState.IsKeyDown(Keys.Space))
            {
                return true;
            }
            return false;
        }

        //exit
        public bool exit()
        {
            if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
                currentGamePadState.Buttons.Back == ButtonState.Pressed)
                return true;
            return false;
        }

        public bool reset()
        {
            if (currentKeyboardState.IsKeyDown(Keys.R))
                return true;
            return false;
        }

        //get mouse data

        public MouseState getMouse()
        {
            return currentMouseState;
        }


        //get hand position
        public Vector2 getHandPosition()
        {
            if (currentSkeleton == null)
                return Vector2.Zero;

            Vector2 handPosition = new Vector2();

            Joint hand = currentSkeleton.Joints[JointType.HandRight];

            float armLength = computeArmLength();
            float headY = currentSkeleton.Joints[JointType.Head].Position.Y;
            float spineY = currentSkeleton.Joints[JointType.Spine].Position.Y;
            float rShoulderX = currentSkeleton.Joints[JointType.ShoulderRight].Position.X;
            float lShoulderX = currentSkeleton.Joints[JointType.ShoulderLeft].Position.X;

            Viewport viewport = graphics.GraphicsDevice.Viewport;

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
            Joint rShoulder = currentSkeleton.Joints[JointType.ShoulderRight];
            Joint rElbow = currentSkeleton.Joints[JointType.ElbowRight];

            return distance(rShoulder, rElbow);
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
