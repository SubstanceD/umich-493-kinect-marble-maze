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
        Skeleton[] skeletons;
        Skeleton currentSkeleton;

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
                    sensor.SkeletonStream.Enable();
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

        //strafe amount

        //turn amount

        //exit
        public bool exit()
        {
            if (currentKeyboardState.IsKeyDown(Keys.Escape) ||
                currentGamePadState.Buttons.Back == ButtonState.Pressed)
                return true;
            return false;
        }


        //get hand position
        public Vector2 getHandPosition()
        {
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

            double x = rShoulder.Position.X
                - rElbow.Position.X;
            double y = rShoulder.Position.Y
                - rElbow.Position.Y;
            double z = rShoulder.Position.Z
                - rElbow.Position.Z;

            return (float)Math.Sqrt(x * x + y * y + z * z);
        }
    }
}
